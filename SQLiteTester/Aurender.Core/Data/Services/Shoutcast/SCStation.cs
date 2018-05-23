using System;
using System.Diagnostics;
using System.Xml.Linq;
using Aurender.Core.Contents.Streaming;

namespace Aurender.Core.Data.Services.Shoutcast
{

    [DebuggerDisplay("InternetRadio {StreamingID} : [{Title}]  - [{ItemPath}]")]
    class SCStation : StreamingTrack, IRadioStation
    {
        private static object lockObject = new object();
        protected SCStation() : base(ContentType.InternetRadio)
        {
        }
        internal SCStation(XElement station, string tunein) : base(ContentType.InternetRadio)
        {
            var ct = station.Attribute("ct");
            var mt = station.Attribute("mt");
            var br = station.Attribute("br");

            if (mt == null)
            {
                if (ct == null)
                    this.ArtistName = "N/A";
                else
                    this.ArtistName = ct.Value;
            }
            else {
                if (ct == null)
                    this.ArtistName = "N/A";
                else
                    this.ArtistName = $"{ct.Value} [{mt.Value}]";
            }


            this.StreamingID = station.Attribute("id").Value;
            this.Title = station.Attribute("name").Value;
            if (br != null)
                this.AlbumTitle = $"{br.Value}kbps";
            else
                this.AlbumTitle = String.Empty;

            //var lc = station.Attribute("lc").Value;
            var etLogo = station.Attribute("logo");

            if (etLogo != null)
            {
                logo = etLogo.Value;
            }

            this.tuneIn = tunein;
        }

        protected String itemPath;
        public override string ItemPath => $"shout://{StreamingID}::{Title}";


        protected string logo;
        protected string tuneIn;

        internal String TuneIn => tuneIn;

        public override string ImageUrl => logo;

        public override string ImageUrlForMediumSize => logo;
        public override string ImageUrlForLargeSize => logo;

        public override object GetExtraImage()
        {
            return null;
        }
    }
}
