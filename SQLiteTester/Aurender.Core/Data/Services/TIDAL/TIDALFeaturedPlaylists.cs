using Aurender.Core.Contents.Streaming;
using System;
using System.Collections.Generic;

namespace Aurender.Core.Data.Services.TIDAL
{

    class TIDALFeaturedPlaylists : TIDALCollectionBase<IStreamingPlaylist>
    {
        internal TIDALFeaturedPlaylists(string title, string url) : base(50, title, token => new TIDALPlaylist(token))
        {
            this.urlForData = ServiceManager.Service(ContentType.TIDAL).URLFor(url);
        }

    }


    class TIDALPlaylistsForMood : TIDALFeaturedPlaylists
    {
        internal TIDALPlaylistsForMood(String path) : base("", $"moods/{path}/playlists")
        {

        }
    }
    class TIDALSearchResultForPlaylists : TIDALSearchCollectionBase<IStreamingPlaylist>
    {
        public TIDALSearchResultForPlaylists(String title) : base (50, title, "PLAYLISTS", token => new TIDALPlaylist(token))
        {
            
        }
    }

    class TIDALMyPlaylists : TIDALCollectionBase<IStreamingPlaylist>
    {
        internal TIDALMyPlaylists(string title, String userID, Contents.Ordering currentOrder) : base (50, title, token => new TIDALPlaylist(token))
        {
            this.urlForData = ServiceManager.Service(ContentType.TIDAL).URLFor($"users/{userID}/playlists");

            this.supportedOrdering = new List<Contents.Ordering>()
            {
                Contents.Ordering.PlaylistName,
                Contents.Ordering.AddedDateDesc
            };
            this.CurrentOrder = currentOrder;
        }
    }
}
