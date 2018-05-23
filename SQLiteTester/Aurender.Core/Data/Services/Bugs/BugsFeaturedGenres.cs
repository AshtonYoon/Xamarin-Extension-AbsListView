using Aurender.Core.Contents.Streaming;

namespace Aurender.Core.Data.Services.Bugs
{

    class BugsFeaturedGenres : BugsCollectionBase<IStreamingAlbum>
    {
        public BugsFeaturedGenres(string title, string path) : base(50, typeof(BugsAlbum), title)
        {
            this.urlForData = ServiceManager.Bugs().URLFor($"albums/new/{path}");
        }
    }

}