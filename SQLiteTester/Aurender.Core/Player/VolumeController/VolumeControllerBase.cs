using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text.RegularExpressions;
using Aurender.Core;
using Aurender.Core.Contents;
using Aurender.Core.Contents.Streaming;
using System.ComponentModel;

namespace Aurender.Core.Player.VolumeController
{

    public static class VolumeControllerFactory
    {
        public static IVolumeController GetVolumeController(this IAurender aurender)
        {
            IVolumeController result = null;

            String url = aurender.ConnectionInfo.WebURLFor("php/getCompatibleAmp");

            using (var response = Utility.WebUtil.GetResponseAsync(url))
            {
                response.Wait();
                using (var res = response.Result)
                {

                    if (response.IsCompleted && response.Result != null && response.Result.IsSuccessStatusCode)
                    {
                        string resultString = response.Result.Content.ReadAsStringAsync().Result;

                        Regex linkFinder = new Regex(@"<p class='link'>(.*)</p>");

                        var match = linkFinder.Match(resultString);

                        if (match.Success)
                        {
                            var link = match.Groups[1].Value;

                            switch (link)
                            {
                                case "ampX7xx":
                                    result = new VolumeControllerForX725(aurender.ConnectionInfo, link);
                                    break;

                                case "ampA10":
                                    result = new VolumeControllerForA10(aurender.ConnectionInfo, link);
                                    break;

                                case "ampGoldMund":
                                    result = new VolumeControllerForGoldmund(aurender.ConnectionInfo, link);
                                    break;

                                case "ampMSB":
                                    result = new VolumeControllerForMSB(aurender.ConnectionInfo, link);
                                    break;

                                case "ampConstel":
                                    result = new VolumeControllerForConstellation(aurender.ConnectionInfo, link);
                                    break;

                                case "ampNADAC":
                                    result = new VolumeControllerForNADAC(aurender.ConnectionInfo, link);
                                    break;

                                case "ampBerkeley":
                                    result = new VolumeControllerForBerkeley(aurender.ConnectionInfo, link);
                                    break;

                                default:
                                    if (!string.IsNullOrEmpty(link))
                                        IARLogStatic.Error("Volume", $"Doesn't support {link} yet");
                                    break;
                            }
                        }
                    }

                    return result;
                }
            }
        }
    }

    public abstract class VolumeControllerBase : BindingSourceObject, IVolumeController
    {
        protected enum Fields
        {
            Volume,
            Phase,
            Output,
            Inputs,
            Mute
        }

        protected double currentVolume;
        public string CurrentVolume
        {
            get
            {
                return string.Format($"-{(currentVolume / 2 + 0.00001).ToString("N1")}dB");
            }
            set
            {
                double.TryParse(value, out currentVolume);
            }
        }

        public IList<string> AvailbleSources { get; protected set; }

        private string currentSource;
        public string CurrentSource
        {
            get => currentSource;
            set => SetProperty(ref currentSource, value);
        }

        private bool isMuted;
        public bool IsMuted
        {
            get => isMuted;
            set => SetProperty(ref isMuted, value);
        }

        private bool isPhasePlus;
        public bool IsPhasePlus
        {
            get => isPhasePlus;
            set => SetProperty(ref isPhasePlus, value);
        }

        public VolumeControllerCapability Capabilty { get; protected set; }

        protected readonly String path;
        protected Dictionary<Fields, String> status;


        protected VolumeControllerBase(IAurenderEndPoint endPoint, String ampPath)
        {
            path = endPoint.WebURLFor($"/php/{ampPath}");
            Capabilty = VolumeControllerCapability.NoControl;
        }

        public async Task LoadVolumeStatus()
        {
            using (var responase = await Utility.WebUtil.GetResponseAsync(path).ConfigureAwait(false))
            {
                if (responase.IsSuccessStatusCode)
                {
                    String str = await responase.Content.ReadAsStringAsync().ConfigureAwait(false);
                    ParseStatus(str);
                }
            }
        }

        public abstract Task Mute(bool on);
        public abstract Task SelectSource(string source);
        public abstract Task SetPhase(bool plus);
        public async Task ToggleMute()
        {
            if ((this.Capabilty & VolumeControllerCapability.Mute) == VolumeControllerCapability.Mute)
            {
                await Mute(!IsMuted);
            }
        }

        public async Task TogglePhase()
        {
            if ((this.Capabilty & VolumeControllerCapability.Phase) == VolumeControllerCapability.Phase
                && (this.Capabilty & VolumeControllerCapability.PhaseToggle) == VolumeControllerCapability.PhaseToggle)
            {
                await Mute(!IsPhasePlus);
            }
        }
        public abstract Task VolumeDown();
        public abstract Task VolumeUp();


        internal abstract void ParseStatus(String response);

        protected async Task<String> SendCommand(String paramInfo)
        {
            String url = $"{path}?{paramInfo}";


            using (var result = await Utility.WebUtil.GetResponseAsync(url).ConfigureAwait(false))
            {

                if (result.IsSuccessStatusCode)
                {
                    string responseString = await result.Content.ReadAsStringAsync().ConfigureAwait(false);

                    ParseStatus(responseString);

                    return responseString;
                }
            }
            return string.Empty;
        }
    }

}