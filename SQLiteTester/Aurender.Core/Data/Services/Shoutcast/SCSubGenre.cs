using System;
using System.Threading.Tasks;
using Aurender.Core.Contents.Streaming;

namespace Aurender.Core.Data.Services.Shoutcast
{

    class SCSubGenre : StreamingGenre
    {

        internal SCSubGenre(String name, String id, int count) : base(ContentType.InternetRadio)
        {
            this.Name = name;
            this.StreamingID = id;
            this.supportsSubGenres = false;
            this.supportsTracks = true;
            this.tracks = new SCStationCollectionForSubGenre(name, count);
        }

        public override string ImageUrl => throw new NotImplementedException();

        public override string ImageUrlForMediumSize => throw new NotImplementedException();

        public override string ImageUrlForLargeSize => throw new NotImplementedException();


        private IStreamingObjectCollection<IStreamingTrack> tracks;
        public override IStreamingObjectCollection<IStreamingTrack> Tracks => tracks;
        public override async Task<IStreamingObjectCollection<IStreamingTrack>> LoadTracksAsync()
        {
            await tracks.LoadNextAsync().ConfigureAwait(false);

            return tracks;
        }
    }

}