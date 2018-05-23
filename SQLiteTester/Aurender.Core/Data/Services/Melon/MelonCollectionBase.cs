using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aurender.Core.Contents.Streaming;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using Newtonsoft.Json;

namespace Aurender.Core.Data.Services.Melon
{

    internal class MelonCollectionBase<T> : StreamingCollectionBase<T>, IStreamingObjectCollection<T> where T : IStreamingServiceObject
    {
        protected const string Tag = "Melon Collection";

        protected readonly Func<JToken, T> constructor;
        protected string nameForItems = String.Empty;


        protected MelonCollectionBase(int bucketSize, string title, Func<JToken, T> constructor) : base(bucketSize, ContentType.Melon, title)
        {
            this.constructor = constructor;
        }

        protected MelonCollectionBase(string title, Func<JToken, T> constructor) : base(50, ContentType.Melon, title)
        {
            this.constructor = constructor;
        }
        protected MelonCollectionBase(int bucketSize, string title, String url, Func<JToken, T> constructor) : base(bucketSize, ContentType.Melon, title)
        {
            this.urlForData = url;
            this.constructor = constructor;
        }
        protected override string URLForNextData()
        {
            Debug.Assert(this.urlForData != null);
            string newURL;
            String pageSize = $"&pageSize={this.BucketSize}";
            String ver;
            if (this.urlForData.Contains("playlistInform.json"))
                ver = "v=1.1";
            else
                ver = "v=1.0";

            string indexParam = $"startIndex={this.CountForLoadedItems}";

            if (urlForData.IndexOf("?") > 0) {
                if (urlForData.Contains("pageSize="))
                    newURL = $"{this.urlForData}&{ver}&{indexParam}";
                else
                    newURL = $"{this.urlForData}&{ver}{pageSize}&{indexParam}";
            }
            else
                newURL = $"{this.urlForData}?{ver}{pageSize}&{indexParam}";


            return newURL;
        }

        protected override bool ProcessItems(Dictionary<string, object> sInfo, IList<T> newItems)
        {
            Object obj = null;
            if (sInfo.ContainsKey("CONTENTS"))
                obj = sInfo["CONTENTS"];

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
            this.EP(Tag, $"Failed to get totalNumberOfItems \n{sInfo}");
            this.items.Clear();
            Count = 0;
            return false;

        }


        protected override async Task LoadNextDataAsync()
        {
            if (Count == this.items.Count)
                return;

            String url = URLForNextData();

            using (var response = await ServiceManager.Melon().GetResponseByGetDataAsync(url, null))
            {

                var str = await response.Content.ReadAsStringAsync();
                if (response.IsSuccessStatusCode)
                {

                    Dictionary<String, Object> sInfo = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(str);

                    var newTracks = new List<T>(this.items);
                    bool sucess = ProcessItems(sInfo, newTracks);

                    this.items = newTracks;
                }
                else
                {
                    items.Clear();
                    Count = 0;
                }
            }
        }
    }


    internal class MelonSearchCollection<T> : MelonCollectionBase<T>, IStreamgingSearchResult<T> where T : IStreamingServiceObject
    {
        public string Keyword { get; protected set; }
        protected MelonSearchCollection(int bucketSize, string title, Func<JToken, T> constructor) : base(bucketSize, title, constructor)
        {
            this.Keyword = String.Empty;
        }

        protected override string URLForNextData()
        {
            Debug.Assert(this.urlForData != null);
            string newURL;
            String pageSize = $"&pageSize={this.BucketSize}";
            String ver = "v=1.0";

            string indexParam = $"startIndex={this.CountForLoadedItems}";

            string search = $"searchKeyword={Uri.EscapeDataString(this.Keyword)}";

            if (urlForData.IndexOf("?") > 0) {
                if (urlForData.Contains("pageSize="))
                    newURL = $"{this.urlForData}&{ver}&{search}&{indexParam}";
                else
                    newURL = $"{this.urlForData}&{ver}&{search}&{pageSize}&{indexParam}";
            }
            else
                newURL = $"{this.urlForData}?{ver}&{search}&{pageSize}&{indexParam}";

            return newURL;
        }

        public Task SearchAsync(string keyword)
        {
            if (keyword == null)
            {
                keyword = String.Empty;
            }

            if (this.Keyword == keyword)
            {
                return Task.CompletedTask;
            }

            this.Keyword = keyword;
            Reset();

            return LoadNextAsync();
        }

        protected String fieldName;

        protected override bool ProcessItems(Dictionary<string, object> sInfo, IList<T> newItems)
        {
            bool sucess = false;
            int totalCount;

            Object obj = null;
            if (sInfo.ContainsKey(fieldName))
                obj = sInfo[fieldName];

            Object hasMore = null;
            if (sInfo.ContainsKey("COUNT"))
                hasMore = sInfo["COUNT"];

            if (obj != null && hasMore != null)
            {
                JArray contents = obj as JArray;
                int more = (int) ((Int64) hasMore);


                totalCount = more;

                if (Count == -1)
                    Count = totalCount;

                Debug.Assert(totalCount == Count);

                foreach (var i in contents)
                {
                    var item = constructor(i);
                    newItems.Add(item);
                    //this.LP("Melon Collection parsing", $"\t {track}");
                }
            }

            if (hasMore == null)
                {
                    this.EP(Tag, $"Failed to get totalNumberOfItems");
                    this.items.Clear();

                    Count = 0;
                    return sucess;
                }

           return sucess;
        }
    }



    internal class MelonGenreCollection : MelonCollectionBase<IStreamingGenre>
    {
        internal MelonGenreCollection(string title, List<(String title, String code)> genres) : base(title, x => default(MelonGenre))
        {
            this.items = new List<IStreamingGenre>();
            foreach(var data in genres)
            {
                var genre = new MelonGenre(data.title, data.code);

                this.items.Add(genre);
            }
            this.Count = this.items.Count;
        }

        protected override Task LoadNextDataAsync()
        {
            return Task.CompletedTask;
        }
    }

    internal class MelonFavoriteCollection<T> : MelonCollectionBase<T> where T : IStreamingServiceObject
    {
        protected MelonFavoriteCollection(string title, Func<JToken, T> constructor) : base(title, constructor)
        {
        }

        protected MelonFavoriteCollection(int bucketSize, string title, Func<JToken, T> constructor) : base(bucketSize, title, constructor)
        {
        }

        protected override bool ProcessItems(Dictionary<string, object> sInfo, IList<T> newItems)
        {
            Object obj = null;

            if (sInfo.ContainsKey("ERRORMSG"))
            {
                this.EP(Tag, $"No favorite: {sInfo["ERRORMSG"]}." );
                this.Count = 0;
                return true;
            }
            if (sInfo.ContainsKey("CONTENTS"))
                obj = sInfo["CONTENTS"];

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
            this.EP(Tag, $"Failed to get totalNumberOfItems \n{sInfo}");
            this.items.Clear();
            Count = 0;
            return false;

        }
    }
    
    internal class MelsonTracksForArtist : MelonCollectionBase<IStreamingTrack>
    {
        public MelsonTracksForArtist(String artistID) : base (100, "ArtistTracks", token => new MelonTrack(token))
        {
            this.urlForData = $"detail/listArtistSong.json?artistId={artistID}&orderBy=POP&listType=A";
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
            this.EP(Tag, $"Failed to get totalNumberOfItems \n{sInfo}");
            this.items.Clear();
            Count = 0;
            return false;

        }
    }

    internal class MelonFeaturedAlbums : MelonCollectionBase<IStreamingAlbum>
    {
        public MelonFeaturedAlbums(string title, String link) : base(title, token => new MelonAlbum(token))
        {
            this.urlForData = link;
        }
        protected override bool ProcessItems(Dictionary<string, object> sInfo, IList<IStreamingAlbum> newItems)
        {
            Object obj;
            if (sInfo.ContainsKey("ALBUMS"))
                obj = sInfo["ALBUMS"];
            else if (sInfo.ContainsKey("CONTENTS"))
                obj = sInfo["CONTENTS"];
            else
                obj = sInfo["ALBUMLIST"];

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
            this.EP(Tag, $"Failed to get totalNumberOfItems \n{sInfo}");
            this.items.Clear();
            Count = 0;
            return false;

        }
    }

    internal class MelonFavoriteAlbums : MelonFavoriteCollection<IStreamingAlbum>
    {
        public MelonFavoriteAlbums(string title, String order) : base(title, token => new MelonAlbum(token))
        {
            this.urlForData = $"mymusic/likeAlbumList.json?orderBy={order}";
        }
     }


    internal class MelonSearchResultForAlbums : MelonSearchCollection<IStreamingAlbum>
    {
        public MelonSearchResultForAlbums(string title) : base(50, title, token => new MelonAlbum(token))
        {
            this.urlForData = "search/listSearchAlbum.json";
            this.fieldName = "ALBUMS";
        }
    }

    internal class MelonFavoriteArtists : MelonFavoriteCollection<IStreamingArtist>
    {
        public MelonFavoriteArtists(string title) : base(title, token => new MelonArtist(token))
        {
            this.urlForData = "mymusic/artistFanList.json";
        }
    }

    internal class MelonSearchResultForArtists : MelonSearchCollection<IStreamingArtist>
    {
        public MelonSearchResultForArtists(string title) : base(50, title, token => new MelonArtist(token))
        {
            this.urlForData = "search/listSearchArtist.json";
            this.fieldName = "ARTISTS";
        }
        /*
        protected override bool ProcessItems(Dictionary<string, object> sInfo, IList<IStreamingArtist> newItems)
        {
            Object obj = null;

            if (sInfo.ContainsKey("ERRORMSG"))
            {
                this.EP("Melon Collection", $"No favorite albums." );
                this.Count = 0;
                return true;
            }
            if (sInfo.ContainsKey("CONTENTS"))
                obj = sInfo["CONTENTS"];

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
                    var item = (IStreamingArtist)Activator.CreateInstance(Y, i);
                    newItems.Add(item);
                }
                return true;
            }
            this.EP("Melon Collection", $"Failed to get totalNumberOfItems \n{sInfo}");
            this.items.Clear();
            Count = 0;
            return false;

        }
        */

    }
    internal class MelonFeaturedPlaylists : MelonCollectionBase<IStreamingPlaylist>
    {
        public MelonFeaturedPlaylists(string title, String link) : base(title, token => new MelonPlaylist(token))
        {
            this.urlForData = link;
        }


        protected override bool ProcessItems(Dictionary<string, object> sInfo, IList<IStreamingPlaylist> newItems)
        {
            Object obj = null;
            if (sInfo.ContainsKey("CONTENTS"))
                obj = sInfo["CONTENTS"];

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

    internal class MelonMyPlaylists : MelonCollectionBase<IStreamingPlaylist>
    {
        public MelonMyPlaylists(string title) : base(title, token => new MelonPlaylist(token))
        {
            this.urlForData = "mymusic/playListList.json?imgW=300&imgH=300";
        }
      protected override bool ProcessItems(Dictionary<string, object> sInfo, IList<IStreamingPlaylist> newItems)
        {
            Object obj = null;
            if (sInfo.ContainsKey("CONTENTS"))
                obj = sInfo["CONTENTS"];

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
            this.EP(Tag, $"Failed to get totalNumberOfItems \n{sInfo}");
            this.items.Clear();
            Count = 0;
            return false;

        }
  
    }


    internal class MelonFavoritePlaylists : MelonFavoriteCollection<IStreamingPlaylist>
    {
        public MelonFavoritePlaylists(string title) : base(title, token => new MelonPlaylist(token))
        {
            this.urlForData = "mymusic/likeDjPlaylistList.json";
        }
    }

}