using System;
using System.Linq;
using System.Collections.Generic;
using Aurender.Core.Contents.Streaming;
using Aurender.Core.Player;
using Aurender.Core.Utility;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Aurender.Core.Data.Services.Shoutcast
{

   class SCFavoriteStationCollection : StreamingCollectionBase<IStreamingTrack>
   {
        internal SCFavoriteStationCollection(String title, string api) : base(50, ContentType.InternetRadio, title)
        {
            this.urlForData = api; 
        }

        protected override async Task LoadNextDataAsync()
        {
            if (Count == this.items.Count)
                return;

            String url = this.URLForNextData();

            var result = await WebUtil.DownloadContentsAsync(url);

            if (result != null & result.Item1)
            {
                ParseList(result.Item2);
            }
            else
            {
                this.Count = 0;
            }
        }
        protected override bool ProcessItems(Dictionary<string, object> sInfo, IList<IStreamingTrack> newTracks)
        {
            return false;   
        }

        internal async Task UpdateListWithURL(String url, IList<KeyValuePair<String, String>> postData)
        {
            var result = await WebUtil.PostDataAndDownloadContentsAsync(url, postData).ConfigureAwait(false);

            if (result.Item1)
            {
                ParseList(result.Item2);
            }
            else
            {
                IARLogStatic.Error($"Shoutcast", "Faeild to process station for {url}");
            }
        }
        internal async Task LoadList(String URL)
        {
            var result = await WebUtil.DownloadContentsAsync(URL).ConfigureAwait(false);

            if (result.Item1)
            {
                ParseList(result.Item2);
            }
            else
            {
                IARLogStatic.Error($"Shoutcast", "Faeild to get radio station list");
            }
        }

        private void ParseList(String result)
        {
            Regex ptn = new Regex("<p class=list>([\\s\\S]*)</p>");

            var matches = ptn.Match(result);

            if (matches.Success)
            {
                var stationLines = matches.Groups[1].Value;

                var seperators = new String[] { "\n" };
                var lines = stationLines.Split(seperators, StringSplitOptions.RemoveEmptyEntries);
                List<IStreamingTrack> stations = new List<IStreamingTrack>();

                foreach (var line in lines)
                {
                    IStreamingTrack station = new SCCustomStation(line);

                    if (station.StreamingID != SCCustomStation.NA_ID)
                        stations.Add(station);

                }
                this.items = stations;
                this.Count = stations.Count;
            }
        }

        protected override string URLForNextData()
        {
            return this.urlForData; 
        }
    }

}