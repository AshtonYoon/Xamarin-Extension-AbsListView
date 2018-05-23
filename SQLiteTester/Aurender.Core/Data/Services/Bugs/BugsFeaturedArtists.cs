using System;
using Aurender.Core.Contents.Streaming;

namespace Aurender.Core.Data.Services.Bugs
{

    class BugsFeaturedArtists : BugsCollectionBase<IStreamingArtist>
    {
        protected BugsFeaturedArtists(string title, int count, Type t) : base(count, typeof(BugsArtist), title)
        {

        }

        public BugsFeaturedArtists(string title, String url) : base(50, typeof(BugsArtist), title)
        {
            this.urlForData = ServiceManager.Bugs().URLFor(url);
        }
    }

    class BugsFavoriteArtists : BugsFeaturedArtists
    {
        internal BugsFavoriteArtists(String title) : base(title, SSBugs.FavoriteBucketSize, typeof(BugsArtist))
        {
            this.urlForData = ServiceManager.Service(ServiceType).URLFor($"me/likes/artist");
        }
    }
}