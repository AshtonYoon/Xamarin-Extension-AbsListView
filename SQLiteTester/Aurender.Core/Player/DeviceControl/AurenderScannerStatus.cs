using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Aurender.Core.Utility;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Aurender.Core.Player.DeviceControl
{
    public class AurenderScannerStatus : DeviceControlBase, INotifyPropertyChanged
    {
        public enum ScanMode
        {
            Clean,
            CleanWithImageClear,
            CleanWithDataClear,
            CleanWithImageAndDataClear
        }

        public AurenderScannerStatus(IAurender aurender) : base(aurender) { }

        private string status;

        public bool IsComplete { get; private set; }
        public bool IsPaused { get; private set; }

        private IDictionary<String, List<String>> parseFailedFiles;
        private bool useHDCover;
        private string scanningPath;
        private int scanFileCount;
        private int ellapsedScanTime;
        private string startTime;
        private string endTime;
        private string averageSpeed;
        private string dBVersion;
        private ScanMode currentCleanMode;

        public string Status
        {
            get => status;
            set
            {
                SetProperty(ref status, value);
                OnPropertyChanged("Status");
            }
        }

        public IDictionary<string, List<string>> ParseFailedFiles
        {
            get => parseFailedFiles;
            set
            {
                SetProperty(ref parseFailedFiles, value);
                OnPropertyChanged("ParseFailedFiles");
            }
        }

        public bool UseHDCover
        {
            get => useHDCover;
            set
            {
                SetProperty(ref useHDCover, value);
                OnPropertyChanged("UseHDCover");
            }
        }

        public string ScanningPath
        {
            get => scanningPath;
            set
            {
                SetProperty(ref scanningPath, value);
                OnPropertyChanged("ScanningPath");
            }
        }

        public int FileCount { get; set; }

        public int ScanFileCount
        {
            get => scanFileCount;
            set
            {
                SetProperty(ref scanFileCount, value);
                OnPropertyChanged("ScanFileCount");
            }
        }

        public int EllapsedScanTime
        {
            get => ellapsedScanTime;
            set
            {
                SetProperty(ref ellapsedScanTime, value);
                OnPropertyChanged("EllapsedScanTime");
            }
        }

        public string StartTime
        {
            get => startTime;
            set
            {
                SetProperty(ref startTime, value);
                OnPropertyChanged("StartTime");
            }
        }

        public string WillEndTime { get; private set; }

        public string EndTime
        {
            get => endTime;
            set
            {
                SetProperty(ref endTime, value);
                OnPropertyChanged("EndTime");
            }
        }

        public string AverageSpeed
        {
            get => averageSpeed;
            set
            {
                SetProperty(ref averageSpeed, value);
                OnPropertyChanged("AverageSpeed");
            }
        }

        public string DBVersion
        {
            get => dBVersion;
            set
            {
                SetProperty(ref dBVersion, value);
                OnPropertyChanged("DBVersion");
            }
        }

        public ScanMode CurrentCleanMode
        {
            get => currentCleanMode;
            set
            {
                SetProperty(ref currentCleanMode, value);
                OnPropertyChanged("CurrentScanMode");
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

        public override async Task<bool> LoadInformation()
        {
            var result = await GetResponse("/php/scanStatus").ConfigureAwait(false);
            ParseScanStatusInfo(result.responseString);

            result = await GetResponse("/php/coverArtSize?args=q");
            UseHDCover = result.isSucess;
            return result.isSucess;
        }

        private void ParseScanStatusInfo(string responseString)
        {
            var running = "Scanner is Running";
            var notRunning = "Scanner is NOT Running";

            IsComplete = responseString.Contains(notRunning);
            IsPaused = false;

            if (responseString.Contains(running))
                ParseWhenRunning(responseString);

            else if(responseString.Contains(notRunning))
                ParseWhenNotRunning(responseString);
        }
        
        private void ParseWhenRunning(string responseString)
        {
            var regex = new Regex(@"<pre class=status>(.*)\s(.*) of (.*) scanned \s\S?Speed: (.*)\s\S?Start at (.*) Will Complete in about (.*)\s\S?Clean Mode:(.*)\s\S?Broken Files:(.*)");
            var matches = regex.Matches(responseString);

            Status = DeviceControlUtility.GetMatchedString(matches, 1).matchedString;

            var pause = "PAUSED";
            if (responseString.Contains(pause))
            {
                status = pause;
                IsPaused = true;
            }

            int.TryParse(DeviceControlUtility.GetMatchedString(matches, 2).matchedString, out int count);
            ScanFileCount = count;

            int.TryParse(DeviceControlUtility.GetMatchedString(matches, 3).matchedString, out count);
            FileCount = count;

            StartTime = DeviceControlUtility.GetMatchedString(matches, 5).matchedString;
            WillEndTime = DeviceControlUtility.GetMatchedString(matches, 6).matchedString;

            //CurrentCleanMode = DeviceControlUtility.GetMatchedString(matches, 7).matchedString;
        }

        private void ParseWhenNotRunning(string responseString)
        {
            var regex = new Regex(@"<pre class=status>(.*),(.*) files in (.*) seconds\s\S?from\s?(.*) to (.*)\s\S?Avg speed: (.*)\s\S?DB version:(.*)\s\S?Clean Mode:(.*)\s\S?Broken Files:(.*)");
            var matches = regex.Matches(responseString);

            Status = DeviceControlUtility.GetMatchedString(matches, 1).matchedString;

            int.TryParse(DeviceControlUtility.GetMatchedString(matches, 2).matchedString, out int count);
            ScanFileCount = count;
            FileCount = count;

            int.TryParse(DeviceControlUtility.GetMatchedString(matches, 3).matchedString, out int time);
            EllapsedScanTime = time;

            StartTime = DeviceControlUtility.GetMatchedString(matches, 4).matchedString;
            EndTime = DeviceControlUtility.GetMatchedString(matches, 5).matchedString;

            AverageSpeed = DeviceControlUtility.GetMatchedString(matches, 6).matchedString;

            DBVersion = DeviceControlUtility.GetMatchedString(matches, 7).matchedString;
        }

        public async Task<bool> SetUseHDCover(bool useHDCover)
        {
            this.UseHDCover = useHDCover;

            string url;
            if (useHDCover)
                url = "php/coverArtSize?set=2X";
            else
                url = "php/coverArtSize?set=1X";

            var result = await GetResponse(url).ConfigureAwait(false);

            return result.isSucess;
        }

        public async Task<bool> ScanForUpdate(String path = "")
        {
            string url = $"/php/update";
            if (path.Length > 0)
            {
                url = $"/php/update?p={path.URLEncodedString()}";
            }
            var result = await GetResponse(url).ConfigureAwait(false);

            return result.isSucess;
        }

        public async Task<bool> ScanAfterClear(ScanMode mode = ScanMode.Clean)
        {
            string path = GetClearOption(mode);
            var result = await GetResponse(path).ConfigureAwait(false);

            return result.isSucess;
        }

        private string GetClearOption(ScanMode mode)
        {
            string path = string.Empty;

            switch (mode)
            {
                case ScanMode.CleanWithImageClear:
                    path = "/php/force=1&image=1";
                    break;
                case ScanMode.CleanWithDataClear:
                    path = "/php/force=1&data=1";
                    break;
                case ScanMode.CleanWithImageAndDataClear:
                    path = "/php/force=1&cache=1";
                    break;
                default:
                case ScanMode.Clean:
                    path = "/php/force=1&rMaLl=1";
                    break;
            }

            return path;
        }

        public async Task<bool> PauseScan()
        {
            String url = "/php/update?rs=-s";
            var result = await GetResponse(url).ConfigureAwait(false);

            return result.isSucess;

        }

        public async Task<bool> ResumeScan()
        {
            String url = "/php/update?rs=-r";
            var result = await GetResponse(url).ConfigureAwait(false);

            return result.isSucess;
        }

        public async Task<bool> KillScanner()
        {
            String url = "/php/update?k=1";

            var result = await GetResponse(url).ConfigureAwait(false);

            return result.isSucess;
        }

        public async Task<bool> StartScan()
        {
            var baseurl = "/php/";

            string option = GetClearOption(CurrentCleanMode);
            if (option.StartsWith(baseurl))
                option = option.Remove(0, 5);

            string url = "/php/update?" + option;

            var result = await GetResponse(url).ConfigureAwait(false);

            return result.isSucess;
        }
    }
}