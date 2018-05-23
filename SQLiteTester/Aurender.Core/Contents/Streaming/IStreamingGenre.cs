using System.Threading.Tasks;

namespace Aurender.Core.Contents.Streaming
{

    public interface IStreamingGenre : IStreamingServiceObject , IStreamingServiceObjectWithImage, IGenre
    {
        bool SupportsAlbums();
        IStreamingObjectCollection<IStreamingAlbum> Albums { get; }
        Task<IStreamingObjectCollection<IStreamingAlbum>> LoadAlbumsAsync();


        bool SupportsPlaylists();
        IStreamingObjectCollection<IStreamingPlaylist> Playlists { get; }
        Task<IStreamingObjectCollection<IStreamingPlaylist>> LoadPlaylistsAsync();

        bool SupportsTracks();
        IStreamingObjectCollection<IStreamingTrack> Tracks { get; }
        Task<IStreamingObjectCollection<IStreamingTrack>> LoadTracksAsync();

        bool SupportsSubGenres();
        IStreamingObjectCollection<IStreamingGenre> SubGenres { get; }
        Task<IStreamingObjectCollection<IStreamingGenre>> LoadSubGenresAsync();

   }

}