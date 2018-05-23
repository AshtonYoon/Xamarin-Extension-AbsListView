using System;
using System.Threading.Tasks;
using Aurender.Core.Contents.Streaming;

namespace Aurender.Core.Data.Services.Melon
{

    class MelonGenre : StreamingGenre
    {
        public MelonGenre(string name, String code) : base(ContentType.Melon)
        {
            this.supportsTracks = true;

            this.Name = name;
            this.StreamingID = code;
        }
        protected IStreamingObjectCollection<IStreamingTrack> tracks;
        public override IStreamingObjectCollection<IStreamingTrack> Tracks { get { return null; } }

        public override string ImageUrl => string.Empty;

        public override string ImageUrlForMediumSize => string.Empty;

        public override string ImageUrlForLargeSize => string.Empty;

        public override async Task<IStreamingObjectCollection<IStreamingTrack>> LoadTracksAsync()
        {
            this.tracks = new MelonTracksByGenre(this.Name, this.StreamingID);

            await tracks.LoadNextAsync();

            return tracks;
        }
    }

}