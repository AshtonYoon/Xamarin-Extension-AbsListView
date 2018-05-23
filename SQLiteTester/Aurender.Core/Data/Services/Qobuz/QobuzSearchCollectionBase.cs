using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Aurender.Core.Contents.Streaming;
using Aurender.Core.Utility;
using Newtonsoft.Json.Linq;

namespace Aurender.Core.Data.Services.Qobuz
{

    [DebuggerDisplay("{ServiceType} SearchCollection{typeof(T)} Search for [{Keyword}]  : [{CountForLoadedItems}/{Count}]")]
    abstract class QobuzSearchCollectionBase<T> : 
        QobuzCollectionBase<T>, IStreamingObjectCollection<T>, IStreamgingSearchResult<T>
        where T : IStreamingServiceObject
    {
     
        public string Keyword { get; protected set; }
        protected readonly string fieldForItems;

        protected QobuzSearchCollectionBase(int count, Type y, string title, String searchTypes, String itemsField) : base(count, y, title)
        {
            this.urlForData = $"{searchTypes}/search";
            this.fieldForItems = itemsField;
        }

         protected override string URLForNextData() {
            
            String url = $"{urlForData}?query={Keyword.URLEncodedString()}&limit={BucketSize}&offset={CountForLoadedItems}";

            return url;
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


        protected override bool ProcessItems(Dictionary<string, object> totalInfo, IList<T> newItems)
        {
            bool sucess = false;
            JToken sInfo = totalInfo[fieldForItems] as JToken;

            if (sInfo != null)
            {

                int totalCount;
                sucess = int.TryParse(sInfo["total"].ToString(), out totalCount);

                if (!sucess)
                {
                    this.EP("TIDAL Collection", $"Failed to get totalNumberOfItems \n{sInfo}");
                    this.items.Clear();
                    Count = 0;
                    return sucess;
                }

                if (Count == -1)
                    Count = totalCount;

                Debug.Assert(totalCount == Count);

                var items = sInfo["items"] as JArray;

                foreach (var i in items)
                {
                    var item = (T)Activator.CreateInstance(Y, i);
                    newItems.Add(item);
                    //this.LP("TIDAL Collection parsing", $"\t {track}");
                }
            }
            return sucess;
        }
    }

    class QobuzSearchResultForAlbums : QobuzSearchCollectionBase<IStreamingAlbum>
    {
        internal QobuzSearchResultForAlbums(String title) : base (50, typeof(QobuzAlbum), title,  "album", "albums")
        {

        }
    }

    class QobuzSearchResultForTracks : QobuzSearchCollectionBase<IStreamingTrack>
    {
        internal QobuzSearchResultForTracks(String title) : base (50, typeof(QobuzTrack), title,  "track", "tracks")
        {

        }
    }

    class QobuzSearchResultForArtists : QobuzSearchCollectionBase<IStreamingArtist>
    {
        internal QobuzSearchResultForArtists(String title) : base (50, typeof(QobuzArtist), title,  "artist", "artists")
        {

        }
    }

    class QobuzSearchResultForPlaylists : QobuzSearchCollectionBase<IStreamingPlaylist>
    {
        internal QobuzSearchResultForPlaylists(String title) : base (50, typeof(QobuzPlaylist), title,  "playlist", "playlists")
        {

        }
    }
}