using Aurender.Core.Contents.Streaming;
using System.Threading.Tasks;

namespace Aurender.Core.Data.Services.Bugs
{
    class BugsGenre : StreamingGenre
    {

        public BugsGenre(string name, string path) : base(ContentType.Bugs)
        {
            this.Name = name;
            this.StreamingID = path;
            this.supportsPlaylists = true;
        }

        public override string ImageUrl => string.Empty; 

        public override string ImageUrlForMediumSize => string.Empty; 

        public override string ImageUrlForLargeSize => string.Empty;
        public override async Task<IStreamingObjectCollection<IStreamingAlbum>> LoadAlbumsAsync()
        {
            if (this.Albums == null)
            {
                // this.Albums = new TIDALAlbumsForGenre(this.StreamingID);
            }
            await this.Albums.LoadNextAsync().ConfigureAwait(false);

            return this.Albums;
        }

        public override async Task<IStreamingObjectCollection<IStreamingPlaylist>> LoadPlaylistsAsync()
        {
            if (this.Playlists == null)
            {
                //  this.Playlists = new TIDALPlaylistsForMood(this.StreamingID);
            }
            await this.Playlists.LoadNextAsync().ConfigureAwait(false);

            return this.Playlists;
        }
    }

    
}
