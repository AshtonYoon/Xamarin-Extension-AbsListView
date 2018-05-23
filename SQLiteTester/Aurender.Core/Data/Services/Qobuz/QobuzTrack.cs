using System;
using System.Collections.Generic;
using Aurender.Core.Utility;
using Newtonsoft.Json.Linq;

namespace Aurender.Core.Data.Services.Qobuz
{

    class QobuzTrack : StreamingTrack
    {
        public bool IsHighRes;
        public List<string> albumCover;

        public QobuzTrack() : base(ContentType.Qobuz)
        {

        }

        public QobuzTrack(JToken aToken) : base(ContentType.Qobuz)
        {
            JToken token = aToken;


            var album = token["album"];

            if (album == null)
            {
                this.AlbumTitle = String.Empty;
                this.StreamingAlbumID = String.Empty;
                this.albumCover = new List<String>() { "", "", "" };
            }
            else
            {
                this.AlbumTitle = album["title"].ToObject<String>();
                this.StreamingAlbumID = album["id"].ToObject<String>();
                this.albumCover = QobuzUtility.CoverLink(album["image"]);
            }



            var artist = token["performer"];
            if (artist != null)
            {
                this.ArtistName = artist["name"].ToObject<String>();
                this.StreamingArtistID = artist["id"].ToObject<String>();
            }
            else
            {
                this.ArtistName = String.Empty;
                this.StreamingArtistID = String.Empty;
            }


            JToken streamable = token["streamable_at"];
            if (streamable != null && streamable.Type != JTokenType.Null) {
                long streamableAt = streamable.ToObject<Int64>();
                this.AllowStreaming = QobuzUtility.IsLaterThanNow(streamableAt);
            }
            else
            {
                this.AllowStreaming = true;
            }

            this.Duration = token["duration"].ToObject<int>();

            this.Title = token["title"].ToObject<String>();

            this.TrackIndex = token["track_number"].ToObject<int>();
            this.DiscIndex = token["media_number"].ToObject<int>();

          //  this.IsPremiumOnly = token["streamable"].ToObject<bool>();

            this.IsHighRes = (16 < token["maximum_bit_depth"].ToObject<int>());

            this.StreamingID = token["id"].ToObject<String>();

        }
          public override string ImageUrl => albumCover[0];

        public override string ImageUrlForMediumSize => albumCover[1];

        public override string ImageUrlForLargeSize => albumCover[2];

        public override object GetExtraImage()
        {
            object source = null;
            if (IsHighRes)
            {
                source = ImageUtility.GetImageSourceFromFile("qobuz_hires.png");
            }

            return source;
        }
    }

}