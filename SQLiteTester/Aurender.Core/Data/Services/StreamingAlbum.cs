using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Aurender.Core.Contents.Streaming;

namespace Aurender.Core.Data.Services
{

    [DebuggerDisplay("{ServiceType} Album {StreamingID} : [{AlbumArtistName}] - ({ReleaseYear}) [{AlbumTitle}]")]
    internal abstract class StreamingAlbum : IStreamingAlbum
    {
        public override bool Equals(object obj)
        {
            StreamingAlbum b = obj as StreamingAlbum;
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
            return $"{ServiceType} Album { StreamingID} : [{AlbumType}][{AlbumArtistName}] - ({ReleaseYear}) [{AlbumTitle}]";
        }



        protected StreamingAlbum(ContentType cType)
        {
            ServiceType = cType;
        }

        public string StreamingID { get; protected set; }
        public string StreamingArtistID { get; protected set; }


        public string AlbumType { get; protected set; }

        public string Copyright { get; protected set; }

        public string Genre { get; protected set; }

        public bool AllowStreaming { get; protected set; }

        public IStreamingObjectCollection<IStreamingTrack> Tracks { get; protected set; }

        public ContentType ServiceType { get; private set; }

        public abstract string ImageUrl { get; }
        public abstract string ImageUrlForMediumSize { get; }

        public abstract string ImageUrlForLargeSize { get; }

        public string AlbumTitle { get; protected set; }

        public string AlbumArtistName { get; protected set; }

        public IArtist AlbumArtist
        {
            get
            {
                IARLogStatic.Error($"{ServiceType} Album", $"Doesn't support to get album artist");
                return null;
            }
        }


        public short ReleaseYear { get; protected set; }

        public string Publisher { get; protected set; }

        public short NumberOfDisc { get; protected set; }

        public short TotalSongs { get; protected set; }

        public IList<ISong> Songs
        {
            get
            {
                List<ISong> songs = new List<ISong>(Tracks != null ? Tracks.Count : 0);

                if (songs.Capacity > 0)
                {
                    songs.AddRange(Tracks);
                }

                return songs;
            }
        }

        public StreamingItemType StreamingItemType => StreamingItemType.Album;

        public bool IsFavorite => ServiceManager.Service(ServiceType).IsFavorite(this);

        public abstract Credits AlbumCredit();

        public StreamingObjectDescription Description { get; protected set; }

        protected int duration;
        public int Duration()
        {
            if (Tracks != null)
            {
                int total = Tracks.Sum<IStreamingTrack>(t => t.Duration);

                return total;
            }

            return duration;
        }

        public object FrontCover
        {
            get
            {
                return this.GetImage(ImageSize.Medium);
            }
        }

        public object BackCover => null;

        public abstract object IconForAdditionalInfo();

        public abstract Task<StreamingObjectDescription> LoadDescriptionAsync();

        public async void LoadSongs()
        {
            await LoadSongsAsync().ConfigureAwait(false);
        }

        public abstract Task<IStreamingObjectCollection<IStreamingTrack>> LoadSongsAsync();

        public void SetFavorite(bool favorite)
        {
            var s = ServiceManager.Service(ServiceType);

            Debug.Assert(s != null, "Service must implement IStreamingServiceFavorite");
            Task.Run(async () => await s.UpdateFavorite(this, favorite).ConfigureAwait(false));
        }

        public abstract object GetExtraImage();
    }
}