using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Aurender.Core.Player.DeviceControl
{

    public class AurenderRemoteSupport : DeviceControlBase
    {
        public int RemoteSupportNumber
        {
            get;
            private set;
        }

        public string USBStatus
        {
            get;
            private set;
        }

       public AurenderRemoteSupport(IAurender aurender) : base(aurender) { }

        public override string ToString()
        {
            return $"Remote support status {aurender.ConnectionDescription()} : {RemoteSupportNumber}";
        }

        public override async Task<bool> LoadInformation()
        {
            var remoteResponse = await GetResponse("/php/raControl?args=status").ConfigureAwait(false);

            if (remoteResponse.isSucess)
                ParseRemoteSupportNumber(remoteResponse.responseString);

            var statusResponse = await GetResponse("/usbdac").ConfigureAwait(false);
            if (statusResponse.isSucess)
                ParseUSBStatus(statusResponse.responseString);

            return statusResponse.isSucess;
        }

        private void ParseUSBStatus(string result)
        {
            var regex = new Regex(@"<pre.*>(.*)\s*</pre>");
            var match = regex.Match(result);
            if (match != null && match.Groups.Count > 1 && match.Groups[1].Value != string.Empty)
                USBStatus = match.Groups[1].Value;
            else
                USBStatus = "N/A";
        }

        public async Task<bool> Stop()
        {
            var result = await GetResponse("/php/raControl?args=stop").ConfigureAwait(false);

            return result.isSucess;
        }
        public async Task<bool> Start()
        {
            var result = await GetResponse("/php/raControl?args=restart").ConfigureAwait(false);

            if (result.isSucess) 
              ParseRemoteSupportNumber(result.responseString);

            return result.isSucess;
        }

        private void ParseRemoteSupportNumber(String result)
        {
            this.RemoteSupportNumber = 0;
            var regex = new Regex(@"<pre class=raControl>Remote assist enabled : ([1-9]*)\s*</pre>");
            var match = regex.Match(result);
            if (match != null && match.Groups.Count > 1)
            {
                String number = match.Groups[1].Value;

                int.TryParse(number, out int port);
                this.RemoteSupportNumber = port;
            }
        }
    }

}