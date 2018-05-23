using System;
using System.Collections.Generic;
using Aurender.Core.Contents.Streaming;
using Aurender.Core.Contents;

namespace Aurender.Core.Data.Services.TIDAL
{
    class TIDALFeaturedTracks : TIDALCollectionBase<IStreamingTrack>
    {
        public TIDALFeaturedTracks(string title, string url) : base(50, title, token => new TIDALTrack(token))
        {
            this.urlForData = ServiceManager.Service(ContentType.TIDAL).URLFor(url);
        }

    }


    class TIDALTracksForAlbum : TIDALCollectionBase<IStreamingTrack>
    {
        public TIDALTracksForAlbum(String albumID) : base(200, "", token => new TIDALTrack(token))
        {
            this.urlForData = ServiceManager.Service(ServiceType).URLFor($"albums/{albumID}/tracks");
        }
    }


    class TIDALTracksForPlaylist : TIDALCollectionBase<IStreamingTrack>
    {
        public TIDALTracksForPlaylist(String playlistID) : base(100, "", token => new TIDALTrack(token))
        {
            this.urlForData = ServiceManager.Service(ServiceType).URLFor($"playlists/{playlistID}/items", "order=INDEX&orderDirection=ASC");
        }
    }


    class TIDALTopTracksForArtist : TIDALCollectionBase<IStreamingTrack>
    {
        public TIDALTopTracksForArtist(String title, String artistID) : base(50, title, token => new TIDALTrack(token))
        {
            this.urlForData = ServiceManager.Service(ServiceType).URLFor($"artists/{artistID}/toptracks");
        }
    }

    class TIDALSearchResultForTracks : TIDALSearchCollectionBase<IStreamingTrack>
    {
        public TIDALSearchResultForTracks(String title) : base (50, title, "TRACKS", token => new TIDALTrack(token))
        {
            
        }


    }

    class TIDALFavoriteTracks : TIDALCollectionBase<IStreamingTrack>
    {
        internal TIDALFavoriteTracks (String title, String userId, Ordering defaultOrder) : base (50, title, token => new TIDALTrackForFavorite(token))
        {
            this.urlForData = ServiceManager.Service(ServiceType).URLFor($"users/{userId}/favorites/tracks");
            this.CurrentOrder = defaultOrder;
            this.supportedOrdering = new List<Ordering>()
            {
               Ordering.AlbumName, //ALBUM
               Ordering.SongName, //"NAME"
               Ordering.AddedDateDesc, //DATE -> DSC
               Ordering.ArtistName //ARTIST
            };
        }

        protected override string GetOrderClause()
        {
            String order;
            switch(CurrentOrder)
            {
                case Ordering.ArtistName:
                    order = "&order=ARTIST&orderDirection=ASC";
                    break;

                case Ordering.AlbumName:
                    order = "&order=ALBUM&orderDirection=ASC";
                    break;

                default:
                    order = base.GetOrderClause();
                    break;
            }
            return order;
        }
    }

    class TIDALFavoriteAlbums : TIDALCollectionBase<IStreamingAlbum>
    {
        internal TIDALFavoriteAlbums (String title, String userId, Ordering defaultOrder) : base (50, title, token => new TIDALAlbumForFavorite(token))
        {
            this.urlForData = ServiceManager.Service(ServiceType).URLFor($"users/{userId}/favorites/albums");
            this.CurrentOrder = defaultOrder;
            this.supportedOrdering = new List<Ordering>()
            {
               Ordering.AlbumName, //ALBUM
               Ordering.AddedDateDesc, //DATE -> DSC
               Ordering.ReleaseDateDesc, //"NAME"
               Ordering.ArtistName, //ARTIST
            };
        }

        protected override string GetOrderClause()
        {
            if (CurrentOrder == Ordering.ArtistName)
            {
                return "&order=ARTIST&orderDirection=ASC";

            }
            else
                return base.GetOrderClause();
        }


    }
    class TIDALFavoriteArtists : TIDALCollectionBase<IStreamingArtist>
    {
        internal TIDALFavoriteArtists (String title, String userId, Ordering defaultOrder) : base (50, title, token => new TIDALArtistForFavorite(token))
        {
            this.urlForData = ServiceManager.Service(ServiceType).URLFor($"users/{userId}/favorites/artists");
            this.CurrentOrder = defaultOrder;
            this.supportedOrdering = new List<Ordering>()
            {
               Ordering.ArtistName, //ARTIST
               Ordering.AddedDateDesc //DATE -> DSC
            };
        }
    }
    class TIDALFavoritePlaylists : TIDALCollectionBase<IStreamingPlaylist>
    {
        internal TIDALFavoritePlaylists (String title, String userId, Ordering defaultOrder) : base (50, title, token => new TIDALPlaylistForFavorite(token))
        {
            this.urlForData = ServiceManager.Service(ServiceType).URLFor($"users/{userId}/favorites/playlists");
            this.CurrentOrder = defaultOrder;
            this.supportedOrdering = new List<Ordering>()
            {
               Ordering.PlaylistName, // NAME
               Ordering.AddedDateDesc //DATE -> DSC
            };
        }
    }
}
