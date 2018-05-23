using System;
using System.Threading.Tasks;

namespace Aurender.Core.Player.DeviceControl
{
    public class IperfUtil : DeviceControlBase
    {
        public IperfUtil(IAurender aAurender) : base(aAurender)
        {

        }

        public override Task<bool> LoadInformation()
        {
            throw new NotImplementedException();
        }

        public async Task<bool> StartIperf()
        {
            var response = await GetResponse("/php/startIperf?args=start");
            if (!response.isSucess) return response.isSucess;

            return response.isSucess;
        }

        public async Task<bool> StopIperf()
        {
            var response = await GetResponse("php/startIperf?args=stop");
            if (!response.isSucess) return response.isSucess;

            return response.isSucess;
        }
    }
}
