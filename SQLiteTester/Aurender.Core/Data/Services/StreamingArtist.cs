using System.Diagnostics;
using System.Threading.Tasks;
using Aurender.Core.Contents.Streaming;

namespace Aurender.Core.Data.Services
{

    [DebuggerDisplay("{ServiceType} Artist {StreamingID} : [{ArtistName}]")]
    internal abstract class StreamingArtist : IStreamingArtist
    {
        public override bool Equals(object obj)
        {
            if (obj is StreamingArtist b)
            {
                return (b.ServiceType == this.ServiceType) && (b.StreamingID.Equals(this.StreamingID));
            }

            return false;
        }
        public override int GetHashCode()
        {
            return $"{ServiceType.GetName()}:{this.StreamingID}".GetHashCode();
        }
        public override string ToString()
        {
            return $"{ServiceType} Artist { StreamingID} : [{ArtistName}]";
        }

        protected StreamingArtist(ContentType cType)
        {
            this.ServiceType = cType;
        }

        public abstract string ImageUrl { get; }
        public abstract string ImageUrlForMediumSize { get; }
        public abstract string ImageUrlForLargeSize { get; }

        public string StreamingID { get; protected set; }

        public StreamingObjectDescription ArtistBiography { get; protected set; }

        public ContentType ServiceType { get; private set; }

        public string ArtistName { get; protected set; }

        public StreamingItemType StreamingItemType => StreamingItemType.Aritst;
        public bool IsFavorite => ServiceManager.Service(ServiceType).IsFavorite(this);

        public object ArtistImage
        {
            get
            {
                return this.GetImage(ImageSize.Medium);
            }
        }

        public int CountOfAlbums { get; protected set; }
        public int CountOfSongs { get; protected set; }

        public abstract Task<StreamingObjectDescription> LoadBiography();
        public abstract Task<IStreamingObjectCollection<IStreamingArtist>> LoadRelatedArtists();
        public abstract Task<IStreamingObjectCollection<IStreamingArtist>> LoadSimilarArtists();
        public abstract Task<IStreamingObjectCollection<IStreamingTrack>> LoadTopTracks();
        public abstract Task<IStreamingObjectCollection<IStreamingAlbum>> LoadAlbums();
        public abstract Task<IStreamingObjectCollection<IStreamingAlbum>> LoadAllAlbums();

        public void SetFavorite(bool favorite)
        {
            var s = ServiceManager.Service(ServiceType);

            Debug.Assert(s != null, "Service must implement IStreamingServiceFavorite");

            Task.Run(async () => await s.UpdateFavorite(this, favorite).ConfigureAwait(false));
        }
    }

}