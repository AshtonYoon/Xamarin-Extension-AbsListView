using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Aurender.Core.Contents.Streaming;
using Newtonsoft.Json.Linq;

namespace Aurender.Core.Data.Services.Qobuz
{

    class QobuzSimilarArtists : QobuzCollectionBase<IStreamingArtist>
    {
        public QobuzSimilarArtists(QobuzArtist artist) : base(50, typeof(QobuzArtist), String.Empty)
        {
            this.urlForData = $"artist/getSimilarArtists?artist_id={artist.StreamingID}";
        }

        protected override bool ProcessItems(Dictionary<string, object> info, IList<IStreamingArtist> newItems)
        {
            var obj = info["artists"];

            if (obj != null && obj is JToken)
            {
                JToken sInfo = obj as JToken;

                int totalCount;
                bool sucess = int.TryParse(sInfo["total"].ToString(), out totalCount);

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
                    var item = (IStreamingArtist) Activator.CreateInstance(Y, i);
                    newItems.Add(item);
                    //this.LP("TIDAL Collection parsing", $"\t {track}");
                }

                return sucess;
            }

            this.EP("Qobuz", "Failed to parse similar artists");

            return false;
        }
    }

    class QobuzAlbumsForArtist : QobuzCollectionBase<IStreamingAlbum>
    {
        public QobuzAlbumsForArtist(QobuzArtist artist) : base(50, typeof(QobuzAlbum), String.Empty)
        {
            this.urlForData = $"artist/get?artist_id={artist.StreamingID}&extra=albums";
        }

        protected override bool ProcessItems(Dictionary<string, object> sInfo, IList<IStreamingAlbum> newItems)
        {
            if (sInfo.ContainsKey("albums"))
            {
                var albums = sInfo["albums"] as JToken;

                int totalCount;
                bool sucess = int.TryParse(albums["total"].ToString(), out totalCount);

                if (!sucess)
                {
                    this.EP("TIDAL Collection", $"Failed to get totalNumberOfItems \n{albums}");
                    this.items.Clear();
                    Count = 0;
                    return sucess;
                }

                if (Count == -1)
                    Count = totalCount;

                Debug.Assert(totalCount == Count);

                var items = albums["items"] as JArray;

                foreach (var i in items)
                {
                    var item = new QobuzAlbum(i);
                    newItems.Add(item);
                    //this.LP("TIDAL Collection parsing", $"\t {track}");
                }
                this.AlbumsByType = new Dictionary<string, List<IStreamingAlbum>>
                {
                    { "All", newItems.ToList() }
                };

                return sucess;
            }
            else
            {
                this.Count = 0;
            }

            return true;

        }
    }
}