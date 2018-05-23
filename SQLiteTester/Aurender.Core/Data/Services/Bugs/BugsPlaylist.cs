using Aurender.Core.Contents.Streaming;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Aurender.Core.Data.Services.Bugs
{
    class BugsPlaylist : StreamingPlaylist
    {
        internal protected String creatorID { get; protected set; }
        protected Dictionary<String, String> artistImages;

        protected BugsPlaylist() : base(ContentType.Bugs)
        {

        }

        public BugsPlaylist(JToken token) : this()
        {
            this.StreamingID = token["playlist_id"].ToObject<String>();
            this.Name = token["title"].ToObject<String>();

            this.SongCount = token["track_count"].ToObject<int>();

            this.Description = token["descr"].ToObject<String>();

            this.creatorID = token["msrl"].ToObject<String>();

            this.artistImages = BugsUtilty.GetImageUrls(token["img_urls"]);

        }

        public override string ImageUrl => artistImages.TryGetValue(BugsUtilty.Small, out var value) ? value : null;

        public override string ImageUrlForMediumSize => artistImages.TryGetValue(BugsUtilty.Medium, out var value) ? value : null;

        public override string ImageUrlForLargeSize => artistImages.TryGetValue(BugsUtilty.Large, out var value) ? value : null;

        public override async Task<IStreamingObjectCollection<IStreamingTrack>> LoadSongsAsync()
        {
            if (this.Songs == null)
            {
                this.Songs = new BugsTracksForPlaylist(this);
            }
            await this.Songs.LoadNextAsync().ConfigureAwait(false);

            return this.Songs;
        }

    }

    class BugsPlaylistForESAlbum : BugsPlaylist
    {
        public BugsPlaylistForESAlbum(JToken token) :base()
        {
            this.StreamingID = token["es_album_id"].ToObject<String>();
            this.Name = token["title"].ToObject<String>();

            this.SongCount = token["track_cnt"].ToObject<int>();

            this.Description = token["descr"].ToObject<String>() ?? String.Empty;

            this.creatorID = "";// token["msrl"].ToObject<String>();

            this.artistImages = BugsUtilty.GetImageUrls(token["img_urls"]);


        }
        public override async Task<IStreamingObjectCollection<IStreamingTrack>> LoadSongsAsync()
        {
            if (this.Songs == null)
            {
                this.Songs = new BugsTracksForPlaylist(this);
            }
            await this.Songs.LoadNextAsync().ConfigureAwait(false);

            return this.Songs;
        }

    }
    class BugsMyPlaylist : BugsPlaylist
    {
        public BugsMyPlaylist(JToken token) : base()
        {
            this.StreamingID = token["playlist_id"].ToObject<String>();
            this.Name = token["title"].ToObject<String>();

            this.SongCount = token["track_count"].ToObject<int>();

            this.Description = token["descr"].ToObject<String>() ?? String.Empty;

            this.creatorID = "";// token["msrl"].ToObject<String>();

            this.artistImages = BugsUtilty.GetImageUrls(token["img_urls"]);


        }

        public override async Task<IStreamingObjectCollection<IStreamingTrack>> LoadSongsAsync()
        {
            if (this.Songs == null)
            {
                this.Songs = new BugsTracksForPlaylist(this);
            }
            await this.Songs.LoadNextAsync().ConfigureAwait(false);

            return this.Songs;
        }
    }

}
