using Aurender.Core.Contents.Streaming;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace Aurender.Core.Data.Services.Qobuz
{
    internal class QobuzPlaylistTracks : QobuzFeaturedTracks
    {
        

        public QobuzPlaylistTracks(String playlist_id, JToken jToken) : base (200, typeof(QobuzTrack), string.Empty)
        {
            this.urlForData = $"playlist/get?playlist_id={playlist_id}&extra=tracks";
            ProcessItems(jToken);
        }

        public QobuzPlaylistTracks(QobuzPlaylist playlist) : base(200, typeof(QobuzTrack), String.Empty)
        {
            this.urlForData = $"playlist/get?playlist_id={playlist.StreamingID}&extra=tracks";
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

    //class QobuzTracksForPlaylistFromNetwork : QobuzCollectionBase<IStreamingTrack>
    //{

    //    internal QobuzTracksForPlaylistFromNetwork(QobuzPlaylist playlist) : base (50, typeof(QobuzTrack), String.Empty)
    //    {
    //        this.urlForData = $"playlist/get?playlist_id={playlist.StreamingID}";
    //    }


    //}
}