using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Aurender.Core.Player.DeviceControl
{

    public class AurenderStorageInfo : DeviceControlBase, INotifyPropertyChanged
    {
        public enum USBStorageStatus
        {
            NotConnected,
            Connected,
            CopyingDoneAndDisconnected,
            CopyingToInternal
        }

        public struct DiskInfo : INotifyPropertyChanged
        {
            private string name;
            private string path;
            private string status;
            private long freeSpace;
            private long totalSpace;

            public long UsedSpace => TotalSpace - FreeSpace;

            public string Name
            {
                get => name;
                set
                {
                    SetProperty(ref name, value);
                    OnPropertyChanged("Name");
                }
            }
            public string Path
            {
                get => path;
                set
                {
                    SetProperty(ref path, value);
                    OnPropertyChanged("Path");
                }
            }
            public string Status
            {
                get => status;
                set
                {
                    SetProperty(ref status, value);
                    OnPropertyChanged("Status");
                }
            }
            public long FreeSpace
            {
                get => freeSpace;
                set
                {
                    SetProperty(ref freeSpace, value);
                    OnPropertyChanged("FreeSpace");
                }
            }
            public long TotalSpace
            {
                get => totalSpace;
                set
                {
                    SetProperty(ref totalSpace, value);
                    OnPropertyChanged("TotalSpace");
                }
            }

            public event PropertyChangedEventHandler PropertyChanged;

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

            public static DiskInfo? DiskInfoFrom(Match match)
            {
                DiskInfo info = new DiskInfo();
                if (match.Groups.Count == 4)
                {
                    var regexForState = new Regex(@"drive state is: (.*)\s");

                    var statusMatch = regexForState.Match(match.Groups[1].Value);
                    if (statusMatch.Success)
                        info.Status = statusMatch.Groups[1].Value;

                    var capacityRegex = new Regex(@"/dev/sd(.)1\s*([0-9]*)\s*([0-9]*)\s*([0-9]*)\s*[0-9]*\%\s*/hdds/(.*)\s*");
                    var capacityMatch = capacityRegex.Match(match.Groups[3].Value);
                    var deviceID = DeviceControlUtility.GetMatchedString(capacityMatch, 1);

                    if (deviceID.hasMatchedGroup && deviceID.matchedString[0] > 'a')
                    {
                        int index = deviceID.matchedString[0] - 'a';
                        info.Name = $"HDD{index}";
                    }
                    
                    var total1K = DeviceControlUtility.GetMatchedString(capacityMatch, 2);
                    if (total1K.hasMatchedGroup)
                    {
                        long.TryParse(total1K.matchedString, out long totalSize);
                        info.TotalSpace = totalSize;
                    }

                    var free1K = DeviceControlUtility.GetMatchedString(capacityMatch, 4);
                    if (free1K.hasMatchedGroup)
                    {
                        long.TryParse(free1K.matchedString, out long totalSize);
                        info.FreeSpace = totalSize;
                    }
 
                    var path    = DeviceControlUtility.GetMatchedString(capacityMatch, 5);
                    if (path.hasMatchedGroup)
                    {
                        info.Path = path.matchedString;                        
                    }

                    if (info.Path.Equals("USB"))
                    {
                        return null;
                    }
                }
                return info;
            }

            public override string ToString()
            {
                var sb = new StringBuilder();
                sb.AppendLine();
                sb.AppendLine($"Disk Information for {Name}");
                sb.AppendLine($"\tPath    : {Path}");
                sb.AppendLine($"\tStatus  : {Status}");
                sb.AppendLine($"\tTotal   : {TotalSpace:n0}");
                sb.AppendLine($"\tUsed    : {UsedSpace:n0}");
                sb.AppendLine($"\tFree    : {FreeSpace:n0}");
                sb.AppendLine();
                return sb.ToString();
            }
        }

        #region fields
        private List<DiskInfo> hDDs;

        private USBStorageStatus uSBStatus;
        #endregion

        public List<DiskInfo> HDDs {
            get => hDDs;
            set
            {
                SetProperty(ref hDDs, value);
                OnPropertyChanged("HDDs");
            }
        }

        public USBStorageStatus USBStatus
        {
            get => uSBStatus;
            set
            {
                SetProperty(ref uSBStatus, value);
                OnPropertyChanged("USBStatus");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public AurenderStorageInfo(IAurender aurender) : base(aurender) { }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine();

            sb.AppendLine("Aurender Storage Info");
            foreach(var hdd in HDDs)
                sb.AppendFormat(hdd.ToString());

            sb.AppendFormat($"USB Status : {USBStatus}");
            sb.AppendLine();
            return sb.ToString();
        }

        public override async Task<bool> LoadInformation()
        {
            var storage = await GetResponse("/php/diskInfo").ConfigureAwait(false);
            var storageRegex = new Regex(@"<h3>([\s\S]*?)</h3><pre class=vn>([\s\S]*?)</pre><pre>([\s\S]*?)</pre>");

            List<DiskInfo> hdds = new List<DiskInfo>();
            if (storage.isSucess)
            {
                var matches = storageRegex.Matches(storage.responseString);
                
                foreach(Match match in matches)
                {
                    var info = DiskInfo.DiskInfoFrom(match);
                    if (info != null)
                        hdds.Add(info.Value);
                }
            }

            this.HDDs = hdds;

            var usbStorage = await GetResponse("/php/usbStatus?args=q").ConfigureAwait(false);
            var regex = new Regex("<p class='usb'>(/hdds/USB)");

            if (usbStorage.isSucess && regex.Match(usbStorage.responseString).Success)
            {
                var copyRegex = new Regex(@"Copying [\s\S]*?About to finish");

                this.USBStatus = USBStorageStatus.Connected;

                var copyPreparingRegex = new Regex("Comparing Start");
                if (copyRegex.Match(usbStorage.responseString).Success
                    || copyPreparingRegex.Match(usbStorage.responseString).Success)
                {
                    this.USBStatus = USBStorageStatus.CopyingToInternal;
                }
                else {
                    

                    var copyDoneRegex = new Regex("All done,device unmounted");
                    if (copyPreparingRegex.Match(usbStorage.responseString).Success)
                    {
                        this.USBStatus = USBStorageStatus.CopyingDoneAndDisconnected;
                    }
                }

                //String isAurenderCopying = await GetResponse("/php/usbStatus?args=st");
                //

            }
            else
            {
                this.USBStatus = USBStorageStatus.NotConnected;
            }
            return usbStorage.isSucess;
        }

        public async Task<bool> CopyAllUSBToInternalStorage()
        {
            String url = "/php/usbStatus?args=cp";

            var result = await GetResponse(url).ConfigureAwait(false);

            return result.isSucess;
        }

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
    }
}