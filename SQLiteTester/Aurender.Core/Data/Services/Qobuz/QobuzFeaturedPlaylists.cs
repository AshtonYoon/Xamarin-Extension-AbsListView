using System;
using System.Collections.Generic;
using Aurender.Core.Contents.Streaming;
using Newtonsoft.Json.Linq;

namespace Aurender.Core.Data.Services.Qobuz
{

    class QobuzFeaturedPlaylists : QobuzCollectionBase<IStreamingPlaylist>
    {
        internal protected QobuzFeaturedPlaylists(int bucketSize, Type y, string title) : base (bucketSize, y, title)
        {
        }
        public QobuzFeaturedPlaylists(int bucketSize, string title) : this(bucketSize, typeof(QobuzPlaylist), title)
        {
        }

        internal QobuzFeaturedPlaylists(String title, String url) : this(50, title)
        {
            this.urlForData = url;
        }


        protected override bool ProcessItems(Dictionary<string, object> info, IList<IStreamingPlaylist> newItems)
        {
            if (info.ContainsKey("playlists"))
            {
                JToken sInfo = info["playlists"] as JToken;

                return this.ProcessItems(sInfo, newItems);
            }

            
            this.EP("Qobuz", "Failed to process featured playlists");
            return false;
        }
    }
}