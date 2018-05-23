using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aurender.Core.Contents.Streaming;
using Aurender.Core.Utility;
using Newtonsoft.Json.Linq;

namespace Aurender.Core.Data.Services.Qobuz
{

    class QobuzAlbum : StreamingAlbum
    {
       protected bool streamReady;
        protected bool IsHighRes;
        protected List<string> albumCover;

        protected QobuzAlbum() : base(ContentType.Qobuz) { }

        public QobuzAlbum(JToken token) : this()
        {
            //        var album = token["album"];
            this.AlbumTitle = token["title"].ToObject<String>();
            this.StreamingID = token["id"].ToObject<String>();

            this.albumCover = QobuzUtility.CoverLink(token["image"]);

            this.duration = token["duration"].ToObject<int>();

            this.IsHighRes = token["hires"].ToObject<bool>();

            this.streamReady = token["streamable"].ToObject<bool>();

            this.AlbumType = ""; // token["type"].ToObject<String>();

            /// tracks_count
            this.NumberOfDisc = token["media_count"].ToObject<byte>();

            var label = token["label"];

            if (label != null)
            {
                this.Publisher = label["name"].ToString();
            }
            else
            {
                this.Publisher = "";
            }

            var releaseDate = token["released_at"];

            if (releaseDate != null && releaseDate.Type != JTokenType.Null) {
                var releaseDateString = QobuzUtility.GetYYYYMMDDFromTickFrom1970(releaseDate.ToObject<long>());
            this.ReleaseYear = short.Parse(releaseDateString.Substring(0, 4));
            }
            else {
                this.ReleaseYear = 0;
            }

       
             JToken streamable = token["streamable_at"];
            if (streamable != null && streamable.Type != JTokenType.Null)
            {
                long streamableAt = token["streamable_at"].ToObject<Int64>();
                this.AllowStreaming = QobuzUtility.IsLaterThanNow(streamableAt);
            }
            else
            {
                this.AllowStreaming = true;
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

            var tracksToken = token["tracks"];
            if (tracksToken != null)
            {
                this.Tracks = new QobuzTracksForAlbum(tracksToken);
            }

            var description = token["description"];

            if (description != null)
            {
                this.Description = new StreamingObjectDescription();
                this.Description.Add(DescriptionField.Content, description.ToString());

            }
        }

        public override string ImageUrl => albumCover[0];

        public override string ImageUrlForMediumSize => albumCover[1];

        public override string ImageUrlForLargeSize => albumCover[2];

        public override Credits AlbumCredit()
        {
            throw new NotImplementedException();
        }

        public override object IconForAdditionalInfo()
        {
            throw new NotImplementedException();
        }

        public override Task<StreamingObjectDescription> LoadDescriptionAsync()
        {
            if (this.Description == null)
            {
                this.Description = new StreamingObjectDescription();
            }

            return Task.FromResult(this.Description);
        }

        public override async Task<IStreamingObjectCollection<IStreamingTrack>> LoadSongsAsync()
        {
            if (Tracks == null)
            {
                this.Tracks = new QobuzTracksForAlbumFromNetwork(this);
                await this.Tracks.LoadNextAsync().ConfigureAwait(false);
            }
            return Tracks;
        }

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



    class QobuzAlbumForSearchResult : QobuzAlbum
    {
        public QobuzAlbumForSearchResult(JToken token) : base()
        {
            //        var album = token["album"];
            this.AlbumTitle = token["title"].ToObject<String>();
            this.StreamingID = token["id"].ToObject<String>();

            this.albumCover = QobuzUtility.CoverLink(token["image"]);

            this.duration = token["duration"].ToObject<int>();

            this.IsHighRes = token["hires"].ToObject<bool>();

            this.streamReady = token["streamable"].ToObject<bool>();

            this.AlbumType = ""; // token["type"].ToObject<String>();

            /// tracks_count
            this.NumberOfDisc = token["media_count"].ToObject<byte>();

            var label = token["label"];

            if (label != null)
            {
                this.Publisher = label["name"].ToString();
            }
            else
            {
                this.Publisher = "";
            }

            var releaseDate = token["released_at"];

            if (releaseDate != null && releaseDate.Type != JTokenType.Null) {
                var releaseDateString = QobuzUtility.GetYYYYMMDDFromTickFrom1970(releaseDate.ToObject<long>());
            this.ReleaseYear = short.Parse(releaseDateString.Substring(0, 4));
            }
            else {
                this.ReleaseYear = 0;
            }

       
             JToken streamable = token["streamable_at"];
            if (streamable != null && streamable.Type != JTokenType.Null)
            {
                long streamableAt = token["streamable_at"].ToObject<Int64>();
                this.AllowStreaming = QobuzUtility.IsLaterThanNow(streamableAt);
            }
            else
            {
                this.AllowStreaming = true;
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

            var tracksToken = token["tracks"];
            if (tracksToken != null)
            {
                this.Tracks = new QobuzTracksForAlbum(tracksToken);
            }

            var description = token["description"];

            if (description != null)
            {
                this.Description = new StreamingObjectDescription();
                this.Description.Add(DescriptionField.Content, description.ToString());

            }
        }


    }
}