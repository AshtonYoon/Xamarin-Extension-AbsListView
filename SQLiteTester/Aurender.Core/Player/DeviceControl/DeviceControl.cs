using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Aurender.Core.Utility;
using System.Diagnostics;

namespace Aurender.Core.Player.DeviceControl
{

    public static class DeviceControlUtility
    {
        public static async Task<(bool isSucess,  String responseString)> GetResponse(IAurenderEndPoint aurender, String path)
        {
            if (aurender == null)
                return (false, String.Empty);

            String url = aurender.WebURLFor(path);

            using (var response = await Utility.WebUtil.GetResponseAsync(url, timeoutSec: 10))
            {

                if (response != null)
                {
                    String result = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    return (response.IsSuccessStatusCode, result);
                }
                else
                {
                    return (false, String.Empty);
                }
            }
        }

        public static (bool hasMatchedGroup, String matchedString) GetMatchedString(MatchCollection matches, int index)
        {
            if (matches.Count > 0)
            {
                var match = matches[0];
                return GetMatchedString(match, index);
            }
            return (false, "");
        }

        public static (bool hasMatchedGroup, String matchedString) GetMatchedString(Match match, int index)
        {
            if (match.Groups.Count > index)
            {
                return (true, match.Groups[index].Value);
            }
            return (false, "");
        }


        public static async Task<(bool isSucess, String responseString)> SendNC(IAurenderEndPoint aurender, String command)
        {
            String path = $"php/nc?args={command.URLEncodedString()}";

            try
            {
                var result = await DeviceControlUtility.GetResponse(aurender, path).ConfigureAwait(false);

                if (result.isSucess)
                {
                    var expression = new Regex(@"<h2><br><pre>([\s\S]*?)</pre></h2>");
                    var mataches = expression.Matches(result.responseString);
                    if (mataches.Count > 0)
                    {
                        return (true, mataches[0].Groups[1].Value);
                    }
                    else
                        return (false, result.responseString);
                }
                return result;
            }
            catch(Exception er)
            {
                Debug.WriteLine(er.Message);
                return (false, "error");
            }
        }
    }

    

    public abstract class DeviceControlBase 
    {
        protected readonly IAurenderEndPoint aurender;

        protected DeviceControlBase(IAurender aAurender)
        {
            if (aAurender != null && aAurender.IsConnected())
                aurender = aAurender.ConnectionInfo;
            else
                aurender = null;
        }

        protected async Task<(bool isSucess,  String responseString)> GetResponse(String path)
        {
            return await DeviceControlUtility.GetResponse(this.aurender, path).ConfigureAwait(false);
        }


        protected async Task<(bool isSucess, String responseString)> SendNC(String command)
        {
            return await DeviceControlUtility.SendNC(this.aurender, command);
        }

        public abstract Task<bool> LoadInformation();
    }
}
