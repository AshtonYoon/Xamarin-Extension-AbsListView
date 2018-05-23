using System;
using System.Collections.Generic;
using Aurender.Core.Contents.Streaming;
using Newtonsoft.Json.Linq;
using System.Diagnostics;

namespace Aurender.Core.Data.Services.Bugs
{

    class BugsFeaturedPlaylist : BugsCollectionBase<IStreamingPlaylist>
    {
        protected BugsFeaturedPlaylist(string title, int count, Type t) : base(count, t, title)
        {

        }
        internal BugsFeaturedPlaylist(String title, String url) : this(title, 50, typeof(BugsPlaylistForESAlbum))
        {
            this.urlForData = ServiceManager.Bugs().URLFor(url);
        }

        protected override bool ProcessItems(Dictionary<string, object> sInfo, IList<IStreamingPlaylist> newItems)
        {
            bool sucess = false;
            int totalCount;
            if (sInfo.ContainsKey("pager"))
            {
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
                        var item = (IStreamingPlaylist)Activator.CreateInstance(Y, i);
                        newItems.Add(item);
                        //this.LP("TIDAL Collection parsing", $"\t {track}");
                    }
                }
            }
            return sucess;
        }
    }
    
    class BugsFeaturedNewPlaylist : BugsFeaturedPlaylist
    {
        internal BugsFeaturedNewPlaylist(String title, String url) : base(title, SSBugs.FavoriteBucketSize, typeof(BugsPlaylistForESAlbum))
        {
            this.urlForData = ServiceManager.Bugs().URLFor(url);
        }

    }
    class BugsFavoritePlaylist : BugsFeaturedPlaylist
    {
        internal BugsFavoritePlaylist(String title) : base(title, SSBugs.FavoriteBucketSize, typeof(BugsPlaylistForESAlbum))
        {
            this.urlForData = ServiceManager.Bugs().URLFor($"me/likes/openalbum");
        }

    }

    class BugsMyPlaylists : BugsFeaturedPlaylist
    {
         public BugsMyPlaylists(string title) : base(title, 50, typeof(BugsMyPlaylist))
        {
               this.urlForData = ServiceManager.Bugs().URLFor($"myalbums");
        }

       
    }
}