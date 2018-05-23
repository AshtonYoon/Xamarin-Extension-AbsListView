using System;
using System.Threading.Tasks;
using Aurender.Core.Contents.Streaming;
using Newtonsoft.Json.Linq;

namespace Aurender.Core.Data.Services.Qobuz
{

    class QobuzPlaylist : StreamingPlaylist
    {
        //protected bool streamReady;
        //private string audioQuality;
        //private bool isExplicit;
        private string albumCover;
        private int duration;
        internal String creatorID { get; private set; }

        public override string ImageUrl => albumCover;
        public override string ImageUrlForMediumSize => albumCover;

        public override string ImageUrlForLargeSize => albumCover;

        private String creatorName;

        public QobuzPlaylist(JToken token) : base(ContentType.Qobuz)
        {
            this.StreamingID = token["id"].ToObject<String>();
            this.Name = token["name"].ToObject<String>();

            this.SongCount = token["tracks_count"].ToObject<int>();
            this.duration = token["duration"].ToObject<int>();
            this.Description = token["description"].ToObject<String>();

            var creator = token["owner"];

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



            this.albumCover = QobuzUtility.CoverLinkForPlaylist(token["images300"]);

            var tracksToken = token["tracks"];
            if (tracksToken != null)
            {
                this.Songs = new QobuzPlaylistTracks(this.StreamingID, tracksToken);
            }
        }

        public override async Task<IStreamingObjectCollection<IStreamingTrack>> LoadSongsAsync()
        {
            if (this.Songs == null)
            {
                this.Songs = new QobuzPlaylistTracks(this);
            }

            await this.Songs.LoadNextAsync().ConfigureAwait(false);
            return this.Songs;
        }

        internal void UpdateTitleAndDescriptionAndResetSongs(String newTitle, String newDescritpion)
        {
            this.Name = newTitle;
            this.Description = newDescritpion;

            this.Songs.Reset();
            this.Songs.LoadNextAsync().Wait();

            this.SongCount = this.Songs.Count;
        }
    }

    /*
    class QobuzUserPlaylist : QobuzPlaylist
    {
        public QobuzUserPlaylist(JToken token) : base(token)
        {
        }

}*/
    
}