using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Aurender.Core.Contents.Streaming;

namespace Aurender.Core.Data.Services
{

    [DebuggerDisplay("{ServiceType} Playlist {StreamingID} : [{Name}] - [{SongCount}]")]
    internal abstract class StreamingPlaylist : IStreamingPlaylist
    {
        public override bool Equals(object obj)
        {
            StreamingPlaylist b = obj as StreamingPlaylist;
            if (b != null)
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
            return $"{ServiceType} Playlist { StreamingID} : [{Name}] - [{SongCount:n0}]";
        }


        protected StreamingPlaylist(ContentType cType)
        {
            this.ServiceType = cType;
        }


        public abstract string ImageUrl { get; }
        public abstract string ImageUrlForMediumSize { get; }

        public abstract string ImageUrlForLargeSize { get; }



        public string StreamingID { get; protected set; }

        public string Name { get; protected set; }
        public string Description { get; protected set; }
        public int SongCount { get; protected set; }

        public IStreamingObjectCollection<IStreamingTrack> Songs { get; protected set; }

        public ContentType ServiceType { get; private set; }

        public StreamingItemType StreamingItemType => StreamingItemType.Playlist;

        public bool IsFavorite => ServiceManager.Service(ServiceType).IsFavorite(this);

        public void SetFavorite(bool favorite)
        {
            var s = ServiceManager.Service(ServiceType);

            Debug.Assert(s != null, "Service must implement IStreamingServiceFavorite");
            Task.Run(async () => await s.UpdateFavorite(this, favorite).ConfigureAwait(false));
        }

        public abstract Task<IStreamingObjectCollection<IStreamingTrack>> LoadSongsAsync();


        public Task<bool> SaveChanges(String title, IList<IStreamingTrack> newTracks, String description)
        {
            var service = ServiceManager.it[this.ServiceType];
            var serviceBase = service as StreamingServiceImplementation;

            if (serviceBase.IsUserUpdatable(this))
            {
                return service.UpdatePlaylistAsync(this, title, newTracks, description);
            }
            else
                return Task.FromResult(false);
        }

    }

}