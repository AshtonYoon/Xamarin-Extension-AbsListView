using System;
using System.Linq;
using System.Collections.Generic;
using Aurender.Core.Contents.Streaming;
using Aurender.Core.Utility;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aurender.Core.Data.Services.TIDAL
{

    internal class TIDALGenre : StreamingGenre
    {
        protected readonly string albumCover;

        public TIDALGenre(JToken token) : base(ContentType.TIDAL)
        {
            this.Name = token["name"].ToObject<String>();
            this.StreamingID = token["path"].ToObject<String>();

            var imageID = token["image"].ToObject<String>();

            string sizeInfo = "{0}x{1}.jpg";
            this.albumCover = $"https://resources.tidal.com/images/{imageID.Replace('-', '/')}/{sizeInfo}";
            this.supportsAlbums = true;
        }
        public override string ImageUrl => String.Format(albumCover, 220, 146);

        public override string ImageUrlForMediumSize => String.Format(albumCover, 480, 320);

        public override string ImageUrlForLargeSize => String.Format(albumCover, 1024, 256);
        public override async Task<IStreamingObjectCollection<IStreamingAlbum>> LoadAlbumsAsync()
        {
            if (this.Albums == null)
            {
                this.Albums = new TIDALAlbumsForGenre(this.StreamingID);
            }
            await this.Albums.LoadNextAsync().ConfigureAwait(false);

            return this.Albums;
        }

        public override async Task<IStreamingObjectCollection<IStreamingPlaylist>> LoadPlaylistsAsync()
        {
            if (this.Playlists == null)
            {
                this.Playlists = new TIDALPlaylistsForMood(this.StreamingID);
            }
            await this.Playlists.LoadNextAsync().ConfigureAwait(false);

            return this.Playlists;
        }
    }

}