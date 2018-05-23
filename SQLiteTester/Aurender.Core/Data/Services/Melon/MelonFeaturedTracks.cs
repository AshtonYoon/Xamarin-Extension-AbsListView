using System;
using System.Collections.Generic;
using Aurender.Core.Contents.Streaming;
using Newtonsoft.Json.Linq;
using System.Diagnostics;

namespace Aurender.Core.Data.Services.Melon
{

    internal class MelonFeaturedTracks : MelonCollectionBase<IStreamingTrack>
    {
        internal MelonFeaturedTracks(string title, string url) : base(title, token => new MelonTrack(token))
        {
            this.urlForData = url;
        }
        protected MelonFeaturedTracks(int bucketSize, string title, string url, Func<JToken, IStreamingTrack> constructor) : base(bucketSize, title, constructor)
        {
            this.urlForData = url;
        }

        protected override bool ProcessItems(Dictionary<string, object> sInfo, IList<IStreamingTrack> newItems)
        {
            Object obj;

            if (sInfo.ContainsKey("ERRORMSG"))
            {
                this.EP("Melon Collection", $"Failed to get data {sInfo["ERRORMSG"]}\n{sInfo}");
                this.Count = 0;
                return true;
            }

            if (sInfo.ContainsKey("SONGS"))
                obj = sInfo["SONGS"];
            else if (sInfo.ContainsKey("CONTENTS"))
                obj = sInfo["CONTENTS"];
            else
                obj = sInfo["SONGLIST"];

            if (obj != null)
            {
                Object hasMore = null;
                if (sInfo.ContainsKey("COUNT"))
                    hasMore = sInfo["COUNT"];

                JArray contents = obj as JArray;

                int more = 0;

                if (hasMore != null)
                    more = (int)((long)hasMore);
                else
                    more = contents.Count;

                if (Count == -1)
                    Count = more;

                Debug.Assert(more == Count);

                foreach (var i in contents)
                {
                    var item = constructor(i);
                    newItems.Add(item);
                }
                return true;
            }
            this.EP("Melon Collection", $"Failed to get totalNumberOfItems \n{sInfo}");
            this.items.Clear();
            Count = 0;
            return false;

        }
 
    }



    internal class MelonTracksByGenre : MelonCollectionBase<IStreamingTrack>
    {
        internal MelonTracksByGenre(string title, string genreID) : base(title, token => new MelonTrack(token))
        {
            this.urlForData = $"alliance/genre/genresong_list.json?gnrCode={genreID}&imgW=300&imgH=300&orderBy=new";
        }

        protected override bool ProcessItems(Dictionary<string, object> sInfo, IList<IStreamingTrack> newItems)
        {
            Object obj = null;
            if (sInfo.ContainsKey("SONGS"))
                obj = sInfo["SONGS"];
            else if (sInfo.ContainsKey("CONTENTS"))
                obj = sInfo["CONTENTS"];
            else if (sInfo.ContainsKey("SONGLIST"))
                obj = sInfo["SONGLIST"];

            if (obj != null)
            {
                bool hasMore = false;
                if (sInfo.ContainsKey("HASMORE"))
                    hasMore =(bool) sInfo["HASMORE"];

                JArray contents = obj as JArray;

                foreach (var i in contents)
                {
                    var item = constructor(i);
                    newItems.Add(item);
                }

                int newTotal = this.Count = this.items.Count + newItems.Count;
                if (hasMore)
                    newTotal ++;

                this.Count = newTotal;

                return true;
            }
            this.EP("Melon collection", $"Failed to get totalNumberOfItems \n{sInfo}");
            this.items.Clear();
            Count = 0;
            return false;

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

            if (sInfo.ContainsKey("SONGS"))
                obj = sInfo["SONGS"];
            else if (sInfo.ContainsKey("CONTENTS"))
                obj = sInfo["CONTENTS"];
            else
                obj = sInfo["SONGLIST"];

            if (obj != null)
            {
                Object hasMore = null;
                if (sInfo.ContainsKey("COUNT"))
                    hasMore = sInfo["COUNT"];

                JArray contents = obj as JArray;

                int more = 0;

                if (hasMore != null)
                    more = (int)((long)hasMore);
                else
                    more = contents.Count;

                if (Count == -1)
                    Count = more;

                Debug.Assert(more == Count);

                foreach (var i in contents)
                {
                    var item = (IStreamingTrack)Activator.CreateInstance(Y, i);
                    newItems.Add(item);
                }
                return true;
            }
            this.EP("Melon Collection", $"Failed to get totalNumberOfItems \n{sInfo}");
            this.items.Clear();
            Count = 0;
            return false;

        }*/

    }

}