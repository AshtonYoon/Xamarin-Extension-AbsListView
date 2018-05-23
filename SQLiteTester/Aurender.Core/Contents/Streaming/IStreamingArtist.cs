using System.Threading.Tasks;

namespace Aurender.Core.Contents.Streaming
{

    public interface IStreamingArtist : IStreamingServiceObject , IStreamingServiceObjectWithImage, IArtist, IStreamingFavoritable
    {

        StreamingObjectDescription ArtistBiography {get;}

        Task<StreamingObjectDescription> LoadBiography();
        Task<IStreamingObjectCollection<IStreamingTrack>> LoadTopTracks();
        Task<IStreamingObjectCollection<IStreamingArtist>> LoadSimilarArtists();
        Task<IStreamingObjectCollection<IStreamingArtist>> LoadRelatedArtists();
        Task<IStreamingObjectCollection<IStreamingAlbum>> LoadAlbums();
        Task<IStreamingObjectCollection<IStreamingAlbum>> LoadAllAlbums();
    }

}