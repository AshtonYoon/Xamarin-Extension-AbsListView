using System;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Aurender.Core.Player.mpd;
using Aurender.Core.Utility;
using System.Collections.Generic;

namespace Aurender.Core.Player.DeviceControl
{
    public struct NetworkInfo : INotifyPropertyChanged
    {
        private string iPAddress;
        private string gateway;
        private string netmask;
        private string mACAddress;
        private string nameserver1;
        private string nameserver2;

        public string IPAddress
        {
            get => iPAddress;
            set
            {
                SetProperty(ref iPAddress, value);
                OnPropertyChanged("IPAddress");
            }
        }
        public string Gateway
        {
            get => gateway;
            set
            {
                SetProperty(ref gateway, value);
                OnPropertyChanged("Gateway");
            }
        }
        public string Netmask
        {
            get => netmask;
            set
            {
                SetProperty(ref netmask, value);
                OnPropertyChanged("Netmask");
            }
        }
        public string MACAddress
        {
            get => mACAddress;
            set
            {
                SetProperty(ref mACAddress, value);
                OnPropertyChanged("MACAddress");
            }
        }
        public string Nameserver1
        {
            get => nameserver1;
            set
            {
                SetProperty(ref nameserver1, value);
                OnPropertyChanged("Nameserver1");
            }
        }
        public string Nameserver2
        {
            get => nameserver2;
            set
            {
                SetProperty(ref nameserver2, value);
                OnPropertyChanged("Nameserver2");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public bool IsSet() => (this.IPAddress != null && this.IPAddress.Length > 0);

        public void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public void SetProperty<T>(ref T field, T value, [CallerMemberName]string name = null)
        {
            if (!value.Equals(field))
            {
                field = value;
                OnPropertyChanged(name);
            }
        }
    };

    public class AurenderBasicInfo : DeviceControlBase, INotifyPropertyChanged
    {
        #region fields
        public string aurenderName;
        public event PropertyChangedEventHandler PropertyChanged;
        private NetworkInfo lan;
        private string defaultEncoding;
        private NetworkInfo wiFi;
        private string timeZone;
        private DateTime time;
        #endregion

        #region properties
        public NetworkInfo Lan
        {
            get => lan;
            private set
            {
                SetProperty(ref lan, value);
                OnPropertyChanged("LAN");
            }
        }

        public string DefaultEncoding
        {
            get => defaultEncoding;
            set
            {
                SetProperty(ref defaultEncoding, value);
                OnPropertyChanged("DefaultEncoding");
            }
        }
        
        public NetworkInfo WiFi
        {
            get => wiFi;
            set 
            {
                SetProperty(ref wiFi, value);
                OnPropertyChanged("WiFi");
            }
        }

        public string TimeZone
        {
            get => timeZone;
            set
            {
                SetProperty(ref timeZone, value);
                OnPropertyChanged("TimeZone");
            }
        }

        public DateTime Time
        {
            get => time;
            set
            {
                SetProperty(ref time, value);
                OnPropertyChanged("Time");
            }
        }

        public string AurenderName => aurenderName;

        #endregion

        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        protected void SetProperty<T>(ref T field, T value, [CallerMemberName]string name = null)
        {
            if (!value.Equals(field))
            {
                field = value;
                OnPropertyChanged(name);
            }
        }

        public AurenderBasicInfo(IAurender aurender) : base(aurender)
        {
            aurenderName = aurender != null && aurender.IsConnected()
                ? aurender.Name
                : string.Empty;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();

            sb.AppendLine();
            sb.AppendLine("AurenderBasicInfo");
            sb.AppendLine($"\tName     : {aurenderName}");

            if (this.Lan.IsSet())
            {
                sb.AppendLine($"\tLan      : {Lan.IPAddress}/{Lan.Netmask}/{Lan.Gateway}/{Lan.MACAddress}");
                sb.AppendLine($"\t   DNS   : {Lan.Nameserver1}, {Lan.Nameserver2}");
            }

            if (this.WiFi.IsSet())
            {
                sb.AppendLine($"\tWiFi     : {WiFi.IPAddress}/{WiFi.Netmask}/{WiFi.Gateway}/{WiFi.MACAddress}");
                sb.AppendLine($"\t   DNS   : {WiFi.Nameserver1}, {WiFi.Nameserver2}");
            }

            sb.AppendLine($"\tEncoding : {DefaultEncoding}");
            sb.AppendLine($"\tTimeZone : {TimeZone}");
            sb.AppendLine($"\tTime     : {Time}");

            return sb.ToString();
        }
        
        public override async Task<bool> LoadInformation()
        {
            var wifiInfo = await GetResponse("/php/network").ConfigureAwait(false);

            if (!wifiInfo.isSucess)
                return false;
            
            var netInfo = ParseNetworkInfo(wifiInfo.responseString);

            if (netInfo.interfaceName == "wlan0")
            {
                this.WiFi = netInfo.info;
                var eth0 = await GetResponse("/php/network?eth0=1").ConfigureAwait(false);

                if (!eth0.isSucess)
                    return false;

                var networkInfo = ParseNetworkInfo(eth0.responseString);
                this.Lan = networkInfo.info;
            }
            else
            {
                this.WiFi = new NetworkInfo();
                this.Lan = netInfo.info;
            }

            var timeZone = await GetResponse("php/getTime").ConfigureAwait(false);
            if (!timeZone.isSucess)
                return false;

            var regexTZ = new Regex(@"<p class=tz>(.*?)\s</p>\s*<p class=time>(.*?)\s</p>");

            var timeZoneInfo = DeviceControlUtility.GetMatchedString(regexTZ.Matches(timeZone.responseString), 1);

            this.Time = default(DateTime);
            if (timeZoneInfo.hasMatchedGroup)
            {
                this.TimeZone = timeZoneInfo.matchedString;

                timeZoneInfo = DeviceControlUtility.GetMatchedString(regexTZ.Matches(timeZone.responseString), 2);
                if (timeZoneInfo.hasMatchedGroup)
                {
                    var timeString = timeZoneInfo.matchedString;
                    var dateTime = DateTime.ParseExact(timeString, "yyyy-MM-dd H:mm:sszzz", null);

                    this.Time = dateTime;
                }
            }
            else
                this.TimeZone = "N/A";
            
            /// get encoding information
            var defaultEncoding = await GetResponse("php/locale?args=status").ConfigureAwait(false);
            if (!defaultEncoding.isSucess)
                return false;

            var regexEncoding = new Regex(@"<center>\s*<pre>(.*?)</pre></center>");

            var encodingInfo = DeviceControlUtility.GetMatchedString(regexEncoding.Matches(defaultEncoding.responseString), 1);

            if (encodingInfo.hasMatchedGroup)
            {
                var response = encodingInfo.matchedString;
                var code = response.Split(':')[0];

                DefaultEncoding = new CultureInfo(code).NativeName;
            }
            else
                DefaultEncoding = "N/A";

            return true;
        }
        
        public async Task<bool> SetTimeZone(string dwt, string timeZone)
        {
            String url = $"/php/setTime?dwt={dwt.URLEncodedString()}&tz={timeZone.URLEncodedString()}";
            var result = await GetResponse(url);

            if(result.isSucess)
                new AurenderScannerStatus(AurenderBrowser.GetCurrentAurender()).StartScan().ConfigureAwait(false);

            return result.isSucess;
        }

        public async Task<bool> SetLocale(String newLocale)
        {
            String url = $"/php/locale?args={newLocale}";
            var result = await GetResponse(url);

            if (result.isSucess)
            {
                return result.responseString.Split(':')[0] == "OK";
            }

            return false;
        }

        /// <summary>
        ///  You should display popup about the Aurender will be turned off and on.
        /// </summary>
        /// <param name="newname"></param>
        public async Task<bool> Rename(String newname)
        {
            List<KeyValuePair<String, String>> postData = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<String, String>("new_name", newname.URLEncodedString())
            };
            var url = AurenderBrowser.GetCurrentAurender().urlFor("/php/config");
            using (var response = await WebUtil.GetResponseByPostDataAsync(url, postData))
            {
                return response.IsSuccessStatusCode;
            }
        }

        public async Task<bool> TurnOff() => await TurnOff(this.aurender, false).ConfigureAwait(false);
        public async Task<bool> Restart() => await TurnOff(this.aurender, true).ConfigureAwait(false);

        public static async Task<bool> TurnOff(IAurenderEndPoint endPoint, bool restart)
        {
            var result = await DeviceControlUtility.SendNC(endPoint, restart ? "r" : "m");

            return result.isSucess;
        }
        
        private (String interfaceName, NetworkInfo info) ParseNetworkInfo(String text)
        {
            var networkResgex = new Regex(@"<td class=smr><pre>([\S\s]*?)</pre>.*?<td class=gw><pre>([\s\S]*?)</pre>[\s\S]*?<td class=dns><pre>nameserver (.*)\s*(nameserver )?(.*?)\s*</pre>");
            // var networkResgex = new Regex(@"<td class=smr><pre>([\S\s]*?)</pre>.*?<td class=gw><pre>(.*?)</pre>.*<td class=dns><pre>nameserver (.*)\snameserver (.*)\s</pre>");
            var matches = networkResgex.Matches(text);

            var interfaceInfo = DeviceControlUtility.GetMatchedString(matches, 1);

            var networkInfo = new NetworkInfo();

            if (interfaceInfo.hasMatchedGroup)
            {
                var informationRegex = new Regex(@"(\S*).*HWaddr\s*([0-9a-f:]*)([\s\S]*?)inet addr:([1-9\.]*)[\S\s]*?Bcast:([1-9\.]*)[\S\s]*?Mask:([0-9\.]*)");
                var informationMatches = informationRegex.Matches(interfaceInfo.matchedString);
                var match = DeviceControlUtility.GetMatchedString(informationMatches, 1);

                if (match.hasMatchedGroup)
                {
                    string name = match.matchedString;

                    networkInfo.MACAddress = DeviceControlUtility.GetMatchedString(informationMatches, 2).matchedString;
                    networkInfo.IPAddress = DeviceControlUtility.GetMatchedString(informationMatches, 4).matchedString;
                    networkInfo.Netmask = DeviceControlUtility.GetMatchedString(informationMatches, 6).matchedString;

                    String gatewayLine = DeviceControlUtility.GetMatchedString(matches, 2).matchedString;
                    var gatewayRegex = new Regex(@"^0.0.0.0 *([0-9\.]*) *");
                    var gateway = DeviceControlUtility.GetMatchedString(gatewayRegex.Matches(gatewayLine), 1).matchedString;

                    networkInfo.Gateway = gateway;

                    networkInfo.Nameserver1 = DeviceControlUtility.GetMatchedString(matches, 3).matchedString;
                    networkInfo.Nameserver2 = DeviceControlUtility.GetMatchedString(matches, 5).matchedString;


                    return (name, networkInfo);
                }
            }

            return ("N/A", networkInfo);
        }
    }
}