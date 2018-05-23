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
using Aurender.Core.Player.mpd;

namespace Aurender.Core.Player.VolumeController
{

    internal class VolumeControllerForConstellation : VolumeControllerBase, INotifyPropertyChanged, IVolumeController
    {
        internal VolumeControllerForConstellation(IAurenderEndPoint endPoint, string ampPath) : base(endPoint, ampPath)
        {
            this.AvailbleSources = new String[] { "A10", "Optical" };
            this.Capabilty = VolumeControllerCapability.Volume | VolumeControllerCapability.Source | VolumeControllerCapability.Mute;
            Task.Run(() => LoadVolumeStatus());
        }

        public override Task Mute(bool on)
        {
            throw new NotImplementedException();
        }

        public override Task SelectSource(string source)
        {
            throw new NotImplementedException();
        }

        public override Task SetPhase(bool plus)
        {
            throw new NotImplementedException();
        }

        public override Task VolumeDown()
        {
            throw new NotImplementedException();
        }

        public override Task VolumeUp()
        {
            throw new NotImplementedException();
        }

        internal override void ParseStatus(String response)
        {
            throw new NotImplementedException();
        }
    }

    internal class VolumeControllerForMSB : VolumeControllerBase, INotifyPropertyChanged, IVolumeController
    {
        internal VolumeControllerForMSB(IAurenderEndPoint endPoint, string ampPath) : base(endPoint, ampPath)
        {
            this.AvailbleSources = new String[] { "A10", "Optical" };
            this.Capabilty = VolumeControllerCapability.Volume | VolumeControllerCapability.Source | VolumeControllerCapability.Mute;
            Task.Run(() => LoadVolumeStatus());

        }

        public override Task Mute(bool on)
        {
            throw new NotImplementedException();
        }

        public override Task SelectSource(string source)
        {
            throw new NotImplementedException();
        }

        public override Task SetPhase(bool plus)
        {
            throw new NotImplementedException();
        }

        public override Task VolumeDown()
        {
            throw new NotImplementedException();
        }

        public override Task VolumeUp()
        {
            throw new NotImplementedException();
        }

        internal override void ParseStatus(String response)
        {
            throw new NotImplementedException();
        }
    }

    internal class VolumeControllerForNADAC : VolumeControllerBase, INotifyPropertyChanged, IVolumeController
    {
        internal VolumeControllerForNADAC(IAurenderEndPoint endPoint, string ampPath) : base(endPoint, ampPath)
        {
            this.AvailbleSources = new String[] { "A10", "Optical" };
            this.Capabilty = VolumeControllerCapability.Volume | VolumeControllerCapability.Source | VolumeControllerCapability.Mute;
            Task.Run(() => LoadVolumeStatus());

        }

        public override Task Mute(bool on)
        {
            throw new NotImplementedException();
        }

        public override Task SelectSource(string source)
        {
            throw new NotImplementedException();
        }

        public override Task SetPhase(bool plus)
        {
            throw new NotImplementedException();
        }

        public override Task VolumeDown()
        {
            throw new NotImplementedException();
        }

        public override Task VolumeUp()
        {
            throw new NotImplementedException();
        }

        internal override void ParseStatus(String response)
        {         
            throw new NotImplementedException();
        }
    }

    internal class VolumeControllerForGoldmund : VolumeControllerBase, INotifyPropertyChanged, IVolumeController
    {
        internal VolumeControllerForGoldmund(IAurenderEndPoint endPoint, string ampPath) : base(endPoint, ampPath)
        {
            this.AvailbleSources = new String[] { "A10", "Optical" };
            this.Capabilty = VolumeControllerCapability.Volume | VolumeControllerCapability.Mute | VolumeControllerCapability.Source;
            Task.Run(() => LoadVolumeStatus());
        }

        public override async Task Mute(bool on)
        {
            string command = on ? "muteOn" : "muteOff";
            String url = AurenderBrowser.GetCurrentAurender().ConnectionInfo.WebURLFor($"/php/ampGoldMund?op={command}");

            using (var response = await Utility.WebUtil.GetResponseAsync(url).ConfigureAwait(false))
            {

            }

             IsMuted = !IsMuted;
        }

        public override Task SelectSource(string source)
        {
            throw new NotSupportedException("Cannot use select source in this model");
        }

        public override Task SetPhase(bool plus)
        {
            throw new NotSupportedException("Cannot use set phase in this model");
        }

        public override async Task VolumeDown()
        {
            String url = AurenderBrowser.GetCurrentAurender().ConnectionInfo.WebURLFor($"/php/ampGoldMund?op=down");
            using (var response = await Utility.WebUtil.GetResponseAsync(url).ConfigureAwait(false))
            {

            }

            currentVolume += 1;
        }

        public override async Task VolumeUp()
        {
            String url = AurenderBrowser.GetCurrentAurender().ConnectionInfo.WebURLFor($"/php/ampGoldMund?op=up");
            using (var response = await Utility.WebUtil.GetResponseAsync(url).ConfigureAwait(false))
            {

            }

            currentVolume -= 1;
        }

        internal override void ParseStatus(string responseString)
        {
            var pattern = @"<p class='output'>(.*)\<br />\n\[OUTPUT\] VOLUME_USB \[(.*)\]\<br />\n(.*)\<br />";
            var regex = new Regex(pattern);

            var matches = regex.Matches(responseString);

            if(matches.Count > 0 && matches[0].Groups.Count > 0)
                CurrentVolume = matches[0].Groups[2].Value;
        }
    }

    internal class VolumeControllerForBerkeley : VolumeControllerBase, INotifyPropertyChanged, IVolumeController
    {
        internal VolumeControllerForBerkeley(IAurenderEndPoint endPoint, string ampPath) : base(endPoint, ampPath)
        {
            this.AvailbleSources = new String[] { "A10", "Optical" };
            this.Capabilty = VolumeControllerCapability.Volume | VolumeControllerCapability.Source | VolumeControllerCapability.Mute;
            Task.Run(() => LoadVolumeStatus());
        }

        public override Task Mute(bool on)
        {
            throw new NotImplementedException();
        }

        public override Task SelectSource(string source)
        {
            throw new NotImplementedException();
        }

        public override Task SetPhase(bool plus)
        {
            throw new NotImplementedException();
        }

        public override Task VolumeDown()
        {
            throw new NotImplementedException();
        }

        public override Task VolumeUp()
        {
            throw new NotImplementedException();
        }

        internal override void ParseStatus(string response)
        {
            throw new NotImplementedException();
        }
    }
}