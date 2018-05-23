using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aurender.Core.Contents.Streaming;
using Newtonsoft.Json.Linq;

namespace Aurender.Core.Data.Services.Melon
{

    class MelonArtist : StreamingArtist
    {
        protected String artistImage;
        protected String artistImageLarge;
        protected IStreamingObjectCollection<IStreamingAlbum> albums;


        public MelonArtist(JToken token) : base(ContentType.Melon)
        {
            this.StreamingID = token["ARTISTID"].ToString();
            this.ArtistName = token["ARTISTNAME"].ToString();
            this.artistImage = token["ARTISTIMG"].ToString();
            if (token["ARTISTIMG"] != null)
                artistImage = token["ARTISTIMG"].ToString();
            else
                artistImage = String.Empty;

            if (token["ARTISTIMGLARGE"] != null)
                artistImageLarge = token["ARTISTIMGLARGE"].ToString();
            else
                artistImageLarge = artistImage;

            StreamingObjectDescription description = MelonUtility.ProcessArtistDescription(token.ToString());

            this.ArtistBiography = description;
        }

        internal MelonArtist(JToken token, bool withAlbums) : this(token)
        {
                
        }
        
        public override string ImageUrl => this.artistImage;

        public override string ImageUrlForMediumSize => ImageUrl;

        public override string ImageUrlForLargeSize => ImageUrl;

        public override async Task<StreamingObjectDescription> LoadBiography()
        {
            await Task.Delay(0).ConfigureAwait(false);
            return this.ArtistBiography;
            /*return Task.Run(() =>
            {
                if (this.ArtistBiography == null)
                {
                    var 
                    if (t != String.Empty)
                    {
                        StreamingObjectDescription description = MelonUtility.ProcessArtistDescription("");

                        this.ArtistBiography = description;
                    }
                }

                return ArtistBiography;
            }
            );*/
        }

        public override Task<IStreamingObjectCollection<IStreamingArtist>> LoadRelatedArtists()
        {
            IARLogStatic.Error("BugsArtist", "Doesn't support biography yet");
            return null;
        }

        public override Task<IStreamingObjectCollection<IStreamingArtist>> LoadSimilarArtists()
        {

            IARLogStatic.Error("BugsArtist", "Doesn't support biography yet");
            return null;
        }

        public override async Task<IStreamingObjectCollection<IStreamingTrack>> LoadTopTracks()
        {
                IStreamingObjectCollection<IStreamingTrack> artists = new MelonTracksForArtist(this);

                await artists.LoadNextAsync().ConfigureAwait(false);

                return artists;
        }
        public override async Task<IStreamingObjectCollection<IStreamingAlbum>> LoadAlbums()
        {
            if (albums != null)
                return albums;

            IStreamingObjectCollection<IStreamingAlbum> artistAlbums = new MelonAlbumsForArtist(this);

            await artistAlbums.LoadNextAsync().ConfigureAwait(false);

            return artistAlbums;
        }

        public override async Task<IStreamingObjectCollection<IStreamingAlbum>> LoadAllAlbums()
        {
            if (albums != null)
                return albums;

            IStreamingObjectCollection<IStreamingAlbum> artistAlbums = new MelonAlbumsForArtist(this);

            await artistAlbums.LoadNextAsync().ConfigureAwait(false);

            artistAlbums.AlbumsByType = new Dictionary<string, List<IStreamingAlbum>>
            {
                { "All", artistAlbums.ToList() }
            };

            return artistAlbums;
        }


      
    }

}