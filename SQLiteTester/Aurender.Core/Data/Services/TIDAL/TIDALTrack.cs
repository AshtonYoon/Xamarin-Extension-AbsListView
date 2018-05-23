using System;
using Aurender.Core.Utility;
using Newtonsoft.Json.Linq;

namespace Aurender.Core.Data.Services.TIDAL
{

    internal class TIDALTrack : StreamingTrack
    {

        public bool streamReady;
        public string audioQuality;
        bool isExplicit;
        bool isMQA;
        bool isPremium;
        public string albumCover;

        public TIDALTrack() : base(ContentType.TIDAL)
        {

        }
        
        public TIDALTrack(JToken aToken) : base(ContentType.TIDAL)
        {
            JToken token = aToken;


            var album = token["album"];

            if (album == null)
            {
                token = aToken["item"];
                album = token["album"];
            }

            this.AlbumTitle = album["title"].ToObject<String>();
            this.StreamingAlbumID = album["id"].ToObject<String>();
            this.albumCover = TIDALUtility.CoverLink(album, StreamingAlbumID);

            var artist = token["artist"];
            if (artist != null)
            {
                this.ArtistName = artist["name"].ToObject<String>();
                this.StreamingArtistID = artist["id"].ToObject<String>();
            }
            else
            {
                var artists = token["artists"];
                if (artists is JArray)
                {
                    artist = artists[0];
                    this.ArtistName = artist["name"].ToObject<String>();
                    this.StreamingArtistID = artist["id"].ToObject<String>();
                }
            }
            this.AllowStreaming = token["allowStreaming"].ToObject<bool>();

            this.Duration = token["duration"].ToObject<int>();

            this.Title = token["title"].ToObject<String>();

            this.TrackIndex = token["trackNumber"].ToObject<int>();
            this.DiscIndex = token["volumeNumber"].ToObject<int>();

            this.isPremium = token["premiumStreamingOnly"].ToObject<bool>();
            this.streamReady = token["streamReady"].ToObject<bool>();
            this.audioQuality = token["audioQuality"].ToObject<String>();
            this.StreamingID = token["id"].ToObject<String>();
            this.isExplicit = token["explicit"].ToObject<bool>();
            string audioQuality = token["audioQuality"].ToString();
            this.isMQA = "HI_RES".Equals(audioQuality);
        }

        public override bool IsPremiumOnly => isPremium; 

        public override string ImageUrl => albumCover != null ? string.Format(albumCover, 160) : null;

        public override string ImageUrlForMediumSize => albumCover != null ? string.Format(albumCover, 320) : null;

        public override string ImageUrlForLargeSize => albumCover != null ? string.Format(albumCover, 1080) : null;

        public override object GetExtraImage()
        {
            string source = null;
            if (isExplicit && isMQA)
            {
                source = "tidal_exclusive_mqa.png";
            }
            else if (isExplicit)
            {
                source = "tidal_explicit.png";
            }
            else if (isMQA)
            {
                source = "tidal_mqa.png";
            }

            return ImageUtility.GetImageSourceFromFile(source);
        }
    }

    internal class TIDALTrackForFavorite : TIDALTrack
    {
        public TIDALTrackForFavorite(JToken token) : base(token["item"])
        {
        }
    }
}