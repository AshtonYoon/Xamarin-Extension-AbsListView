using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Aurender.Core.Utility
{
    public static class FileDownloader
    {
        //public static async Task<bool> CreateDownloadTask(String url, String targetFilePath, Action<Int64, Int64> progressReport)
        //{
        //    bool success = false;
        //    var client = new HttpClient(new NativeMessageHandler());

        //    var totalBytesRead = 0L;
        //    var readCount = 0L;
        //    var buffer = new byte[8192 * 4];
        //    var isMoreToRead = true;
        //    var totalDownloadSize = 0L;
        //    var folderPaht = System.IO.Path.GetDirectoryName(targetFilePath);


        //    using (var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead))
        //    {
        //        // You must use as stream to have control over buffering and number of bytes read/received
        //        using (var contentStream = await response.Content.ReadAsStreamAsync())
        //        {
        //            totalDownloadSize = response.Content.Headers.ContentLength ?? 0;

        //            var fileName = Path.GetFileName(targetFilePath);
        //            IFolder folder = await FileSystem.Current.GetFolderFromPathAsync(folderPaht);
        //            IFile file = await folder.CreateFileAsync(fileName, CreationCollisionOption.ReplaceExisting);

        //            IARLogStatic.Info("Download", $"Open file    +++++++++++++++++++++ [{fileName}]");
        //            using (Stream fileStream = await file.OpenAsync(PCLStorage.FileAccess.ReadAndWrite))
        //            {
        //                try
        //                {
        //                    do
        //                    {
        //                        var bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false);
        //                        if (bytesRead == 0)
        //                        {
        //                            isMoreToRead = false;
        //                            continue;
        //                        }
        //                        //IARLogStatic.Info("Download", $"read {totalBytesRead}");
        //                        progressReport?.BeginInvoke(totalDownloadSize, totalBytesRead, null, null);

        //                        await fileStream.WriteAsync(buffer, 0, bytesRead).ConfigureAwait(false);

        //                        totalBytesRead += bytesRead;
        //                        readCount += 1;
        //                    }
        //                    while (isMoreToRead);

        //                    progressReport?.BeginInvoke(totalDownloadSize, totalBytesRead, null, null);
        //                    fileStream.Flush();
        //                }
        //                catch (Exception ex)
        //                {
        //                    IARLogStatic.Error("Download", "Failed to download.", ex);
        //                }
        //                finally
        //                {
        //                    success = (totalBytesRead == totalDownloadSize);
        //                }
        //            }
        //            IARLogStatic.Info("Download", $"Close file    -------------------- [{fileName}]");

        //        }
        //    }


        //    IARLogStatic.Info("Download", $"Source : {url}");
        //    IARLogStatic.Info("Download", $"Target : {targetFilePath}");
        //    IARLogStatic.Info("Download", $"Result : {success}");
        //    if (!success)
        //    {
        //        IARLogStatic.Error("Download", $"Failed to download {url}");
        //    }
        //    return success;
        //}

        public static async Task<bool> CreateDownloadTask(string urlToDownload, String targetFilePath, Action<Int64, Int64> progressReport)
        {
            bool success = false;

            int receivedBytes = 0;
            long totalBytes = 0;

            using (var handler = new HttpClientHandler())
            using (var httpClient = new HttpClient(handler))
            using (var response = await httpClient.GetAsync(urlToDownload, HttpCompletionOption.ResponseHeadersRead))
            //using (var contentStream = await response.Content.ReadAsStreamAsync())
            using (var client = new WebClient())
            using (var streamForRead = await client.OpenReadTaskAsync(urlToDownload))
            using (Stream fileStream = File.Open(targetFilePath, FileMode.Create, System.IO.FileAccess.Write))
            {
                try
                {
                    byte[] buffer = new byte[1048576];
                    totalBytes = response.Content.Headers.ContentLength ?? 0;
                    while (true)
                    {
                        int bytesRead = await streamForRead.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false);
                        if (bytesRead == 0) break;

                        Debug.WriteLine($"read : {bytesRead} / {receivedBytes}");

                        receivedBytes += bytesRead;
                        await fileStream.WriteAsync(buffer, 0, bytesRead).ConfigureAwait(false);
                        //await streamForWrite.WriteAsync(buffer, 0, bytesRead).ConfigureAwait(false);
                        progressReport?.Invoke(totalBytes, receivedBytes);
                    }
                }
                finally
                {
                    success = (receivedBytes == totalBytes);
                    await streamForRead.FlushAsync().ConfigureAwait(false);
                    await fileStream.FlushAsync().ConfigureAwait(false);
                    //await contentStream.FlushAsync().ConfigureAwait(false);
                }
            }
            return success;
        }
    }
}
