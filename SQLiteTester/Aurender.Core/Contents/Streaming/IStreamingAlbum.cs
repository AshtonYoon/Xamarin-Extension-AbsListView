using System;
using System.Threading.Tasks;

namespace Aurender.Core.Contents.Streaming
{
    public enum DescriptionField
    {
        Source,
        UpdatedDate,
        Content,
        ContentType,
        Summary,
        Review,
    }

    public interface IStreamingAlbum : IStreamingServiceObject , IStreamingServiceObjectWithImage, IAlbum, IStreamingFavoritable
    {
        String StreamingArtistID {get;}
        String AlbumType {get;}
        String Copyright {get;}
        String Genre {get;}

        object IconForAdditionalInfo();

        StreamingObjectDescription Description { get; }

        Task<StreamingObjectDescription> LoadDescriptionAsync();

        bool AllowStreaming {get;}

        IStreamingObjectCollection<IStreamingTrack> Tracks {get;}

        Task<IStreamingObjectCollection<IStreamingTrack>> LoadSongsAsync();

        object GetExtraImage();
    }

}