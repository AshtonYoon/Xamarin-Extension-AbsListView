using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

using Aurender.Core.Utility;

namespace Aurender.Core
{

}
namespace Aurender.Core.Player.mpd
{
    internal static class AurenderRegister
    {
        internal const String NOT_AN_AURENDER = "NOT_AURENDER";

        internal static async Task<String> GetAurenderMACAddress(String url)
        {
            String result = null;

            var regex = new Regex(@"([0-9a-f]{2}:[0-9a-f]{2}:[0-9a-f]{2}:[0-9a-f]{2}:[0-9a-f]{2}:[0-9a-f]{2})");

            var webData = await WebUtil.DownloadContentsAsync(url).ConfigureAwait(false);

            if (webData.Item1)
            {
                var match = regex.Match(webData.Item2);
                if (match.Success) {
                    result = match.Groups[1].Value;
                }
            }
            else
            {
                IARLogStatic.Error("Aurender.Connect", $"Failed to check Aurender MAC : {url}");
            }

            return result;
        }

        internal static async Task<AurenderPassCodeInfo> GetPasscode(String url)
        {
            bool registered = false;
            String passcode = null;


            var webData = await WebUtil.DownloadContentsAsync(url).ConfigureAwait(false);

            if (webData.Item1)
            {
                ParsePasscode(ref registered, ref passcode, webData.Item2);
            }
            else
            {
                IARLogStatic.Info("Aurender.Connect",
                    $"Looks like this is not aurender, since can't check registeration status. {url}");
                passcode = NOT_AN_AURENDER;
            }

            return new AurenderPassCodeInfo(registered, passcode);
        }

        internal static async Task<AurenderPassCodeInfo> RegisterRemoteToAurenderAsync(string url, String orgPasscode, String userPassCode)
        {
            IList<KeyValuePair<string, string>> postData = new List<KeyValuePair<String, String>>()
            {
                new KeyValuePair<string, string>("passcode", orgPasscode),
                new KeyValuePair<string, string>("user_input", userPassCode)
            };

            var result = await WebUtil.PostDataAndDownloadContentsAsync(url, postData).ConfigureAwait(false);


            var registered = false;
            var passcode = "";

            if (result.Item1)
            {
                ParsePasscode(ref registered, ref passcode, result.Item2);
            }
            else
            {
                passcode = NOT_AN_AURENDER;
            }

            return new AurenderPassCodeInfo(registered, passcode);
        }

        private static void ParsePasscode(ref bool registered, ref string passcode, String responseString)
        {
            var rgxPasscode = new Regex("<input *type=hidden *value=(\\d*) *name=\"passcode\" */>");
            var rgxRegistered = new Regex("Device registered, password for auRender is:");

            if (rgxRegistered.IsMatch(responseString))
            {
                registered = true;
            }
            else
            {
                var match = rgxPasscode.Match(responseString);

                if (match.Success)
                {
                    passcode = match.Groups[1].Value;
                    IARLogStatic.Log("Aurender.Connect", $"Got passcode : {passcode}");
                }
                else
                {
                    IARLogStatic.Info("Aurender.Connect",
                        $"Looks like this is not aurender, since can't check registeration status.");
                    passcode = NOT_AN_AURENDER;
                }
            }
        }



    }
}
