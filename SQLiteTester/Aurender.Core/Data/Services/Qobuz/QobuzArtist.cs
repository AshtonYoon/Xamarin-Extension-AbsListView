using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aurender.Core.Contents.Streaming;
using Newtonsoft.Json.Linq;

namespace Aurender.Core.Data.Services.Qobuz
{

    class QobuzArtist : StreamingArtist
    {
        protected List<String> artistImage;
        protected String biography;

        public QobuzArtist(JToken token) : base(ContentType.Qobuz)
        {
            this.StreamingID = token["id"].ToObject<string>();

            this.ArtistName = token["name"].ToObject<String>();
            this.biography = token["biography"]?.ToString() ?? String.Empty;                


           
            this.artistImage = QobuzUtility.CoverLinkForArtist(token);

        }

        public override string ImageUrl => artistImage[0];

        public override string ImageUrlForMediumSize => artistImage[1];

        public override string ImageUrlForLargeSize => artistImage[2];

        public override async Task<StreamingObjectDescription> LoadBiography()
        {
            await this.LoadArtistDetail().ConfigureAwait(false);
            if (this.ArtistBiography == null)
            {
                StreamingObjectDescription description = QobuzUtility.ProcessDescription(biography);

                this.ArtistBiography = description;
            }

            return ArtistBiography;
        }

        protected async Task LoadArtistDetail()
        {
            var newArtist = await ServiceManager.Qobuz().GetArtistWithIDAsync(this.StreamingID).ConfigureAwait(false) as QobuzArtist;

            this.artistImage = newArtist.artistImage;
            this.biography = newArtist.biography;
        }


        public override async Task<IStreamingObjectCollection<IStreamingArtist>> LoadSimilarArtists()
        {
            IStreamingObjectCollection<IStreamingArtist> similar = new QobuzSimilarArtists(this);

            await similar.LoadNextAsync().ConfigureAwait(false);

            return similar;
        }


        public override async Task<IStreamingObjectCollection<IStreamingAlbum>> LoadAlbums()
        {
            IStreamingObjectCollection<IStreamingAlbum> similar = new QobuzAlbumsForArtist(this);

            await similar.LoadNextAsync().ConfigureAwait(false);

            return similar;
        }

        public override Task<IStreamingObjectCollection<IStreamingArtist>> LoadRelatedArtists()
        {
            throw new NotImplementedException();
        }

        public override Task<IStreamingObjectCollection<IStreamingTrack>> LoadTopTracks()
        {
            throw new NotImplementedException();
        }

        public override async Task<IStreamingObjectCollection<IStreamingAlbum>> LoadAllAlbums()
        {
            IStreamingObjectCollection<IStreamingAlbum> similar = new QobuzAlbumsForArtist(this);

            await similar.LoadNextAsync().ConfigureAwait(false);

            return similar;
        }
    }

}