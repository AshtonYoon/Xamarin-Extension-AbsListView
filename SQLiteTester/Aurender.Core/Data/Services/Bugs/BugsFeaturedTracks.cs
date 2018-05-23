using Aurender.Core.Contents.Streaming;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace Aurender.Core.Data.Services.Bugs
{
    class BugsFeaturedTracks : BugsCollectionBase<IStreamingTrack>
    {
        protected BugsFeaturedTracks(string title, int count, Type t) : base(count, typeof(BugsTrack), title)
        {

        }
     
        public BugsFeaturedTracks(string title, String url) : base(50, typeof(BugsTrack), title)
        {
            this.urlForData = ServiceManager.Bugs().URLFor(url);
        }
    }


    class BugsFavoriteTracks : BugsFeaturedTracks
    {
        internal BugsFavoriteTracks(String title) : base(title, SSBugs.FavoriteBucketSize, typeof(BugsAlbum))
        {
            this.urlForData = ServiceManager.Service(ServiceType).URLFor($"me/likes/track");
        }
    }
    class BugsTracksForAlbum : BugsFeaturedTracks
    {
        protected BugsTracksForAlbum(string title, String url) : base(title, url)
        {
        }

        public BugsTracksForAlbum(BugsAlbum album) : base(album.AlbumTitle, $"albums/{album.StreamingID}")
        {
        }

        protected override bool ProcessItems(Dictionary<string, object> sInfo, IList<IStreamingTrack> newItems)
        {
            JToken result = sInfo["result"] as JToken;
            if (result != null)
            {
                var items = result["tracks"] as JArray;

                foreach (var i in items)
                {
                    var item = new BugsTrack(i);
                    newItems.Add(item);
                }

                this.Count = newItems.Count;

                return true;
            }
            else
            {
                this.EP("BugsTracksForAlbum", "Failed to load tracks");
                return false;
            }
        }
    }
    class BugsTracksForPlaylist : BugsTracksForAlbum
    {
        public BugsTracksForPlaylist(BugsPlaylistForESAlbum playlist) : base(playlist.Name, $"musicpd/{playlist.StreamingID}")
        {
        }
        public BugsTracksForPlaylist(BugsPlaylist playlist) : base(playlist.Name, $"musicpd/{playlist.StreamingID}")
        {
        }
        public BugsTracksForPlaylist(BugsMyPlaylist playlist) : base(playlist.Name, $"myalbums/{playlist.StreamingID}")
        {
        }

    }



    class BugsTracksForArtist : BugsFeaturedTracks
    {
        public BugsTracksForArtist(BugsArtist artist) : base(artist.ArtistName, $"artists/{artist.StreamingID}/tracks")
        {
        }
    }

    class BugsSearchResultForTracks : BugsSearchCollectionBase<IStreamingTrack>
    {
        public BugsSearchResultForTracks(String title) : base (typeof(BugsTrack), title, "track")
        {

        }
    }
   class BugsSearchResultForAlbums : BugsSearchCollectionBase<IStreamingAlbum>
    {
        public BugsSearchResultForAlbums(String title) : base (typeof(BugsAlbum), title, "album")
        {

        }
    
    } class BugsSearchResultForArtists : BugsSearchCollectionBase<IStreamingArtist>
    {
        public BugsSearchResultForArtists(String title) : base (typeof(BugsArtist), title, "artist")
        {

        }
    }
  class BugsSearchResultForPlaylist : BugsSearchCollectionBase<IStreamingPlaylist>
    {
        public BugsSearchResultForPlaylist(String title) : base (typeof(BugsPlaylistForESAlbum), title, "esalbum")
        {

        }
    }
}
