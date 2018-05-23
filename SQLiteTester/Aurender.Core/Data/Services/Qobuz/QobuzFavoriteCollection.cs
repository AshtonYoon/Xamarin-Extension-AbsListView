using System;
using System.Collections.Generic;
using System.Diagnostics;
using Aurender.Core.Contents.Streaming;
using Newtonsoft.Json.Linq;

namespace Aurender.Core.Data.Services.Qobuz
{

    class QobuzFavoriteCollection<T> : QobuzCollectionBase<T> where T : IStreamingServiceObject
    {
        protected readonly String fieldForItems;
        public QobuzFavoriteCollection(string title, string type, Type y) : base(500, y, title)
        {
            fieldForItems = type;
            this.urlForData = $"favorite/getUserFavorites?type={type}";
        }

        public QobuzFavoriteCollection(String title, String type, Type y, JToken token) : this(title, type, y)
        {
                bool sucess = ProcessItems(token);

                if (!sucess)
                {
                    this.LP("Qobuz", $"Failed to get favorite items[{this}] from token : {token.ToString()}");
                }
        }


        protected override string URLForNextData()
        {
            String orderClause = "";
            if (CurrentOrder != Contents.Ordering.Default)
            {
                orderClause = GetOrderClause();
            }
            if (urlForData.IndexOf('?') > 0)
                return $"{urlForData}&limit={BucketSize}&offset={CountForLoadedItems}&{orderClause}";
            else
                return $"{urlForData}?limit={BucketSize}&offset={CountForLoadedItems}&{orderClause}";
        }



        protected override bool ProcessItems(Dictionary<string, object> info, IList<T> newItems)
        {
            if (info.ContainsKey(this.fieldForItems))
            {
                JToken sInfo = info[fieldForItems] as JToken;

                return this.ProcessItems(sInfo, newItems);
            }

            this.EP("Qobuze", $"Failed to parse favorite {this.fieldForItems}");
            return false;
        }
    }


    class QobuzFavoriteTracks : QobuzFavoriteCollection<IStreamingTrack>
    {
        public QobuzFavoriteTracks(String title, String url, JToken token) : base(title, "tracks", typeof(QobuzTrack), token)
        {
            this.urlForData = url;
            while (this.Count != this.CountForLoadedItems)
            {
                var t = this.LoadNextAsync();
                t.Wait();
            }
        }
    }

    class QobuzFavoriteAlbums : QobuzFavoriteCollection<IStreamingAlbum>
    {

        public QobuzFavoriteAlbums(String title, String url, JToken token) : base(title, "albums", typeof(QobuzAlbum), token)
        {
            this.urlForData = url;
        }
    }

    class QobuzFavoriteArtists : QobuzFavoriteCollection<IStreamingArtist>
    {
        public QobuzFavoriteArtists(String title, String url, JToken token) : base(title, "artists", typeof(QobuzArtist), token)
        {
            this.urlForData = url;
        }
    }

    class QobuzMyPlaylists : QobuzFavoriteCollection<IStreamingPlaylist>
    {
        internal QobuzMyPlaylists(string title) : base(title, "playlists", typeof(QobuzPlaylist))
        {
            var userId = ServiceManager.Qobuz().userID;
            this.urlForData = $"playlist/getUserPlaylists?user_id={userId}";
        }

        protected override bool ProcessItems(Dictionary<string, object> info, IList<IStreamingPlaylist> newItems)
        {
            if (info.ContainsKey(this.fieldForItems))
            {
                JToken sInfo = info[fieldForItems] as JToken;

                return this.ProcessItems2(sInfo, newItems);
            }

            this.EP("Qobuze", $"Failed to parse favorite {this.fieldForItems}");
            return false;
        }


        protected bool ProcessItems2(JToken sInfo, IList<IStreamingPlaylist> newItems)
        {
            JToken total = sInfo["total"];
            if (total != null)
            {
                int totalCount = total.ToObject<Int32>();

                if (Count == -1)
                    Count = totalCount;

                Debug.Assert(totalCount == Count);

                var items = sInfo["items"] as JArray;

                foreach (var i in items)
                {
                    var owner = i["owner"];
                    if (owner != null && owner["id"].ToString() == ServiceManager.Qobuz().userID)
                    {
                            var item = (QobuzPlaylist)Activator.CreateInstance(Y, i);
                            newItems.Add(item);
                    }
                    //this.LP("Qobuz Collection parsing", $"\t {track}");
                }
                Count = newItems.Count;

                return true;
            }

            this.EP("Qobuz Collection", $"Failed to get totalNumberOfItems \n{sInfo}");
            this.items.Clear();
            Count = 0;
            return false;

        }
    }

}