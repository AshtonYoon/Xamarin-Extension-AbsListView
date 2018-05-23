using System;
using System.Collections.Generic;
using Aurender.Core.Contents.Streaming;
using Newtonsoft.Json.Linq;

namespace Aurender.Core.Data.Services.Qobuz
{
    class QobuzFeaturedTracks : QobuzCollectionBase<IStreamingTrack>
    {
        protected QobuzFeaturedTracks(int count, Type y, String title) : base (count, y, title)
        {

        }
        protected QobuzFeaturedTracks(string title) : this(50, typeof(QobuzTrack), title)
        {
        }
    }

    class QobuzPurchasedTracks : QobuzFeaturedTracks
    {
        protected QobuzPurchasedTracks(int count, Type y, String title) : base (count, y, title)
        {

        }
        public QobuzPurchasedTracks(string title, string auth_token) : this(50, typeof(QobuzTrack), title)
        {
            this.urlForData = $"purchase/getUserPurchases?flat=1&user_auth_token={auth_token}";
        }

        protected override bool ProcessItems(Dictionary<string, object> info, IList<IStreamingTrack> newItems)
        {
            if (info.ContainsKey("tracks"))
            {
                JToken sInfo = info["tracks"] as JToken;

                return this.ProcessItems(sInfo, newItems);
            }

            this.EP("Qobuze", "Failed to parse pucrchased albums");
            return false;
        }
    }

    class QobuzTracksForAlbum : QobuzCollectionBase<IStreamingTrack>
    {
        internal QobuzTracksForAlbum(JToken tracksToken) : base(50, typeof(QobuzTrack), String.Empty)
        {
            this.ProcessItems(tracksToken);
        }

    }

    class QobuzTracksForAlbumFromNetwork : QobuzPurchasedTracks
    {

        internal QobuzTracksForAlbumFromNetwork(QobuzAlbum album) : base (50, typeof(QobuzTrack), String.Empty)
        {
            this.urlForData = $"album/get?album_id={album.StreamingID}";
        }

    }

   
 }