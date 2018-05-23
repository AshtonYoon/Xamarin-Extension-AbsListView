using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Aurender.Core.Contents.Streaming
{

    public interface IStreamingPlaylist : IStreamingServiceObject , IStreamingServiceObjectWithImage, IStreamingFavoritable
    {
        String Name {get;}
        int SongCount { get; }
        String Description { get; }
        Task<IStreamingObjectCollection<IStreamingTrack>> LoadSongsAsync();
        IStreamingObjectCollection<IStreamingTrack> Songs { get; }

        Task<bool> SaveChanges(String title, IList<IStreamingTrack> newTracks, String description);
    }

}