using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aurender.Core.Contents.Streaming;
using Aurender.Core.Utility;
using Newtonsoft.Json.Linq;

namespace Aurender.Core.Data.Services.Bugs
{
    internal class BugsAlbum : StreamingAlbum
    {
        protected StreamingTrackFeatures feature;
        protected Dictionary<String, String> albumCover;
        protected String genre;
        //protected String desc;

        protected BugsAlbum() : base(ContentType.Bugs)
        {

        }

        public BugsAlbum(JToken token) : this()
        {
            //        var album = token["album"];
            this.AlbumTitle = token["title"].ToObject<String>();
            this.StreamingID = token["album_id"].ToObject<String>();
            this.AlbumType = BugsUtilty.GetAlbumType(token["album_tp"].ToString());
            this.albumCover = BugsUtilty.GetImageUrls(token["img_urls"]);

            this.AllowStreaming = true;
            this.genre = token["style_str"].ToString();

            this.NumberOfDisc = 0;
            this.Publisher = "";
            
            String year = token["release_ymd"]?.ToString()?.Substring(0, 4) ?? "0";
            short intYear = 0;
            short.TryParse(year, out intYear);
            this.ReleaseYear = intYear;

            this.Description = new Contents.Streaming.StreamingObjectDescription();
            JToken desc = token["desc"];
            if (desc != null)
            {
                this.Description.Add(DescriptionField.Content, desc.ToString());
            }
            feature = 0;
            if (token["bside_yn"].ToString() == "Y")
                feature = StreamingTrackFeatures.BSide;

            this.AlbumArtistName = token["artist_disp_nm"].ToObject<String>();
            this.StreamingArtistID = token["artist_id"].ToObject<String>();

        }

        public override string ImageUrl => albumCover[BugsUtilty.Small];

        public override string ImageUrlForMediumSize => albumCover[BugsUtilty.Medium];

        public override string ImageUrlForLargeSize => albumCover[BugsUtilty.Large];

        public override Credits AlbumCredit()
        {
            IARLogStatic.Error("BugsAlbum", "Doesn't support icon yet");
            return null;
        }

        public override object IconForAdditionalInfo()
        {
            IARLogStatic.Error("BugsAlbum", "Doesn't support icon yet");
            return null;
        }

        public override async Task<StreamingObjectDescription> LoadDescriptionAsync()
        {
            String url = $"albums/{StreamingID}/desc";
            String fullUrl = ServiceManager.Bugs().URLFor(url);

            using(var result = await ServiceManager.Bugs().GetResponseByGetDataAsync(fullUrl, null).ConfigureAwait(false)) {

                if (result.IsSuccessStatusCode)
                {
                    var responseString = await result.Content.ReadAsStringAsync();

                    StreamingObjectDescription description = BugsUtilty.ProcessAlbumDescription(responseString);

                    return description;
                }
            }
            return null;
        }

        public override async Task<IStreamingObjectCollection<IStreamingTrack>> LoadSongsAsync()
        {
            if (this.Tracks == null)
            {
                this.Tracks = new BugsTracksForAlbum(this);
            }
            await this.Tracks.LoadNextAsync().ConfigureAwait(false);

            return this.Tracks;
        }

        public override object GetExtraImage()
        {
            string source = null;

            if (feature.HasFlag(StreamingTrackFeatures.Adult) &&
                feature.HasFlag(StreamingTrackFeatures.Prime) &&
                feature.HasFlag(StreamingTrackFeatures.BSide))
            {
                source = "bugs_19_premium_bside.png";
            }
            else if (feature.HasFlag(StreamingTrackFeatures.BSide) &&
                feature.HasFlag(StreamingTrackFeatures.Adult))
            {
                source = "bugs_beside_19.png";
            }
            else if (feature.HasFlag(StreamingTrackFeatures.Prime) &&
                feature.HasFlag(StreamingTrackFeatures.Adult))
            {
                source = "bugs_premium_19.png";
            }
            else if (feature.HasFlag(StreamingTrackFeatures.BSide) &&
                feature.HasFlag(StreamingTrackFeatures.Prime))
            {
            }
            else if (feature.HasFlag(StreamingTrackFeatures.BSide))
            {
                source = "bugs_bside.png";
            }
            else if (feature.HasFlag(StreamingTrackFeatures.Prime))
            {
                source = "bugs_premium.png";
            }
            else if (feature.HasFlag(StreamingTrackFeatures.Adult))
            {
                source = "adult.png";
            }

            return ImageUtility.GetImageSourceFromFile(source);
        }
    }
}
