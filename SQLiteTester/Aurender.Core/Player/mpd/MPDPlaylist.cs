using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Aurender.Core.Contents;
using Aurender.Core.Contents.Streaming;
using Aurender.Core.Data.Services;
using Aurender.Core.Utility;

namespace Aurender.Core.Player.mpd
{
    abstract class MPDPlaylistBase : IPlaylist, INotifyCollectionChanged, IARLog
    {
        #region IARLog
        private bool LogAll = true;
        bool IARLog.IsARLogEnabled { get { return LogAll; } set { LogAll = value; } }
        #endregion
        protected Func<String, IPlayableItem> GetPlayableItem;

        internal protected MPDPlaylistBase(bool isQueue, Func<string, IPlayableItem> playableItemFactory, Func<string, string> urlGetter)
        {
            this.GetPlayableItem = playableItemFactory;
            this.isQueue = isQueue;
            this.urlGetter = urlGetter;

            ServiceManager.it.OnStreamingTrackLoaded += ServiceManager_OnStreamingTrackLoaded;
        }

        private IList<IPlayableItem> items;
        protected string name = "";
        protected readonly bool isQueue;
        protected DateTime timeToStartSync = DateTime.Now;
        protected long playlistVersion = 0;
        protected Func<string, string> urlGetter;
        protected Dictionary<int, string> unloadedStreamingTracks = new Dictionary<int, string>();

        public IPlayableItem this[int index]
        {
            get => this.items[index];
            set => items[index] = value;
        }

        string IPlaylist.Name => name;

        public int CurrentPosition { get; internal set; } = -1;

        int IPlaylist.TotalDuration// => this/*items*/.Sum((arg) => arg.Duration);
        {
            get
            {
                var cloned = new List<IPlayableItem>(this);


                return cloned.Sum((arg) => arg.Duration);
            }
        }
        bool IPlaylist.IsQueue => isQueue;

        public int Count => this.items.Count;

        bool ICollection<IPlayableItem>.IsReadOnly => false;

        protected IList<IPlayableItem> Items
        {
            get => items;
            set
            {
                var isInitializing = (items == null);
                items = value;

                if (!isInitializing)
                    CallOnPlaylistItemUpdated(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            }
        }

        int GetIndexFor(OptionForAddPosition position)
        {
            OptionForAddPosition newPosition;
            if (position == OptionForAddPosition.UserDefault)
                newPosition = Setting.UserSetting.Setting.App.Get(Setting.FieldsForAppConfig.DefaultActionForSelect, OptionForAddPosition.End);
            else
                newPosition = position;

            int nexIndex = 0;

            if (newPosition == OptionForAddPosition.End)
                nexIndex = this.Count();
            else
                nexIndex = CurrentPosition + 1;

            return nexIndex;
        }


        Task IPlaylist.AddAsync(IPlayableItem item, OptionForAddPosition position)
        {
            var allItems = new List<IPlayableItem> { item };

            return ((IPlaylist)this).AddRangeAsync(allItems, position);
        }

        Task IPlaylist.AddRangeAsync(IAlbum album, OptionForAddPosition position)
        {
            album.LoadSongs();
            var allItems = album.Songs;
            return ((IPlaylist)this).AddRangeAsync(allItems, position);
        }


        Task IPlaylist.AddRangeAsync(IEnumerable<IPlayableItem> sourceItems, OptionForAddPosition position)
        {
            if (items.Count() >= 2000)
                PlaylistFulled?.Invoke(this, 0);

            if (sourceItems.Count() <= 0)
                return Task.CompletedTask;

            int startedIndex = GetIndexFor(position);

            int index = startedIndex;

            int addedCount = 0;
            lock (sourceItems)
            {
                foreach (IPlayableItem item in sourceItems)
                {
                    if (items.Count() >= 2000)
                    {
                        PlaylistFulled?.Invoke(this, addedCount);
                        break;
                    }
                    else
                    {
                        this.items.Insert(index, item);
                        RegisterUnloadedStreamingTrack(item, index);

                        index++;
                        addedCount++;
                    }
                }
            }

            var changedItems = sourceItems is List<object> ? (List<object>)sourceItems : new List<object>(sourceItems);

            // I don't know the reason,
            // Telerik listview reverses and shows the added tracks.
            // So, It have to reverse the added tracks before notifying the collection changed.
            changedItems.Reverse();

            CallOnPlaylistItemUpdated(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, changedItems, startedIndex));

            return Task.CompletedTask;
        }

        protected void RegisterUnloadedStreamingTrack(IPlayableItem item, int index)
        {
            if (item is PlayableFile && item.ServiceType != ContentType.Local)
            {
                unloadedStreamingTracks[index] = item.ItemPath;
            }
        }

        void ICollection<IPlayableItem>.Add(IPlayableItem item)
        {
            var allItems = new List<IPlayableItem>();

            ((IPlaylist)this).AddRangeAsync(allItems, OptionForAddPosition.UserDefault).Wait();
        }

        void ICollection<IPlayableItem>.Clear()
        {
            this.items.Clear();
            CallOnPlaylistItemUpdated(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        bool ICollection<IPlayableItem>.Contains(IPlayableItem item)
        {
            return this.items.Contains(item);
        }

        void ICollection<IPlayableItem>.CopyTo(IPlayableItem[] array, int arrayIndex)
        {
            this.items.CopyTo(array, arrayIndex);
        }

        IEnumerator<IPlayableItem> IEnumerable<IPlayableItem>.GetEnumerator()
        {
            return items.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return items.GetEnumerator();
        }

        List<IPlayableItem> IPlaylist.GetStreamingItems(IStreamingService service)
        {
            var itemsForService = this./*items.*/Where((IPlayableItem item) => (item.ServiceType == service.ServiceType));

            var list = new List<IPlayableItem>(itemsForService);

            return list;
        }

        public int IndexOf(IPlayableItem item)
        {
            return this.items.IndexOf(item);
        }

        public void Insert(int index, IPlayableItem item)
        {
            this.items.Insert(index, item);
            CallOnPlaylistItemUpdated(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, index));
        }

        Task<bool> IPlaylist.Load(string nameOfPlaylist)
        {
            if (isQueue)
            {
                throw new NotSupportedException();
            }
            else
            {
                return LoadItemsFromAurender(nameOfPlaylist);
            }
        }

        public bool Remove(IPlayableItem item)
        {
            int index = items.IndexOf(item);
            if (index < 0) return false;

            RemoveAt(index);
            return true;
        }

        public void RemoveAt(int index)
        {
            var removedItem = items[index];
            this.items.RemoveAt(index);
            CallOnPlaylistItemUpdated(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, removedItem, index));
        }

        public void ReplaceItem(IStreamingTrack track)
        {
            int index = 0;
            foreach (IPlayableItem item in this)
            {
                if (item.ItemPath == track.ItemPath)
                {
                    this.LP("Queue", $"Replace loaded track. Index: {index}, Track: {track.Title}");
                    this[index] = track;
                    CallOnPlaylistItemUpdated(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, track, item, index));
                    return;
                }
                index++;
            }

            this.EP("Queue", $"Fail to replace loaded track. Track: {track.Title}");
        }

        public void Move(int oldIndex, int newIndex)
        {
            var item = this[oldIndex];

            RemoveAt(oldIndex);
            Insert(newIndex, item);

            CallOnPlaylistItemUpdated(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Move, item, newIndex, oldIndex));
        }

        public abstract void Save(string name);

        void IPlaylist.ShuffleItems()
        {
            CallOnPlaylistItemUpdated(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            throw new NotSupportedException();
        }

        public event EventHandler<int> PlaylistFulled;

        public event EventHandler<long> OnPlaylistItemUpdated;
        public event NotifyCollectionChangedEventHandler CollectionChanged;

        protected abstract Task<bool> LoadItemsFromAurender(string nameOfPlaylist);
        protected void CallOnPlaylistItemUpdated(NotifyCollectionChangedEventArgs e)
        {
            /// Controller will call beginInvoke
            /// Player will call Task.Run, so we just call invoke.
            try
            {
                OnPlaylistItemUpdated?.Invoke(this, this.playlistVersion);
            }
            catch (Exception ex)
            {
                IARLogStatic.Error("MPD Playlist", "Fail when calling OnPlaylistItemUpdated", ex);
            }

            PlatformUtility.BeginInvokeOnMainThread(() =>
            {
                try
                {
                    CollectionChanged?.Invoke(this, e);
                }
                catch (Exception ex)
                {
                    IARLogStatic.Error("MPD Playlist", "Fail when calling CollectionChanged", ex);
                }
            });
        }

        protected void SetWaitForSync(Int32 sec)
        {
            this.timeToStartSync = DateTime.Now.AddSeconds(sec);
        }

        internal abstract Task SyncPlaylistAsync(IAurenderEndPoint endPoint, Int64 targetVersion, bool force = false);


        [Conditional("DEBUG")]
        internal void PrintItems()
        {
            this.LP("Queue", $"----------------{this.GetType()}----------------");
            int i = 0;
            foreach (IPlayableItem item in Items)
            {
                i++;
                this.LP("Queue", $"  {i:02} : {item.ItemPath}     ===> [{item.ServiceType}]");
            }
            this.LP("Queue", "==========================================================");
        }

        public Task AddAsync(string filePath, OptionForAddPosition position = OptionForAddPosition.UserDefault)
        {
            throw new NotImplementedException();
        }

        public void ReloadPlaylist()
        {
            this.playlistVersion = 0;
        }

        private void ServiceManager_OnStreamingTrackLoaded(IStreamingService sender, IStreamingTrack track)
        {
            string path = track.ItemPath;

            var indexs = from item in unloadedStreamingTracks
                         where item.Value == path
                         select item.Key;

            foreach (int index in indexs)
            {
                if (index >= Count)
                    return;

                var oldTrack = this[index];

                if (oldTrack is IStreamingTrack)
                    continue;

                var newTrack = new PlayingStreamingTrack(track);

                // copy IsPlaying property for highlight in the queue
                if (oldTrack is IPlayingTrack playable && playable.IsPlaying)
                {
                    newTrack.IsPlaying = true;
                }

                this[index] = newTrack;
                CallOnPlaylistItemUpdated(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, newTrack, oldTrack, index));
            }
        }
    }

    class MPDQueue : MPDPlaylistBase
    {
        long PlaylistVersion => playlistVersion;

        internal MPDQueue(Func<string, IPlayableItem> playableItemFactory, Func<string, string> urlGetter) : base(true, playableItemFactory, urlGetter)
        {
            this.Items = new List<IPlayableItem>(100);
            this.playlistVersion = -1;
            ((IARLog)this).IsARLogEnabled = false;
        }

        //internal MPDQueue(bool log) : this()
        //{
        //    ((IARLog)this).IsARLogEnabled = log;
        //}

        internal override async Task SyncPlaylistAsync(IAurenderEndPoint endPoint, Int64 targetVersion, bool force = false)
        {
            if (timeToStartSync.CompareTo(DateTime.Now) >= 0)
            {
                return;
            }


            //if (this.playlistVersion != targetVersion)
            {
                MPDResponse response;

                using (var con = new MPDConnection())
                {
                    bool connected = await con.InitAsync(endPoint).ConfigureAwait(false);

                    response = await con.SendCommandAsync("playlist").ConfigureAwait(false);
                }

                if (response != null)
                {
                    bool synced = false;
                    try
                    {
                        synced = SyncToCurrent(response, force);

                        this.playlistVersion = targetVersion;
                    }
                    catch (Exception ex)
                    {
                        this.EP("Playlist", "Failed to sync playlist", ex);
                    }
                    finally
                    {
                        if (this.playlistVersion == targetVersion && synced)
                            CallOnPlaylistItemUpdated(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
                    }
                }
            }
            //else 
            //{
            //  this.L($"The Queue is already in version {targetVersion}");
            //}
        }

        protected override async Task<bool> LoadItemsFromAurender(string nameOfPlaylist)
        {
            SetWaitForSync(1);
            await Task.Delay(1);
            return true;
        }

        public void ClearExceptCurrentlyPlaying(IPlayableItem item)
        {
            Items = new List<IPlayableItem> { item };
        }

        void SuffleItems()
        {
            SetWaitForSync(1);
        }

        internal List<string> ResponseLines { get; set; } = new List<string>();
        private bool SyncToCurrent(MPDResponse response, bool force)
        {
            if (response.IsOk)
            {
                var lines = response.ResponseLines;
                if (!force && lines.SequenceEqual(ResponseLines))
                    return false;
                ResponseLines = lines;

                IList<IPlayableItem> newItems = new List<IPlayableItem>(lines.Count);
                unloadedStreamingTracks.Clear();

                foreach (var (line, index) in lines.Select((x, index) => (x, index)))
                {
                    // 1:file: tidal://73364270
                    var match = regx.Match(line);

                    if (match.Success)
                    {
                        String path = match.Groups[1].Value;

                        IPlayableItem file = GetPlayableItem(path);
                        RegisterUnloadedStreamingTrack(file, index);

                        newItems.Add(file);
                    }
                }

                Items = newItems;
                return true;
            }
            else
            {
                this.E("Failed to get result for command : playlist");
                this.E($"{response.ErrorMessage}");

                return false;
            }
        }

        public override async void Save(string name)
        {
            var url = this.urlGetter($"php/saveQueue?pl={name.URLEncodedString()}");

            using (var result = await WebUtil.GetResponseAsync(url).ConfigureAwait(false))
            {

            }
        }

        static readonly Regex regx = new Regex("\\d*:file: (.*)$");
    }

    class MPDPlaylistEditor : MPDPlaylistBase
    {
        internal MPDPlaylistEditor(Func<string, IPlayableItem> playableItemFactory, Func<string, string> urlGetter) : base(false, playableItemFactory, urlGetter)
        {
            this.Items = new List<IPlayableItem>(100);
        }

        public override void Save(string name)
        {
            //    NSString *urlString = [NSString stringWithFormat:@"http://%@:%ld/php/upload",
            //                       [[[[self playlistDelegate] deviceStatus] statusDelegate] ipAddress],
            //                       NSIntToLong([[[[self playlistDelegate] deviceStatus] statusDelegate] webPort])];

            //NSMutableString* cmd = [[NSMutableString alloc] initWithCapacity:[WLDeviceManager maxPlaylistLength]];
            //for (WLMPDFile* file in self.localItems) {
            //    [cmd appendFormat:@"%@%@", [file fileName], kNewLineString];
            //}

            //result = [cmd uploadTo:urlString
            //                asFile:[NSString stringWithFormat:@"%@.m3u", name]
            //             fieldName:@"pl"];
            throw new NotImplementedException();
        }

        protected override Task<bool> LoadItemsFromAurender(string nameOfPlaylist)
        {
            throw new NotImplementedException();
        }

        internal override async Task SyncPlaylistAsync(IAurenderEndPoint endPoint, long targetVersion, bool force = false)
        {
            await Task.Delay(100);
        }


        void ShuffleItems()
        {
            this.Items.Shuffle();
        }
    }
}
