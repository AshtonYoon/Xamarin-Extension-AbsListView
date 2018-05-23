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

    internal class VolumeControllerForX725 : VolumeControllerBase, IVolumeController, INotifyPropertyChanged
    {

        internal VolumeControllerForX725(IAurenderEndPoint endPoint, string link) : base(endPoint, link)
        {
            this.AvailbleSources = new String[] { "USB", "Optical" };
            this.Capabilty = VolumeControllerCapability.Volume | VolumeControllerCapability.Mute;
            Task.Run(() => LoadVolumeStatus());
        }

        public override async Task Mute(bool on)
        {
            String command = on ? "op=muteOn" : "op=muteOff";
            await SendCommand(command).ConfigureAwait(false);
        }

        public override async Task SelectSource(string source)
        {
            String command = $"op={source}";
            await SendCommand(command).ConfigureAwait(false);
        }

        public override Task SetPhase(bool plus)
        {
            IARLogStatic.Error("Volume", "Doesn't support phase");
            return Task.CompletedTask;
        }


        public override async Task VolumeDown()
        {
            String command = "op=down";
            await SendCommand(command).ConfigureAwait(false);
        }

        public override async Task VolumeUp()
        {
            String command = "op=up";
            await SendCommand(command).ConfigureAwait(false);
        }

        internal override void ParseStatus(String response)
        {
            //<p class='output'>[OUTPUT] USB_IN
            //[OUTPUT] MUTED
            //[OUTPUT] VOLUME_USB[84]
            //</p>
            var contents = response;
            

            Regex input = new Regex(@"\[OUTPUT\] (.*)_IN");
            Regex level = new Regex(@"\[OUTPUT\] VOLUME_.*\[([0-9]*)\]");
            Regex muted = new Regex(@"\[OUTPUT\] (MUTED)");

            var match = input.Match(contents);

            if (match.Success)
            {
                this.CurrentSource = match.Groups[1].Value;
            }

            match = level.Match(contents);
            if (match.Success)
                this.CurrentVolume = match.Groups[1].Value;

            match = muted.Match(contents);
            this.IsMuted = match.Success;

        }
    }
  internal class VolumeControllerForA10 : VolumeControllerForX725, IVolumeController, INotifyPropertyChanged
    {
        internal VolumeControllerForA10(IAurenderEndPoint endPoint, string link) : base(endPoint, link)
        {
            this.AvailbleSources = new String[] { "A10", "Optical" };
            this.Capabilty = VolumeControllerCapability.Volume | VolumeControllerCapability.Source | VolumeControllerCapability.Mute;
        }

       
    }


}