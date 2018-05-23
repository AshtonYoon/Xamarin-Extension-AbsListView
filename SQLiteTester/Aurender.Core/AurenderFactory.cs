using System;
using System.Threading.Tasks;
using Aurender.Core.Player;
using Aurender.Core.Player.mpd;
using Aurender.Core.Utility;

namespace Aurender.Core
{
    public static class AurenderFactory
    {
        public static IAurender CreateManually(String name, String IPV4, Int32 port = 12019, Int32 webPort = 80)
        {
            IAurenderEndPoint end = new AurenderEndPoint(name, IPV4);

            return Create(end);
        }

        public static IAurender Create(IAurenderEndPoint end)
        {
            IAurender aurender = new Aurender(end);

            return aurender;
        }

        public static async Task<string> GetAurenderName(string IPAddress, int port=12019)
        {
            var result = await WebUtil.DownloadContentsAsync($"http://{IPAddress}/php/whatIsYourName", timeout: 0.5).ConfigureAwait(false);

            if (result.Item1)
            {
                var name = result.Item2.Replace("\n", string.Empty);
                return name;
            }
            else
                return string.Empty;
        }

        /// <summary>
        /// Once users select a aurender to connect, please set here.
        /// So all framework can refer the current unit.
        /// </summary>
        //public static Func<IAurender> Current;

        public static Func<IAurenderEndPoint, IAurender> Instantiator;
    }

    public static class IAurenderExt
    {
        public static String urlFor(this IAurender aurender, string url)
        {
            String fullURL = $"http://{aurender.ConnectionInfo.IPV4Address}:{aurender.ConnectionInfo.WebPort}/{url}";

            return fullURL;
        }
    }
}
