using System;
using Aurender.Core.Contents.Streaming;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;

namespace Aurender.Core.Data.Services.TIDAL
{

    internal class TIDALPlaylist : StreamingPlaylist
    {
        //protected bool streamReady;
        //private string audioQuality;
        //private bool isExplicit;
        private string albumCover;
        private int duration;
        internal String creatorID { get; private set; }
        private String creatorName;

        public TIDALPlaylist(JToken token) : base(ContentType.TIDAL)
        {
            this.StreamingID = token["uuid"].ToObject<String>();
            this.Name = token["title"].ToObject<String>();

            this.SongCount = token["numberOfTracks"].ToObject<int>();
            this.duration = token["duration"].ToObject<int>();
            this.Description = token["description"].ToObject<String>();

            var creator = token["creator"];
           
            if (creator != null && creator.First != null)
            {
                this.creatorID = creator["id"].ToObject<String>();
                var creatorName = creator["name"];

                if (creatorName != null)
                    this.creatorName = creatorName.ToObject<String>();
                else
                    this.creatorName = String.Empty;
            }
            else
            {
                this.creatorID = String.Empty;
                this.creatorName = String.Empty;
            }


            string cLink = token["image"].ToObject<String>().Replace('-', '/');
            string sizeInfo = "{0}x{1}.jpg";
            this.albumCover = $"https://resources.tidal.com/images/{cLink}/{sizeInfo}";
        }

        public override string ImageUrl => String.Format(albumCover, 160, 107);

        public override string ImageUrlForMediumSize => String.Format(albumCover, 320, 214);

        public override string ImageUrlForLargeSize => String.Format(albumCover, 1080, 720);

        public override async Task<IStreamingObjectCollection<IStreamingTrack>> LoadSongsAsync()
        {
                if (this.Songs == null)
                {
                    this.Songs = new TIDALTracksForPlaylist(this.StreamingID);
                }
                await this.Songs.LoadNextAsync();

                return this.Songs;
        }


    }
    internal class TIDALPlaylistForUserCreated : TIDALPlaylist
    {
        internal String etag { get; set; }

        internal TIDALPlaylistForUserCreated(JToken token, String eTag) : base(token)
        {
            etag = eTag;
        }
    }

    internal class TIDALPlaylistForFavorite : TIDALPlaylist
    {
        public TIDALPlaylistForFavorite(JToken token) : base(token["item"])
        {
        }
    }
}