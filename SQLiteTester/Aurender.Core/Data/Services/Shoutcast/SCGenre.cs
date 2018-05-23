using System;
using System.Threading.Tasks;
using Aurender.Core.Contents.Streaming;
using Newtonsoft.Json.Linq;

namespace Aurender.Core.Data.Services.Shoutcast
{

    class SCGenre : StreamingGenre
    {
        internal SCGenre(JToken token) : base(ContentType.InternetRadio)
        {
            this.Name = token["name"].ToObject<String>();
            this.StreamingID = token["id"].ToObject<String>();
            this.supportsSubGenres = true;
            this.subGenres = new SCSubGenreCollection(this.Name, this.StreamingID);
        }

        public override string ImageUrl => throw new NotImplementedException();

        public override string ImageUrlForMediumSize => throw new NotImplementedException();

        public override string ImageUrlForLargeSize => throw new NotImplementedException();

        public override Task<IStreamingObjectCollection<IStreamingAlbum>> LoadAlbumsAsync()
        {
            throw new NotImplementedException();
        }

        public override Task<IStreamingObjectCollection<IStreamingPlaylist>> LoadPlaylistsAsync()
        {
            throw new NotImplementedException();
        }





        private IStreamingObjectCollection<IStreamingGenre> subGenres;
        public override IStreamingObjectCollection<IStreamingGenre> SubGenres => subGenres;
        public override async Task<IStreamingObjectCollection<IStreamingGenre>> LoadSubGenresAsync()
        {
            await this.subGenres.LoadNextAsync();

            return subGenres;
        }
    }

}