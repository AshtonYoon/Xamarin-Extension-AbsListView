using System;
using System.Threading.Tasks;
using Aurender.Core.Contents.Streaming;

namespace Aurender.Core.Data.Services
{
    internal abstract class StreamingServiceImplementation 
    {
        internal abstract Task UpdateFavorite(IStreamingFavoritable track, bool toFavorite);

        internal abstract bool IsFavorite(IStreamingFavoritable track);


        internal abstract String URLFor(String partialPath, String paramString = "");
        internal abstract String URLForWithoutDefaultParam(String partialPath);

        internal abstract bool IsUserUpdatable(IStreamingPlaylist playlist);
//        internal abstract void AddToCache(IStreamingTrack track);
    }
}