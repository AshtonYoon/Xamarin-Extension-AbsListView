using System.Threading.Tasks;

namespace Aurender.Core.Player.DeviceControl
{
    public class AurenderClone
    {
        private IAurenderEndPoint targetIP;

        public AurenderClone(IAurenderEndPoint targetIP)
        {
            this.targetIP = targetIP;
        }

        /// <summary>
        /// put ipv4 addresss
        /// </summary>
        /// <param name="sourceIPAddress"></param>
        /// <param name="targetIPAddress"></param>
        public async Task<bool> StartClone(string sourceIPAddress, string targetIPAddress)
        {
            var url = $"http://{targetIPAddress}:80/php/system?clone=1&source={sourceIPAddress}";

            var result = await DeviceControlUtility.GetResponse(targetIP, url);

            return result.isSucess;
        }
    }
}
