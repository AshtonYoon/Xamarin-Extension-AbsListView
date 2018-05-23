using System;
using System.Threading.Tasks;
using Aurender.Core.Contents.Streaming;
using Newtonsoft.Json.Linq;

namespace Aurender.Core.Data.Services.Melon
{

    class MelonPlaylist : StreamingPlaylist
    {
        private string imageUrl;

        public MelonPlaylist(JToken token) : base (ContentType.Melon)
        {
            this.StreamingID = token["PLYLSTSEQ"].ToObject<String>();
            this.SongCount = token["SONGCNT"].ToObject<int>();
            this.imageUrl = token["THUMBIMG"].ToObject<String>();
            this.Name = token["PLYLSTTITLE"].ToObject<String>();
        }

        public override string ImageUrl => imageUrl;

        public override string ImageUrlForMediumSize => imageUrl;

        public override string ImageUrlForLargeSize => imageUrl;
        public override async Task<IStreamingObjectCollection<IStreamingTrack>> LoadSongsAsync()
        {
            this.Songs = new MelonTracksForPlaylist(this);

            await this.Songs.LoadNextAsync().ConfigureAwait(false);
            return this.Songs;
        }
    }

}