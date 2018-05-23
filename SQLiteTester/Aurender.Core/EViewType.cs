using System;

namespace Aurender.Core
{
    /// <summary>
    /// For Playlist, use Folders
    /// </summary>
    public enum EViewType : Int32
    {
        None,
        Songs,
        Artists,
        Albums,
        Genres,
        Composers,
        Conductors,
        /// <summary>
        /// For Playlist, use Folders
        /// </summary>
        Folders,
        Playlist = Folders
    }
}
