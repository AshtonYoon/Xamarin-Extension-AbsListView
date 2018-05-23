using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aurender.Core.Contents.Streaming;

using Newtonsoft.Json;

namespace Aurender.Core.Data.Services.Melon
{
    internal class MelonTracksForArtist : MelonCollectionBase<IStreamingTrack>
    {

        public MelonTracksForArtist(MelonArtist melonArtist): base("TracksForArtist", token => new MelonTrack(token))
        {
            this.urlForData = $"detail/listArtistSong.json?startIndex=1&pageSize=100&artistId={melonArtist.StreamingID}&orderBy=POP&listType=A";
        }

        protected override async Task LoadNextDataAsync()
        {
            if (Count == this.items.Count)
                return;

            String url = URLForNextData();

            var t = await ServiceManager.Melon().GetResponseStringByGetDataAsync(url);

            if (t.Length > 0)
            {

                Dictionary<String, Object> sInfo = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(t);
                var newTracks = new List<IStreamingTrack>(this.items);
                bool sucess = ProcessItems(sInfo, newTracks);

                this.items = newTracks;

                this.Info("Melon collection", this.ToString());
            }
            else
            {
                items.Clear();
                Count = 0;
            }
        }
/*
        protected override bool ProcessItems(Dictionary<string, object> sInfo, IList<IStreamingTrack> newItems)
        {
            Object obj;

            if (sInfo.ContainsKey("ERRORMSG"))
            {
                this.EP("Melon Collection", $"Failed to get data {sInfo["ERRORMSG"]}\n{sInfo}");
                this.Count = 0;
                return true;
            }

            obj = sInfo["CDLIST"];

            if (obj != null)
            {
                Object hasMore = null;
                if (sInfo.ContainsKey("COUNT"))
                    hasMore = sInfo["COUNT"];


                JArray cds = obj as JArray;

                foreach (var cd in cds)
                {
                    JArray songs = cd["SONGLIST"] as JArray;

                    foreach (var i in songs)
                    {
                        if (i["SONGID"] != null)
                        {
                            var item = (IStreamingTrack)Activator.CreateInstance(Y, i);
                            newItems.Add(item);
                        }
                    }
                }
                this.Count = newItems.Count;
                return true;
            }
            this.EP("Melon Collection", $"Failed to get totalNumberOfItems \n{sInfo}");
            this.items.Clear();
            Count = 0;
            return false;

        }*/
     }
}