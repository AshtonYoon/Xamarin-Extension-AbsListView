using System;
using System.Threading.Tasks;

namespace Aurender.Core.Contents.Streaming
{
    public interface IStreamingTrack : IStreamingServiceObject, IStreamingServiceObjectWithImage, IPlayableItem, ISong, IStreamingFavoritable
    {
        new ContentType ServiceType { get; }

        String StreamingArtistID {get;}
        String StreamingAlbumID {get;}


        bool AllowStreaming {get;}

        bool IsPremiumOnly { get; }

        Task<String> LoadLyrics();

        Task AddToCache();

        object GetExtraImage();
    }

    public interface IRadioStation : IStreamingTrack
    {

    }
}