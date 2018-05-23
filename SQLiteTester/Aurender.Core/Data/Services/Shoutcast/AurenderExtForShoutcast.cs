using Aurender.Core.Contents.Streaming;
using Aurender.Core.Player;
using Aurender.Core.Utility;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Aurender.Core.Data.Services.Shoutcast
{
    public static class AurenderExtForShoutcast
    {
        public static async Task SCAddStation(this IAurender aurender, IStreamingObjectCollection<IStreamingTrack> stations, String title, String url)
        {         
            String URL = aurender.urlFor("/wapi/contents/radio/radioAdd");
            List<KeyValuePair<String, String>> postData = new List<KeyValuePair<string, string>>()
            {
                new KeyValuePair<string, string>("title", title),
                new KeyValuePair<string, string>("url", url),
            };

            SCFavoriteStationCollection favStations = stations as SCFavoriteStationCollection;
            await favStations.UpdateListWithURL(URL, postData).ConfigureAwait(false);
        }

        public static async Task SCRemoveStation(this IAurender aurender, IStreamingObjectCollection<IStreamingTrack> stations, IStreamingTrack station)
        {
            SCCustomStation sct = station as SCCustomStation;
            if (sct == null)
            {
                IARLogStatic.Error("InternetRadio", $"Only InternetRadio can be added, but {station}");
                return;
            }
            String URL = aurender.urlFor("/wapi/contents/radio/radioDelete");
            List<KeyValuePair<String, String>> postData = new List<KeyValuePair<string, string>>()
            {
                new KeyValuePair<string, string>("station_id", sct.ItemLine)
            };
            
            SCFavoriteStationCollection favStations = stations as SCFavoriteStationCollection;
            await favStations.UpdateListWithURL(URL, postData).ConfigureAwait(false);
        }


        public static async Task SCUpdateStation(this IAurender aurender, IStreamingObjectCollection<IStreamingTrack> stations, IStreamingTrack station, String title, String url)
        {
            SCCustomStation sct = station as SCCustomStation;
            if (sct == null)
            {
                IARLogStatic.Error("InternetRadio", $"Only InternetRadio can be added, but {station}");
                return;
            }
            String URL = aurender.urlFor("/wapi/contents/radio/radioUpdate");

            string newLine = $"{title}::{url}";

            List<KeyValuePair<String, String>> postData = new List<KeyValuePair<string, string>>()
            {
                new KeyValuePair<string, string>("id", sct.ItemLine),
                new KeyValuePair<string, string>("station", newLine)
            };
            
            SCFavoriteStationCollection favStations = stations as SCFavoriteStationCollection;
            await favStations.UpdateListWithURL(URL, postData).ConfigureAwait(false);
        }

        public static async Task<IStreamingObjectCollection<IStreamingTrack>> GetFavoriteStations(this IAurender aurender)
        {
            String URL = aurender.urlFor("/wapi/contents/radio/radioList");
            String title = ServiceManager.Favorite;
            SCFavoriteStationCollection stations = new SCFavoriteStationCollection(title, URL);
            
            await stations.LoadList(URL).ConfigureAwait(false);

            return stations;
        }


    }

}
