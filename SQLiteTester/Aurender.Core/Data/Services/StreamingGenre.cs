using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Aurender.Core.Contents.Streaming;

namespace Aurender.Core.Data.Services
{
    [DebuggerDisplay("{ServiceType} Genre {StreamingID} : [{Name}]")]
    internal abstract class StreamingGenre : IStreamingGenre
    {
        public override bool Equals(object obj)
        {
            StreamingGenre b = obj as StreamingGenre;
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
            return $"{ServiceType} Genre { StreamingID} : [{Name}]";
        }

        protected bool supportsAlbums = false;
        protected bool supportsPlaylists = false;
        protected bool supportsTracks = false;
        protected bool supportsSubGenres = false;

        protected StreamingGenre(ContentType cType)
        {
            this.ServiceType = cType;
        }

        public abstract string ImageUrl { get; }
        public abstract string ImageUrlForMediumSize { get; }

        public abstract string ImageUrlForLargeSize { get; }

        public string StreamingID { get; protected set; }
        public string Name { get; protected set; }


        public bool SupportsAlbums() => supportsAlbums;
        public IStreamingObjectCollection<IStreamingAlbum> Albums { get; protected set; }

        public virtual Task<IStreamingObjectCollection<IStreamingAlbum>> LoadAlbumsAsync()
        {
            throw new NotImplementedException();
        }

        public bool SupportsPlaylists() => supportsPlaylists;
        public IStreamingObjectCollection<IStreamingPlaylist> Playlists { get; protected set; }

        public virtual Task<IStreamingObjectCollection<IStreamingPlaylist>> LoadPlaylistsAsync()
        {
            throw new NotImplementedException();
        }

        public bool SupportsTracks() => supportsSubGenres;
        public virtual IStreamingObjectCollection<IStreamingTrack> Tracks { get { return null; } }
        public virtual Task<IStreamingObjectCollection<IStreamingTrack>> LoadTracksAsync()
        {
            throw new NotImplementedException();
        }

        public bool SupportsSubGenres() => supportsSubGenres;
        public virtual IStreamingObjectCollection<IStreamingGenre> SubGenres { get { return null; } }
        public virtual Task<IStreamingObjectCollection<IStreamingGenre>> LoadSubGenresAsync()
        {
            throw new NotImplementedException();
        }

        public ContentType ServiceType { get; private set; }


        public object GenreImage
        {
            get
            {
                IARLogStatic.Error($"{ServiceType} Genre", $"Doesn't support to get genre image");
                return null;
            }
        }

        public virtual int CountOfAlbums => 0;

        public virtual int CountOfSongs => 0;
    }
}
