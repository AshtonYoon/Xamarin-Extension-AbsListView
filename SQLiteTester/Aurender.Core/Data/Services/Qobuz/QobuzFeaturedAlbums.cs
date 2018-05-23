using System;
using System.Collections.Generic;
using Aurender.Core.Contents.Streaming;
using Newtonsoft.Json.Linq;

namespace Aurender.Core.Data.Services.Qobuz
{

    class QobuzFeaturedAlbums : QobuzCollectionBase<IStreamingAlbum>
    {
        internal protected QobuzFeaturedAlbums(int bucketSize, Type y, string title) : base (bucketSize, y, title)
        {
        }
            
        public QobuzFeaturedAlbums(string title, string url) : this(50, typeof(QobuzAlbum), title)
        {
            this.urlForData = url;
        }

        protected override bool ProcessItems(Dictionary<string, object> info, IList<IStreamingAlbum> newItems)
        {
            if (info.ContainsKey("albums"))
            {
                JToken sInfo = info["albums"] as JToken;

                return this.ProcessItems(sInfo, newItems);
            }

            this.EP("Qobuze", "Failed to parse pucrchased albums");
            return false;
        }


    }
    class QobuzPurchasedAlbums : QobuzFeaturedAlbums
    {       
        public QobuzPurchasedAlbums(string title, string auth_token) : base(50, typeof(QobuzAlbum), title)
        {
            this.urlForData = $"purchase/getUserPurchases?user_auth_token={auth_token}";
        }
    }


    class QobuzAlbumsForGenres : QobuzFeaturedAlbums
    {
        public QobuzAlbumsForGenres(QobuzGenre genre) : base(50, typeof(QobuzAlbum), genre.Name)
        {
            String typeParam = String.Empty;
            if (genre.genreType != null && genre.genreType.Length > 0)
                typeParam = $"&type={genre.genreType}";

            this.urlForData = $"album/getFeatured?genre_id={genre.StreamingID}{typeParam}";
        }

    }

}