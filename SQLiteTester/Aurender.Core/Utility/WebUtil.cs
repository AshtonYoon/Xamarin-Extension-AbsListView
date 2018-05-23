using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Http;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Net;

namespace Aurender.Core.Utility
{
    public static class WebUtil
    {
        public static object ServiceManager { get; private set; }

        public static String GetBaseAddress(String url)
        {
            Regex rx = new Regex("[hH][tT][tT][pP][s]?://.*?/");

            String baseURL = rx.Match(url).Value;
            return baseURL;
        }
        public static async Task<HttpResponseMessage> GetResponseAsync(String url,
            ICollection<KeyValuePair<String, String>> postData = null,
            String etag = "",
            CookieContainer cookies = null,
            List<KeyValuePair<String,String>> extraHeader = null,
            double timeoutSec = 20,
            Dictionary<String, String> headers = null)
        {
            Debug.Assert(url != null && url.Length > 0, "Url for download cannot be null");
            try
            {
                using (var handler = new HttpClientHandler())
                {
                    if (cookies != null)
                        handler.CookieContainer = cookies;


                    using (var client = new HttpClient(handler))
                    {
                        //client.MaxResponseContentBufferSize
                        client.Timeout = TimeSpan.FromSeconds(timeoutSec);
                        client.DefaultRequestHeaders.Add("Accept", "*/*");
                        client.DefaultRequestHeaders.Add("Connection", "close");
                        if (headers != null)
                        {
                            foreach (var header in headers)
                            {
                                client.DefaultRequestHeaders.Add(header.Key, header.Value);
                            }
                        }
                        else
                        {
                            client.DefaultRequestHeaders.Add("User-Agent", $"AurenderApp");
                        }

                        if (etag.Length == 0 && cookies == null && postData != null)
                        {

                            var response = await client.GetAsync(url).ConfigureAwait(false);

                            return response;
                        }
                        else
                        {
                            String baseURL = GetBaseAddress(url);
                            String relative = url.Replace(baseURL, "");

                            client.BaseAddress = new Uri(baseURL);
                            if ((etag?.Length ?? 0) != 0)
                                client.DefaultRequestHeaders.TryAddWithoutValidation("If-None-Match", etag.ToString());

                            using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, relative))
                            {
                                if (postData != null && postData.Count > 0)
                                {
                                    var content = new FormUrlEncodedContent(postData);
                                    request.Content = content;
                                }
                                if (extraHeader != null)
                                {
                                    foreach (var kv in extraHeader)
                                        request.Headers.Add(kv.Key, kv.Value);
                                }

                                try
                                {
                                    var response = await client.SendAsync(request).ConfigureAwait(false);
                                    //var response = await client.GetAsync(relative).ConfigureAwait(false);

                                    return response;
                                }
                                catch (HttpRequestException ex)
                                {
                                    IARLogStatic.Error("WebUtil", "Failed to get response", ex, new Dictionary<string, string>
                                    {
                                        { "Url", url }
                                    });

                                    return null;
                                }
                            }
                        }
                    }
                }
            }
            catch (InvalidOperationException ex)
            {
                IARLogStatic.Error("WebUtil", $"Might be 404 error, {ex.Message}", logs: new Dictionary<string, string>
                {
                    { "Url", url }
                });
            }
            catch  (TaskCanceledException ex)
            {
                IARLogStatic.Error("WebUtil", "Failed to get response", ex, new Dictionary<string, string>
                {
                    { "Url", url }
                });
            }
            catch (Exception ex)
            {
                //Socket Closed가 발생
                IARLogStatic.Error("WebUtil", $"Failed to handle", ex, new Dictionary<string, string>
                {
                    { "Url", url }
                });
            }
            return null;
        }


        public static async Task<Tuple<bool, String>> DownloadContentsAsync(String url, String etag = "", CookieContainer cookies = null, double timeout = 20)
        {
            Debug.Assert(url != null && url.Length > 0, "Url for download cannot be null");
            bool sucess = false;
            String result;
            try
            {
                using (var response = await GetResponseAsync(url, etag: etag, cookies: cookies, timeoutSec: timeout).ConfigureAwait(false))
                {
                    if (response == null)
                    {
                        sucess = false;
                        result = null;
                    }
                    else if (response.IsSuccessStatusCode)
                    {
                        sucess = true;
                        result = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                        //                        IARLogStatic.Log("WebUtil", $"URL     : {url}");
                        //                        IARLogStatic.Log("WebUtil", $"Reponse : {result}");
                    }
                    else
                    {
                        sucess = false;
                        result = null;

                        if (url.Contains("aurender/upgrade") && response.StatusCode == HttpStatusCode.NotFound)
                        {
                            IARLogStatic.Log("WebUtil", "Upgrade is not available.");
                        }
                        else
                        {
                            IARLogStatic.Error("WebUtil", $"Failed to check", logs: new Dictionary<string, string>
                            {
                                { "Url", url },
                                { "Status code", response.StatusCode.ToString() },
                                { "Reason phrase", response.ReasonPhrase }
                            });
                        }
                    }
                }
            }
            catch (InvalidOperationException ex)
            {
                IARLogStatic.Log("WebUtil", $"Might be 404 error for {url}, {ex.Message}");
                sucess = false;
                result = "";
            }
            catch (Exception ex)
            {
                IARLogStatic.Error("WebUtil", $"Failed to handle", ex, new Dictionary<string, string>
                {
                    { "Url", url }
                });
                result = ex.Message;

            }
            return new Tuple<bool, String>(sucess, result);
        }

        public static async Task<HttpResponseMessage> GetResponseByPostDataAsync(String url,
            ICollection<KeyValuePair<String, String>> postData,
            String etag = "",
            CookieContainer cookies = null,
            List<KeyValuePair<String, String>> extraHeader = null)

        {

            using (var handler = new HttpClientHandler())
            {
                if (cookies != null)
                    handler.CookieContainer = cookies;

                using (var client = new HttpClient(handler))
                {
                    client.DefaultRequestHeaders.Add("Accept", "*/*");
                    client.DefaultRequestHeaders.Add("Connection", "close");
                    client.DefaultRequestHeaders.Add("User-Agent", $"AurenderApp");

                    if (extraHeader != null)
                    {
                        foreach (var kv in extraHeader)
                            client.DefaultRequestHeaders.Add(kv.Key, kv.Value);
                    }

                    FormUrlEncodedContent content = null;

                    if (postData != null && postData.Count > 0)
                        content = new FormUrlEncodedContent(postData);

                    if (etag.Length != 0 || cookies != null)
                    {
                        String baseURL = GetBaseAddress(url);
                        String relative = url.Replace(baseURL, "");

                        client.BaseAddress = new Uri(baseURL);
                        if (etag.Length > 0)
                            client.DefaultRequestHeaders.TryAddWithoutValidation("If-None-Match", etag);


                        HttpResponseMessage result = null;

                        if (client != null)
                        {
                            using (var request = new HttpRequestMessage(HttpMethod.Post, relative) { Content = content })
                            {
                                //try
                                //{
                                result = await client.SendAsync(request).ConfigureAwait(false);
                                //}
                                //catch (Exception e)
                                //{
                                //    Debug.WriteLine(e);
                                //}
                            }
                        }

                        return result;
                    }
                    else
                    {
                        var result = await client.PostAsync(url, content);

                        return result;
                    }
                }
            }
        }

        public static async Task<Tuple<bool, String>> PostDataAndDownloadContentsAsync(String url,
            IList<KeyValuePair<String, String>> postData,
            String etag = "", CookieContainer cookies = null)
        {
            using (var result = await GetResponseByPostDataAsync(url, postData, etag, cookies))
            {

                if (result.IsSuccessStatusCode)
                {
                    string resultContent = await result.Content.ReadAsStringAsync().ConfigureAwait(false);

                    return new Tuple<bool, string>(true, resultContent);
                }
                else
                {
                    IARLogStatic.Error("WebUtil", $"Failed to post data", logs: new Dictionary<string, string>
                    {
                        { "Url", url },
                        { "Status code", result.StatusCode.ToString() },
                        { "Reason phrase", result.ReasonPhrase.ToString() }
                    });
                    return new Tuple<bool, string>(false, result.ReasonPhrase);
                }
            }
        }

        public static async Task<Tuple<bool, String>> DeleteDataAndDownloadContentsAsync(String url, CookieContainer cookies = null)
        {
            var result = await GetResponseByDeleteAsync(url, cookies: cookies);

            if (result.IsSuccessStatusCode)
            {
                string resultContent = await result.Content.ReadAsStringAsync().ConfigureAwait(false);

                return new Tuple<bool, string>(true, resultContent);
            }
            else
            {
                IARLogStatic.Error("WebUtil", $"Failed to delete data", logs: new Dictionary<string, string>
                {
                    { "Url", url },
                    { "Status code", result.StatusCode.ToString() },
                    { "Reason phrase", result.ReasonPhrase.ToString() }
                });
                return new Tuple<bool, string>(false, result.ReasonPhrase);
            }
        }

        public static async Task<HttpResponseMessage> GetResponseByDeleteAsync(String url,
            String etag = "",
            IList<KeyValuePair<String, String>> postData = null, CookieContainer cookies = null)
        {
            HttpResponseMessage result = await SendAsync(HttpMethod.Delete, url, etag, postData, cookies).ConfigureAwait(false);

            return result;
        }

        private static async Task<HttpResponseMessage> SendAsync(HttpMethod method,
            string url,
            string etag = "",
            IList<KeyValuePair<String, String>> postData = null,
            CookieContainer cookies = null)
        {

            using (var handler = new HttpClientHandler())
            {
                if (cookies != null)
                    handler.CookieContainer = cookies;
                using (var client = new HttpClient(handler))
                {
                    client.DefaultRequestHeaders.Add("Accept", "*/*");
                    client.DefaultRequestHeaders.Add("Connection", "close");
                    client.DefaultRequestHeaders.Add("User-Agent", $"AurenderApp");

                    String baseURL = GetBaseAddress(url);
                    String relative = url.Replace(baseURL, "");

                    if (etag.Length != 0 || postData != null || cookies != null)
                    {
                        client.BaseAddress = new Uri(baseURL);

                        if (etag.Length > 0)
                            client.DefaultRequestHeaders.TryAddWithoutValidation("If-None-Match", etag.ToString());

                        using (HttpRequestMessage request = new HttpRequestMessage(method, relative))
                        {
                            if (postData != null)
                            {
                                var content = new FormUrlEncodedContent(postData);
                                request.Content = content;
                            }
                            request.Headers.Add("Accept", "*/*");
                            request.Headers.Add("Connection", "close");
                            request.Headers.Add("User-Agent", $"AurenderApp");
                            var result = await client.SendAsync(request).ConfigureAwait(false);
                            return result;
                        }
                    }
                    else
                    {
                        HttpResponseMessage message;
                        FormUrlEncodedContent content = null;

                        if (postData != null && postData.Count > 0)
                            content = new FormUrlEncodedContent(postData);

                        if (method == HttpMethod.Delete)
                        {
                            message = await client.DeleteAsync(url).ConfigureAwait(false);
                        }
                        else if (method == HttpMethod.Get)
                            message = await client.GetAsync(url).ConfigureAwait(false);
                        else if (method == HttpMethod.Post)
                        {
                            Debug.Assert(postData != null, $"When you Post, you must supply postdata");
                            message = await client.PostAsync(url, content).ConfigureAwait(false);
                        }
                        else if (method == HttpMethod.Put)
                        {
                            Debug.Assert(postData != null, $"When you Put, you must supply postdata");

                            message = await client.PutAsync(url, content).ConfigureAwait(false);
                        }
                        else
                        {
                            Debug.Assert(false, $"Doesn't support this method : {method}");
                            message = null;
                        }
                        return message;
                    }
                }
            }
        }

        public static async Task<Tuple<bool, Stream>> DownloadContentsAsStreamAsync(String url, String etag = "", CookieContainer cookies = null)
        {
            Debug.Assert(url != null && url.Length > 0, "Url for download cannot be null");
            bool sucess = false;
            Stream result;
            try
            {
                using (var response = await GetResponseAsync(url, etag: etag, cookies: cookies).ConfigureAwait(false))
                {

                    if (response.IsSuccessStatusCode)
                    {

                        sucess = true;
                        result = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);

                        //                        IARLogStatic.Log("WebUtil", $"URL     : {url}");
                        //                        IARLogStatic.Log("WebUtil", $"Reponse : {result}");
                    }
                    else
                    {
                        sucess = false;
                        result = null;
                        IARLogStatic.Error("WebUtil", $"Failed to check", logs: new Dictionary<string, string>
                        {
                            { "Url", url },
                            { "Status code", response.StatusCode.ToString() },
                            { "Reason phrase", response.ReasonPhrase.ToString() }
                        });
                    }
                }
            }
            catch (InvalidOperationException ex)
            {
                IARLogStatic.Log("WebUtil", $"Might be 404 error for {url}, {ex.Message}");
                sucess = false;
                result = null;
            }
            catch (Exception ex)
            {
                IARLogStatic.Error("WebUtil", $"Failed to handle", ex, logs: new Dictionary<string, string>
                {
                    { "Url", url }
                });
                result = null;
            }
            return new Tuple<bool, Stream>(sucess, result);
        }
    }

    public static class HTTPResponseMessageHelper
    {
        public static String ETag(this HttpResponseMessage message)
        {
            string value = message.Headers?.ETag?.Tag.Replace("\"", "") ?? string.Empty;

            return value;
        }
    }
}
