using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using Aurender.Core.Contents.Streaming;
using Aurender.Core.Utility;

namespace Aurender.Core.Data.Services.Shoutcast
{

    class SCStationCollection : StreamingCollectionBase<IStreamingTrack>
    {
        internal SCStationCollection(String name, String api) : base(50, ContentType.InternetRadio, name)
        {
            this.urlForData = api;
        }
        internal SCStationCollection(String name, String api, int count) : base(50, ContentType.InternetRadio, name)
        {
            this.urlForData = api;
            this.Count = count;
        }
        protected override bool ProcessItems(Dictionary<string, object> info, IList<IStreamingTrack> newTracks)
        {
            this.LP("ShoutcastGenre", "Faeild to get response");
            return true;
        }

        protected override string URLForNextData()
        {
            String pararm = $"limit={this.items.Count},{this.BucketSize}";
            return SSShoutcast.URLForShoutcast(urlForData, pararm);
        }

        protected override async Task LoadNextDataAsync()
        {
            if (Count == this.items.Count)
                return;

            String url = this.URLForNextData();

            using (var response = await WebUtil.GetResponseAsync(url))
            {
                if (response.IsSuccessStatusCode)
                {
                    var xml = await response.Content.ReadAsStringAsync();

                    if (!string.IsNullOrWhiteSpace(xml))
                    {
                        var doc = XDocument.Parse(xml);
                        var tunein = doc.Root.Element("tunein");
                        String strTuneIn = null;

                        if (tunein != null)
                        {
                            strTuneIn = tunein.Attribute("base").Value;
                        }

                        var newTracks = new List<IStreamingTrack>(this.items);

                        ProcessItems(doc, strTuneIn, newTracks);

                        this.items = newTracks;

                        return;
                    }
                }

                items.Clear();
                Count = 0;
            }
        }

        protected bool ProcessItems(XDocument doc, String tunein, IList<IStreamingTrack> newTracks)
        {
            var stations = doc.Root.Elements("station");

            if (stations != null)
            {
                if (Count == -1)
                {
                    int count = stations.Count();
                    if (count <= 50)
                    {
                        Count = count;
                    }
                    else
                    {
                        Count = 500;
                    }
                }

                foreach (var station in stations)
                {
                    var newStation = new SCStation(station, tunein);

                    newTracks.Add(newStation);
                }
                
            }


            return true;
        }


    }

}