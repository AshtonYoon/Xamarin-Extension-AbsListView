using System;
using System.Threading.Tasks;
using Aurender.Core.Contents.Streaming;
using Aurender.Core.Utility;
using Newtonsoft.Json.Linq;

namespace Aurender.Core.Data.Services.TIDAL
{

    class TIDALArtist : StreamingArtist
    {
        protected string artistImage;
        bool hasPicture;

        public TIDALArtist(JToken aToken) : base(ContentType.TIDAL)
        {
            JToken token = aToken;

            var item = token["id"];

            if (item == null)
            {
                token = aToken["items"];
                item = token["id"];
            }

            this.StreamingID = item.ToObject<string>();

            this.ArtistName = token["name"].ToObject<String>();

            String cLink = token["picture"].ToObject<String>()?.Replace('-', '/');
            String sizeInfo = "{0}x{1}.jpg";

            this.artistImage = $"https://resources.tidal.com/images/{cLink}/{sizeInfo}";
            hasPicture = cLink != null;
        }

        public override string ImageUrl => hasPicture ? String.Format(artistImage, 160, 107) : null;

        public override string ImageUrlForMediumSize => hasPicture ? String.Format(artistImage, 320, 214) : null;

        public override string ImageUrlForLargeSize => hasPicture ? String.Format(artistImage, 1080, 720) : null;

        public override async Task<StreamingObjectDescription> LoadBiography()
        {
            if (this.ArtistBiography == null)
            {

                String url = $"artists/{StreamingID}/bio";
                String fullUrl = ServiceManager.Service(ContentType.TIDAL).URLFor(url, "includeImageLinks=false");

                var t = await WebUtil.DownloadContentsAsync(fullUrl).ConfigureAwait(false);

                if (t.Item1)
                {

                    StreamingObjectDescription description = TIDALUtility.ProcessDescription(t);

                    this.ArtistBiography = description;

                }

            }

            return ArtistBiography;
        }

        public override async Task<IStreamingObjectCollection<IStreamingArtist>> LoadRelatedArtists()
        {
            IStreamingObjectCollection<IStreamingArtist> artists = new TIDALRelatedArtists("Related artists", this.StreamingID);

            await artists.LoadNextAsync().ConfigureAwait(false);

            return artists;
        }

        public override async Task<IStreamingObjectCollection<IStreamingArtist>> LoadSimilarArtists()
        {
            IStreamingObjectCollection<IStreamingArtist> artists = new TIDALSimilarArtists("Similar artists", this.StreamingID);

            await artists.LoadNextAsync().ConfigureAwait(false);

            return artists;
        }

        public override async Task<IStreamingObjectCollection<IStreamingTrack>> LoadTopTracks()
        {
            IStreamingObjectCollection<IStreamingTrack> artists = new TIDALTopTracksForArtist("Top tracks", this.StreamingID);

            await artists.LoadNextAsync().ConfigureAwait(false);

            return artists;
        }
        public override async Task<IStreamingObjectCollection<IStreamingAlbum>> LoadAlbums()
        {
            IStreamingObjectCollection<IStreamingAlbum> albums = new TIDALAlbumsForArtist(this);
            await albums.LoadNextAsync().ConfigureAwait(false);
            return albums;
        }

        public override async Task<IStreamingObjectCollection<IStreamingAlbum>> LoadAllAlbums()
        {
            IStreamingObjectCollection<IStreamingAlbum> albums = new TIDALAlbumsForArtist(this);
            await albums.LoadNextAsync().ConfigureAwait(false);

            return albums;
        }
    }

    class TIDALArtistForFavorite : TIDALArtist
    {
        public TIDALArtistForFavorite(JToken token) : base(token["item"])
        {
        }

    }

    class TIDALSimilarArtists : TIDALCollectionBase<IStreamingArtist>
    {
        internal TIDALSimilarArtists(string title, String artistID) : base(50, title, token => new TIDALArtist(token))
        {
            String url = $"artists/{artistID}/similar";
            this.urlForData = ServiceManager.Service(ContentType.TIDAL).URLFor(url);
        }
    }

    class TIDALRelatedArtists : TIDALCollectionBase<IStreamingArtist>
    {
        internal TIDALRelatedArtists(string title, String artistID) : base(50, title, token => new TIDALArtist(token))
        {
            String url = $"artists/{artistID}/related";
            this.urlForData = ServiceManager.Service(ContentType.TIDAL).URLFor(url);
        }
    }
}