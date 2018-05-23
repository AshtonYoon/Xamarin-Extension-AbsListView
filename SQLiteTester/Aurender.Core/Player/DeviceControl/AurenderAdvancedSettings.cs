using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Aurender.Core.Player.DeviceControl
{
    public class AurenderAdvancedSettings : DeviceControlBase, INotifyPropertyChanged
    {
        public enum AurenderModel
        {
            S10,
            A10,
            N10,
            W20,
            Others
        }

        public AurenderModel aurenderModel;

        private string modelName = "";

        private int secForChangeSampleRate;
        private bool spdifModePro;
        private bool fadeInOut;
        private bool supportDualWire;
        private bool supportWordClock;
        private bool userSetDualWire;
        private bool userSetWordClock;
        private int workingClockRate;
        private bool workingClockLocked;
        private bool workingClockMatached;
        private bool workingDualWire;
        private bool audioUSBPower;

        private int dsd2pcm;
        private int dsd2pcmOutput;
        private int dsd2pcmFilter;
        private int samplingRateTime;

        private string a10volume; 

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

        public AurenderAdvancedSettings(IAurender aurender) : base(aurender)
        {
            
        }

        public override async Task<bool> LoadInformation()
        {
            // TO DO: 모델별로 파싱할 데이터 선택

            //Get model
            var Modelresponse = await GetResponse("php/model").ConfigureAwait(false);
            if (!Modelresponse.isSucess)
                return false;

            ModelName = ParseModelName(Modelresponse.responseString);
            aurenderModel = CheckingExtensionable(ModelName.Replace("Aurender ", string.Empty));

            switch (aurenderModel)
            {
                case AurenderModel.S10:
                    break;
                case AurenderModel.A10:
                    var volumeResponse = await GetResponse("/php/setA10Volume").ConfigureAwait(false);
                    if (!volumeResponse.isSucess)
                        return false;

                    A10volume = ParseA10Volume(volumeResponse.responseString);
                    break;
                case AurenderModel.N10:
                    break;
                case AurenderModel.W20:
                    break;
                case AurenderModel.Others:
                    break;
                default:
                    break;
            }

            // Advanced
            var response = await GetResponse("php/getAudioStatus").ConfigureAwait(false);
            if (!response.isSucess)
                return false;
            
            var advancedInfo = ParseAdvancedInfo(response.responseString);

            //PCM
            var PCMresponse = await GetResponse("php/nc?args=%").ConfigureAwait(false);
            if (!PCMresponse.isSucess)
                return false;

            var PCMInfo = ParsePCMInfo(PCMresponse.responseString);
            
            //Sampling rate time
            var SamplingResponse = await GetResponse("/php/pauseConfig").ConfigureAwait(false);
            if (!SamplingResponse.isSucess)
                return false;

            var regex = new Regex(@"<pre class='pause'>(.*?)</pre>");
            var time = regex.Match(SamplingResponse.responseString).Groups[1].Value;
            int.TryParse(time, out int value);
            samplingRateTime = value;

            return true;
        }

        private string ParseModelName(string responseString)
        {
            var regex = new Regex(@"Aurender\s(.*)");
            var matches = regex.Matches(responseString);

            return matches[1].Groups[1].Value;
        }

        private string ParseA10Volume(string responseString)
        {
            var regex = new Regex("<p class='output'>(.*)\n</p>");
            var matches = regex.Matches(responseString);

            return matches[0].Groups[1].Value;
        }

        public AurenderModel CheckingExtensionable(string modelName)
        {
            if (Enum.TryParse(modelName, out AurenderModel model) && Enum.IsDefined(typeof(AurenderModel), model))
                return model;

            return AurenderModel.Others;
        }

        private string ParsePCMInfo(string text)
        {
            var regex = new Regex(@"D2PEn : ([012]), D2PFreq : ([0-9]*), D2PFilter : ([0-9]*)");
            var matches = regex.Matches(text);

            Dsd2pcm
                = int.Parse(DeviceControlUtility.GetMatchedString(matches, 1).matchedString);

            Dsd2pcmOutput
                = int.Parse(DeviceControlUtility.GetMatchedString(matches, 2).matchedString);

            Dsd2pcmFilter
                = int.Parse(DeviceControlUtility.GetMatchedString(matches, 3).matchedString);

            return string.Empty;
        }

        private string ParseAdvancedInfo(string text)
        {
            var regex = new Regex(@"SRC:([0-9]*) [|] SPDIF:([01]) [|] FIO:([01]) [|] SDW:([01]) [|] SMC:([01]) [|] UDW:([01]) [|] UWCM:([01]) [|] WCR:([0-9]*) [|] WCL:([01]) [|] WCM:([01]) [|] WDW:([01])");
            var matches = regex.Matches(text);

            SecForChangeSampleRate
                = int.Parse(DeviceControlUtility.GetMatchedString(matches, 1).matchedString);

            SpdifModePro
                = (DeviceControlUtility.GetMatchedString(matches, 2).matchedString == "1");

            FadeInOut
                = (DeviceControlUtility.GetMatchedString(matches, 3).matchedString == "1");

            SupportDualWire
                = (DeviceControlUtility.GetMatchedString(matches, 4).matchedString == "1");

            SupportWordClock
                = (DeviceControlUtility.GetMatchedString(matches, 5).matchedString == "1");

            UserSetDualWire
                = (DeviceControlUtility.GetMatchedString(matches, 7).matchedString == "1");

            UserSetWordClock
                = (DeviceControlUtility.GetMatchedString(matches, 7).matchedString == "1");

            WorkingClockRate
                = int.Parse(DeviceControlUtility.GetMatchedString(matches, 8).matchedString);

            WorkingClockLocked
                = (DeviceControlUtility.GetMatchedString(matches, 9).matchedString == "1");

            WorkingClockMatached
                = (DeviceControlUtility.GetMatchedString(matches, 10).matchedString == "1");

            WorkingDualWire
                = (DeviceControlUtility.GetMatchedString(matches, 11).matchedString == "1");

            var usbRegex = new Regex(@"[|] AUP:([01])");
            var usbMatches = usbRegex.Matches(text);

            AudioUSBPower
                = (DeviceControlUtility.GetMatchedString(usbMatches, 1).matchedString == "1");

            return string.Empty;
        }

        #region Sampling Rate
        public async Task<bool> SetSamplingRateTime(int sec)
        {
            string url = $"/php/pauseConfig?interval={sec}";
            var result = await GetResponse(url).ConfigureAwait(false);

            return result.isSucess;
        }
        #endregion 

        #region Fade In Out
        public async Task<bool> SetFadeIn(bool use)
        {
            string command = use ? "s" : "r";
            string url = $"php/setPC?op={command}";
            var result = await GetResponse(url).ConfigureAwait(false);

            return result.isSucess;
        }
        #endregion

        #region Volume
        public async Task<bool> SetDirectOutput()
        {
            string url = "/php/setA10Volume?type=direct";
            var result = await GetResponse(url).ConfigureAwait(false);

            return result.isSucess;
        }

        public async Task<bool> SaveLastVolume()
        {
            string url = "/php/setA10Volume?type=0";
            var result = await GetResponse(url).ConfigureAwait(false);

            return result.isSucess;
        }

        public async Task<bool> SetLastVolume(int volume)
        {
            string url = $"/php/setA10Volume?type={volume}";
            var result = await GetResponse(url).ConfigureAwait(false);

            return result.isSucess;
        }
        #endregion

        #region Merging Technology
        public bool ProfessionalMode { get => false; set => throw new NotImplementedException(); }
        #endregion

        #region DSD to PCM

        public async Task<bool> ConvertToPCM(bool on, bool is88_2kHZ, string _c = "0")
        {
            var a = on ? "1" : "0";
            var b = is88_2kHZ ? "0" : "1";
            var c = _c;
            var command =  "&" + a + b + c;

            var result = await DeviceControlUtility.SendNC(base.aurender, command);

            return result.isSucess;
        }

        public async Task<bool> WordClockMode(bool on)
        {
            var command = $"!{(on ? "1" : "0")}";
            var result = await DeviceControlUtility.SendNC(base.aurender, command);

            return result.isSucess;
        }

        public async Task<bool> SetDualWire(bool on)
        {
            var command = "=" + (on ? "1" : "0");
            var result = await DeviceControlUtility.SendNC(base.aurender, command);
            
            return result.isSucess;
        }
        #endregion

        #region Dual Wire
        public bool DualWire { get => false; set => throw new NotImplementedException(); }
        #endregion

        public async Task<bool> SetSPDIFModePro(bool on)
        {
            var command = "g" + (on ? "0" : "1");
            var result = await DeviceControlUtility.SendNC(base.aurender, command);

            return result.isSucess;
        }

        public string ModelName { get => modelName; set => modelName = value; }

        public int SecForChangeSampleRate
        {
            get => secForChangeSampleRate;
            set
            {
                SetProperty(ref secForChangeSampleRate, value);
                OnPropertyChanged("SecForChangeSampleRate");
            }
        }
        public bool SpdifModePro
        {
            get => spdifModePro;
            set
            {
                SetProperty(ref spdifModePro, value);
                OnPropertyChanged("SpdifModePro");
            }
        }
        public bool FadeInOut
        {
            get => fadeInOut;
            set
            {
                SetProperty(ref fadeInOut, value);
                OnPropertyChanged("FadeInOut");
            }
        }
        public bool SupportDualWire
        {
            get => supportDualWire;
            set
            {
                SetProperty(ref supportDualWire, value);
                OnPropertyChanged("SupportDualWire");
            }
        }
        public bool SupportWordClock
        {
            get => supportWordClock;
            set
            {
                SetProperty(ref supportWordClock, value);
                OnPropertyChanged("SupportWordClock");
            }
        }
        public bool UserSetDualWire
        {
            get => userSetDualWire;
            set
            {
                SetProperty(ref userSetDualWire, value);
                OnPropertyChanged("UserSetDualWire");
            }
        }
        public bool UserSetWordClock
        {
            get => userSetWordClock;
            set
            {
                SetProperty(ref userSetWordClock, value);
                OnPropertyChanged("UserSetWordClock");
            }
        }
        public int WorkingClockRate
        {
            get => workingClockRate;
            set
            {
                SetProperty(ref workingClockRate, value);
                OnPropertyChanged("WorkingClockRate");
            }
        }
        public bool WorkingClockLocked
        {
            get => workingClockLocked;
            set
            {
                SetProperty(ref workingClockLocked, value);
                OnPropertyChanged("WorkingClockLocked");
            }
        }
        public bool WorkingClockMatached
        {
            get => workingClockMatached;
            set
            {
                SetProperty(ref workingClockMatached, value);
                OnPropertyChanged("WorkingClockMatached");
            }
        }
        public bool WorkingDualWire
        {
            get => workingDualWire;
            set
            {
                SetProperty(ref workingDualWire, value);
                OnPropertyChanged("WorkingDualWire");
            }
        }
        public bool AudioUSBPower
        {
            get => audioUSBPower;
            set
            {
                SetProperty(ref audioUSBPower, value);
                OnPropertyChanged("AudioUSBPower");
            }
        }
        public int Dsd2pcm
        {
            get => dsd2pcm;
            set
            {
                SetProperty(ref dsd2pcm, value);
                OnPropertyChanged("Dsd2pcm");
            }
        }
        public int Dsd2pcmOutput
        {
            get => dsd2pcmOutput;
            set
            {
                SetProperty(ref dsd2pcmOutput, value);
                OnPropertyChanged("Dsd2pcmOutput");
            }
        }

        public int Dsd2pcmFilter
        {
            get => secForChangeSampleRate;
            set
            {
                SetProperty(ref dsd2pcmFilter, value);
                OnPropertyChanged("Dsd2pcmFilter");
            }
        }

        public int SamplingRateTime
        {
            get => samplingRateTime;
            set
            {
                SetProperty(ref samplingRateTime, value);
                OnPropertyChanged("SamplingRateTime");
            }
        }

        public string A10volume
        {
            get => a10volume;
            set
            {
                SetProperty(ref a10volume, value);
                OnPropertyChanged("A10volume");
            }
        }
    }
}
