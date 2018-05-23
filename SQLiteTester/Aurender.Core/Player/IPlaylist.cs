using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Aurender.Core;
using Aurender.Core.Contents;
using Aurender.Core.Contents.Streaming;

namespace Aurender.Core.Player
{

    public interface IPlaylist : IList<IPlayableItem>
    {
        /// <summary>
        /// To sync with playlist of Aurender using status.
        /// </summary>
        /// <value>The playlist version.</value>
        //Int64 PlaylistVersion { get; }

        String Name { get; }

        Int32 CurrentPosition { get; }
        Int32 TotalDuration { get; }

        Boolean IsQueue { get; }

        List<IPlayableItem> GetStreamingItems(IStreamingService service);

        Task AddAsync(String filePath, OptionForAddPosition position = OptionForAddPosition.UserDefault);
        Task AddAsync(IPlayableItem item, OptionForAddPosition position = OptionForAddPosition.UserDefault);
        Task AddRangeAsync(IAlbum album, OptionForAddPosition position = OptionForAddPosition.UserDefault);
        Task AddRangeAsync(IEnumerable<IPlayableItem> items, OptionForAddPosition position = OptionForAddPosition.UserDefault);

        void Move(int oldIndex, int newIndex);

        void ShuffleItems();

        Task<bool> Load(string nameOfPlaylist);

        /// <summary>
        /// Save current list as name, if there is a same named playlit, it will overwrite
        /// </summary>
        void Save(string name);
        
        event EventHandler<Int64> OnPlaylistItemUpdated;
        event EventHandler<Int32> PlaylistFulled;

        void ReloadPlaylist();

        void ReplaceItem(IStreamingTrack track);
    }
}