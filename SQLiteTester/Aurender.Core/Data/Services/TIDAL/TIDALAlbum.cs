using System;
using System.Threading.Tasks;
using Aurender.Core.Contents.Streaming;
using Aurender.Core.Utility;
using Newtonsoft.Json.Linq;

namespace Aurender.Core.Data.Services.TIDAL
{

    internal class TIDALAlbum : StreamingAlbum
    {
        protected bool streamReady;
        protected string audioQuality;
        protected bool isExplicit;
        protected string albumCover;

        private bool IsMQA
        {
            get => this.audioQuality == "HI_RES";
        }

        public TIDALAlbum(JToken token) : base(ContentType.TIDAL)
        {
            //        var album = token["album"];
            this.AlbumTitle = token["title"].ToObject<String>();
            this.StreamingID = token["id"].ToObject<String>();
            this.albumCover = TIDALUtility.CoverLink(token, this.StreamingID);
            this.AllowStreaming = token["allowStreaming"].ToObject<bool>();
            this.duration = token["duration"].ToObject<int>();
            this.audioQuality = token["audioQuality"].ToObject<String>();
            this.streamReady = token["streamReady"].ToObject<bool>();
            this.AlbumType = token["type"].ToObject<String>();

            this.NumberOfDisc = token["numberOfVolumes"].ToObject<byte>();
            this.Publisher = token["copyright"].ToObject<String>();
            var releaseDate = token["releaseDate"];

            if (releaseDate.Type == JTokenType.Null)
            {
                this.ReleaseYear = 0;
            }
            else
            {
                this.ReleaseYear = short.Parse(releaseDate.ToObject<String>().Substring(0, 4) ?? "0");
            }

            var artist = token["artist"];
            if (artist != null)
            {
                this.AlbumArtistName = artist["name"].ToObject<String>();
                this.StreamingArtistID = artist["id"].ToObject<String>();
            }
            else
            {
                /// I18N
                this.AlbumArtistName = "N/A";
                this.StreamingArtistID = String.Empty;

            }

            this.isExplicit = token["explicit"].ToObject<bool>();
        }

        public override string ImageUrl => albumCover != null ? String.Format(albumCover, 160) : null;

        public override string ImageUrlForMediumSize => albumCover != null ? String.Format(albumCover, 320) : null;

        public override string ImageUrlForLargeSize => albumCover != null ? String.Format(albumCover, 1080) : null;

        public override Credits AlbumCredit()
        {
            IARLogStatic.Error("TIDALAlbum", "Doesn't support AlbumCredit yet");
            return null;
        }

        public override object GetExtraImage()
        {
            string source = null;
            if (isExplicit && IsMQA)
            {
                source = "tidal_exclusive_mqa.png";
            }
            else if (isExplicit)
            {
                source = "tidal_explicit.png";
            }
            else if (IsMQA)
            {
                source = "tidal_mqa.png";
            }
            return ImageUtility.GetImageSourceFromFile(source);
        }

        public override object IconForAdditionalInfo()
        {
            IARLogStatic.Error("TIDALAlbum", "Doesn't support AlbumCredit yet");
            return null;
        }

        public override async Task<StreamingObjectDescription> LoadDescriptionAsync()
        {
            if (this.Description != null)
            {
                return this.Description;
            }

            String url = $"albums/{StreamingID}/credits";
            String fullUrl = ServiceManager.Service(ContentType.TIDAL).URLFor(url, "includeImageLinks=false");

            var t = await WebUtil.DownloadContentsAsync(fullUrl).ConfigureAwait(false);

            StreamingObjectDescription description;

            if (t.Item1)
            {
                description = TIDALUtility.ProcessCredits(t);
            }
            else
            {
                IARLogStatic.Log("TIDAL Album", $"Failed to load description [{this}] \n\t{t.Item2}");
                description = new StreamingObjectDescription();
            }

            url = $"albums/{StreamingID}/review";
            fullUrl = ServiceManager.Service(ContentType.TIDAL).URLFor(url, "includeImageLinks=false");

            t = await WebUtil.DownloadContentsAsync(fullUrl).ConfigureAwait(false);

            if (t.Item1)
            {
                var review = TIDALUtility.ProcessDescription(t);
                foreach (var element in review)
                    description.Add(element.Key, element.Value);
            }
            else
            {
                IARLogStatic.Log("TIDAL Album", $"Failed to load description [{this}] \n\t{t.Item2}");
                description.Add(DescriptionField.Review, "N/A");
            }

            this.Description = description;

            return this.Description;

        }

        public override async Task<IStreamingObjectCollection<IStreamingTrack>> LoadSongsAsync()
        {
            if (this.Tracks == null)
            {
                this.Tracks = new TIDALTracksForAlbum(this.StreamingID);
            }
            await this.Tracks.LoadNextAsync().ConfigureAwait(false);

            return this.Tracks;
        }

    }

    internal class TIDALAlbumForFavorite : TIDALAlbum
    {
        public TIDALAlbumForFavorite(JToken token) : base(token["item"])
        {
        }
    }

    
}