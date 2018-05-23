using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Aurender.Core.Utility;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Aurender.Core.Player.DeviceControl
{
    public class AurenderAMOLEDControl : DeviceControlBase, INotifyPropertyChanged
    {
        public enum FrontUI : byte
        {
            MainUI,
            OrangeMeter,
            BlueMeter,
            Playlist,
            ACSHome,
            AurenderStatus
        }

        private FrontUI activeUI;
        private byte brightness;
        private bool isListeningModeSet;

        public FrontUI ActiveUI
        {
            get => activeUI;
            set
            {
                SetProperty(ref activeUI, value);
                OnPropertyChanged("ActiveUI");
            }
        }
        public byte Brightness
        {
            get => brightness;
            set
            {
                SetProperty(ref brightness, value);
                OnPropertyChanged("Brightness");
            }
        }
        public bool IsListeningModeSet
        {
            get => isListeningModeSet;
            set
            {
                SetProperty(ref isListeningModeSet, value);
                OnPropertyChanged("IsListeningModeSet");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

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

        public AurenderAMOLEDControl(IAurender aurender) : base(aurender) { }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine();

            sb.AppendLine("Aurender AMOLED Info");

            sb.AppendFormat($"\tActive UI     : {ActiveUI}");
            sb.AppendFormat($"\tBrightness    : {Brightness:n0}");
            String lmStatus = IsListeningModeSet ? "Set" : "Not set";
            sb.AppendFormat($"\tListeningMode : {lmStatus}");
            sb.AppendLine();
            return sb.ToString();
        }

        public override async Task<bool> LoadInformation()
        {
            var status = await GetResponse("/php/getAStatus").ConfigureAwait(false);

            var currentUI = await GetResponse("/php/whichUI").ConfigureAwait(false);
            var activUIRegex = new Regex(@"<body>\s*(.*?)\s*</body>");
            if (currentUI.isSucess)
            {
                activUIRegex.Match(currentUI.responseString);
            }

            ParseBrightness(await SendNC("B").ConfigureAwait(false));

            var listeningMode = await GetResponse("/php/getLM").ConfigureAwait(false);
            if (listeningMode.isSucess)
            {
                bool.TryParse(listeningMode.responseString, out bool lm);
                this.IsListeningModeSet = lm;
            }

            return listeningMode.isSucess;
        }

        private void ParseBrightness((bool isSucess, string responseString) response)
        {
            if (response.isSucess)
            {
                var brightnessRegex = new Regex(@"BRIGHTNESS :(\d+)");
                var result = brightnessRegex.Match(response.responseString);
                var value = result.Groups[1].Value;

                byte.TryParse(value, out brightness);
            }
        }

        public async Task<bool> SetListeningMode(bool on)
        {
            String command = on ? "s" : "c";
            String url = $"getLM?op={command}";
            var result = await GetResponse(url).ConfigureAwait(false);

            return result.isSucess;
        }

        public async Task<bool> SetBrightness(byte brightness)
        {
            String url = $"b{brightness:D3}";
            var result = await SendNC(url).ConfigureAwait(false);

            return result.isSucess;
        }

        public async Task<bool> ShowUI(FrontUI uiType)
        {
            String name = "";

            switch (uiType)
            {
                case FrontUI.OrangeMeter:
                    name = "auMeterOrange";
                    break;
                case FrontUI.BlueMeter:
                    name = "auMeterBlue";
                    break;
                case FrontUI.Playlist:
                    name = "qtpl";
                    break;
                case FrontUI.ACSHome:
                    name = "ACSHome";
                    break;
                case FrontUI.AurenderStatus:
                    name = "auStatus";
                    break;
                case FrontUI.MainUI:
                default:
                    name = "aurenderUI";
                    break;
            }

            String url = $"php/showUI?ui={name}";
            var result = await GetResponse(url).ConfigureAwait(false);

            return result.isSucess;
        }
    }
}