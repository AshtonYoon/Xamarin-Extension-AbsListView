using Aurender.Core.Contents.Streaming;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Aurender.Core.Data.Services.Bugs
{
    class BugsFeaturedAlbums : BugsCollectionBase<IStreamingAlbum>
    {
        protected BugsFeaturedAlbums(string title, int count, Type t) : base(count, typeof(BugsAlbum), title)
        {

        }
        public BugsFeaturedAlbums(string title, String url) : base(50, typeof(BugsAlbum), title)
        {
            this.urlForData = ServiceManager.Bugs().URLFor(url);
        }
    }

    class BugsAlbumsForArtist : BugsFeaturedAlbums
    {
        public BugsAlbumsForArtist(BugsArtist artist) : base(artist.ArtistName, $"artists/{artist.StreamingID}/albums")
        {
        }

        protected override bool ProcessItems(Dictionary<string, object> sInfo, IList<IStreamingAlbum> newItems)
        {
            bool sucess = false;
            int totalCount;

            Object obj = sInfo["pager"];
            if (obj != null)
            {
                JToken pager = obj as JToken;

                sucess = pager != null;

                if (!sucess)
                {
                    this.EP("TIDAL Collection", $"Failed to get totalNumberOfItems \n{sInfo}");
                    this.items.Clear();
                    Count = 0;
                    return sucess;
                }


                totalCount = pager["total_count"].ToObject<int>();

                if (Count == -1)
                    Count = totalCount;

                Debug.Assert(totalCount == Count);

                var items = sInfo["list"] as JArray;

                foreach (var i in items)
                {
                    var item = new BugsAlbum(i);
                    newItems.Add(item);
                    //this.LP("TIDAL Collection parsing", $"\t {track}");
                }

                this.AlbumsByType = new Dictionary<string, List<IStreamingAlbum>>
                {
                    { "All", newItems.ToList() }
                };
            }
            return sucess;
        }
    }

     
    class BugsFavoriteAlbums : BugsFeaturedAlbums
    {
        internal BugsFavoriteAlbums (String title) : base (title, SSBugs.FavoriteBucketSize, typeof(BugsAlbum))
        {
            this.urlForData = ServiceManager.Service(ServiceType).URLFor($"me/likes/album");
        }

    }



}
