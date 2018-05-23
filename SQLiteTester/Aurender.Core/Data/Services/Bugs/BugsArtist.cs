using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aurender.Core.Contents.Streaming;

namespace Aurender.Core.Data.Services.Bugs
{
    internal class BugsArtist : StreamingArtist
    {
        Dictionary<String, String> artistImages;

        public BugsArtist(JToken token) : base(ContentType.Bugs)
        {
            this.StreamingID = token["artist_id"].ToString();
            this.ArtistName  = token["disp_nm"].ToString();
            this.artistImages = BugsUtilty.GetImageUrls(token["img_urls"]);
        }

        public override string ImageUrl => artistImages[BugsUtilty.Small];

        public override string ImageUrlForMediumSize => artistImages["350"];

        public override string ImageUrlForLargeSize => artistImages[BugsUtilty.Medium];

        public override async Task<StreamingObjectDescription> LoadBiography()
        {
            if (this.ArtistBiography == null)
            {

                String url = $"artists/{StreamingID}/desc";
                String fullUrl = ServiceManager.Bugs().URLFor(url);

                using (var t = await ServiceManager.Bugs().GetResponseByGetDataAsync(fullUrl, null).ConfigureAwait(false))
                {
                    if (t.IsSuccessStatusCode)
                    {
                        var responseString = await t.Content.ReadAsStringAsync();

                        StreamingObjectDescription description = BugsUtilty.ProcessArtistDescription(responseString);

                        this.ArtistBiography = description;

                    }
                }
            }

            return ArtistBiography;
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
            IStreamingObjectCollection<IStreamingTrack> artists = new BugsTracksForArtist(this);

            await artists.LoadNextAsync().ConfigureAwait(false);

            return artists;
        }

        public override async Task<IStreamingObjectCollection<IStreamingAlbum>> LoadAlbums()
        {
            IStreamingObjectCollection<IStreamingAlbum> albums = new BugsAlbumsForArtist(this);

            await albums.LoadNextAsync();

            return albums;
        }

        public override async Task<IStreamingObjectCollection<IStreamingAlbum>> LoadAllAlbums()
        {
            IStreamingObjectCollection<IStreamingAlbum> albums = new BugsAlbumsForArtist(this);

            await albums.LoadNextAsync().ConfigureAwait(false);

            return albums;
        }
    }
}
