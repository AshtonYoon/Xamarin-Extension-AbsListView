using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Aurender.Core;
using Aurender.Core.Contents;

namespace Aurender.Core.Player
{
    public interface IPlayerController : IPlaylist
    {
        IAurenderStatus Status { get; }

        void Play();
        void Play(IPlayableItem item);
        void Play(Int32 index);
        void Pause();
        void Next();
        void Previous();
        void SeekCurrent(Int32 sec);

        Task ClearExceptCurrentlyPlayingAsync();

        void ToNextRepeatMode();
        void SetRepeat(OptionForRepeat newRepeat);
        void ToNextRandomMode();

        void SetRandom(bool random);
        void SetConsume(bool random);

        void DeleteCachedFile(string path);

        IPlaylist Queue { get; }
        IPlaylist Editor { get; }

        /// <summary>
        /// It will be queue or editor. It stores the status of active playlist UI
        /// </summary>
        /// <value>The current.</value>
        IPlaylist Current { get; }

        void ActivatePlaylist(bool isQueue);
        void ActivateQueue();
        void ActivateEditor();

        List<String> PlaylistNames { get; }
        Task<bool> LoadPlaylistNamesAsync();
        Task ReplaceQueueWithPlaylistAsync(string name);
        Task AddPlaylistToQueueAsync(string name);

        Task RemoveRangeAsync(IEnumerable<IPlayableItem> items);
        Task MoveAsync(int oldIndex, int newIndex);
        Task ClearAsync();
    }
}
