using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Aurender.Core.Contents;
using Aurender.Core.Contents.Streaming;
using Aurender.Core.Data.Services;
using Aurender.Core.Utility;

namespace Aurender.Core.Player.mpd
{
    class MPDController : IPlayerController, IARLog
    {
        #region IARLog
        private bool LogAll = false;
        bool IARLog.IsARLogEnabled { get { return LogAll; } set { LogAll = value; } }
        #endregion

        const string Tag = "MPDController";

        protected Func<String, IPlayableItem> GetPlayableItem;
        private IAurenderEndPoint end;

        internal MPDQueue queue { get; private set; }
        internal MPDPlaylistEditor editor { get; private set; }

        public MPDController(IAurenderEndPoint endPoint, IAurenderStatus status, Func<String, IPlayableItem> playableItemFactory)
        {
            end = endPoint;
            this.Status = status;
            GetPlayableItem = playableItemFactory;
            this.queue = new MPDQueue(playableItemFactory, end.WebURLFor);
            this.editor = new MPDPlaylistEditor(playableItemFactory, end.WebURLFor);

            this.queue.OnPlaylistItemUpdated += Queue_OnPlaylistItemUpdated;
            this.Editor.OnPlaylistItemUpdated += Editor_OnPlaylistItemUpdated;
            this.queue.PlaylistFulled += Queue_PlaylistFulled;
            this.IsQueue = true;

            MPDStatus mStatus = (MPDStatus)status;
            mStatus.PropertyChanged += MStatus_PropertyChanged;

            AurenderBrowser.GetCurrentAurender().OnDatabaseOpened += Aurender_OnDatabaseOpened;
        }

        private void Queue_PlaylistFulled(object sender, int e)
        {
            PlaylistFulled?.Invoke(sender, e);
        }

        private void Editor_OnPlaylistItemUpdated(object sender, long e)
        {
            if (!IsQueue)
            {
                Task.Run(() => this.OnPlaylistItemUpdated?.Invoke(this.editor, e)).ContinueWith(task =>
                {
                    task.Exception.Handle(ex =>
                    {
                        IARLogStatic.Error("Exception in Event", "For Aurender.Editor.OnPlaylistUpdated.", ex);
                        return true;
                    });
                }, TaskContinuationOptions.OnlyOnFaulted);
            }
        }

        private void Queue_OnPlaylistItemUpdated(object sender, long e)
        {
            if (IsQueue)
            {
                Task.Run(() =>
                {
                    var invoker = this.OnPlaylistItemUpdated;
                    invoker?.Invoke(this.queue, e);
                }).ContinueWith(task =>
                {
                    task.Exception.Handle(ex =>
                    {
                        IARLogStatic.Error("Exception in Event", "For Aurender.Queue.OnPlaylistItemUpdated.", ex);
                        return true;
                    });
                }, TaskContinuationOptions.OnlyOnFaulted);
            }
        }

        ~MPDController()
        {
            this.queue.OnPlaylistItemUpdated -= OnPlaylistItemUpdated;
            this.queue.PlaylistFulled -= Queue_PlaylistFulled;
            this.editor.OnPlaylistItemUpdated -= OnPlaylistItemUpdated;
            this.queue = null;
            this.editor = null;
            ((MPDStatus)Status).PropertyChanged -= MStatus_PropertyChanged;
        }

        private void MStatus_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName.Equals("PlaylistVersion"))
            {
                Task.Run(async () =>
                {
                    await this.queue.SyncPlaylistAsync(this.end, this.Status.PlaylistVersion).ConfigureAwait(false);
                });
            }
            else if (e.PropertyName == "CurrentTrackIndex")
            {
                int index = Status.CurrentTrackIndex;
                queue.CurrentPosition = index;
                editor.CurrentPosition = index;
            }
        }

        private async void Aurender_OnDatabaseOpened(object sender, bool e)
        {
            await this.queue.SyncPlaylistAsync(this.end, this.Status.PlaylistVersion, true).ConfigureAwait(false);
        }

        public event EventHandler<int> PlaylistFulled;
        public event EventHandler<long> OnPlaylistItemUpdated;

        public IAurenderStatus Status { get; private set; }

        public IPlaylist Queue { get => queue; }

        public IPlaylist Editor { get => editor; }

        public IPlaylist Current { get => IsQueue ? Queue : Editor; }

        public List<string> PlaylistNames { get; private set; } = new List<string>();

        //public long PlaylistVersion { get => this.Queue.PlaylistVersion; }

        public string Name { get => Current.Name; }

        public int CurrentPosition { get => Current.CurrentPosition; }

        public int TotalDuration { get => Current.TotalDuration; }

        public bool IsQueue { get; private set; } = true;

        public int Count => Current.Count;

        public bool IsReadOnly => false;

        public IPlayableItem this[int index]
        {
            get => this.Current[index];
            set => this.Current[index] = value;
        }

        public void ActivatePlaylist(bool isQueue) => this.IsQueue = isQueue;

        public void ActivateEditor() => this.IsQueue = false;

        public void ActivateQueue() => this.IsQueue = true;

        async Task UpdateQueueAsync()
        {
            using (var con = new MPDConnection())
            {
                bool connected = await con.InitAsync(end).ConfigureAwait(false);

                var response = await con.SendCommandAsync("playlist").ConfigureAwait(false);
                queue.ResponseLines = response.ResponseLines;
            }
        }

        public async Task AddAsync(String filePath, OptionForAddPosition position = OptionForAddPosition.UserDefault)
        {
            String[] names = new String[] { filePath };

            await AddFiles(names, position).ConfigureAwait(false);
        }

        public async Task AddAsync(IPlayableItem item, OptionForAddPosition position = OptionForAddPosition.UserDefault)
        {
            IPlayableItem[] items = new IPlayableItem[] { item };
            await AddItems(items, position).ConfigureAwait(false);
        }

        public async Task AddRangeAsync(IAlbum album, OptionForAddPosition position = OptionForAddPosition.UserDefault)
        {
            List<IPlayableItem> items = new List<IPlayableItem>();
            album.LoadSongs();

            foreach (var song in album.Songs)
            {
                items.Add(song);
            }
            await AddItems(items, position).ConfigureAwait(false);
        }

        public async Task AddRangeAsync(IEnumerable<IPlayableItem> items, OptionForAddPosition position = OptionForAddPosition.UserDefault)
        {
            await AddItems(items, position).ConfigureAwait(false);
        }

        // T can be string or IPlayableItem
        async Task AddTracks<T>(IEnumerable<T> items, OptionForAddPosition position, Func<T, IPlayableItem> getPlayableItem, Func<T, string> getItemPath)
        {
            Int32 counter = 0;

            StringBuilder sb = new StringBuilder(128);

            if (position == OptionForAddPosition.UserDefault)
                position = Setting.UserSetting.Setting.App.Get(Setting.FieldsForAppConfig.DefaultActionForSelect, OptionForAddPosition.End);

            Int32 ptr = GetPosition(position);

            IList<IPlayableItem> tracks = new List<IPlayableItem>();

            var remainCount = (2000 - queue.Count());
            var takeCount = remainCount - items.Count() < 0 ? remainCount : items.Count();

            foreach (T item in items.Take(remainCount))
            {
                var track = getPlayableItem(item);
                tracks.Add(track);

                Int32 realPosition = ptr + counter++;
                String strPosition = position == OptionForAddPosition.End ? string.Empty : realPosition.ToString();
                sb.AppendLine($"addid \"{getItemPath(item)}\" {strPosition}");
            }

            bool haveToPlay = position == OptionForAddPosition.Now || Status.State == PlayState.Stopped;
            if (haveToPlay)
            {
                sb.AppendLine($"play {ptr}");
            }

            await Current.AddRangeAsync(tracks, position).ConfigureAwait(false);

            if (counter > 1 || (counter == 1 && haveToPlay))
            {
                sb.Insert(0, "command_list_begin\n");
                sb.AppendLine("command_list_end\n");
            }
            else if (counter == 0)
            {
                await RunCommand("idle").ConfigureAwait(false);
            }

            await RunCommand(sb.ToString()).ConfigureAwait(false);
            await UpdateQueueAsync().ConfigureAwait(false);
        }

        public async Task AddFiles(IEnumerable<String> items, OptionForAddPosition position = OptionForAddPosition.UserDefault)
        {
            await AddTracks(items, position, GetPlayableItem, x => x);
        }

        private async Task AddItems(IEnumerable<IPlayableItem> items, OptionForAddPosition position = OptionForAddPosition.UserDefault)
        {
            await AddTracks(items, position, item =>
            {
                IPlayableItem track = null;
                if (item is IStreamingTrack streamingTrack)
                {
                    track = new PlayingStreamingTrack(streamingTrack);
                    streamingTrack.AddToCache().ConfigureAwait(false);
                }
                else
                {
                    track = GetPlayableItem(item.ItemPath);
                }
                return track;
            },
            x => x.ItemPath);
        }

        private int GetPosition(OptionForAddPosition position)
        {
            if (this.Status.PlaylistLength < 0)
            {
                return 0;
            }

            int nexIndex = 0;

            if (position == OptionForAddPosition.End)
                nexIndex = this.Count;
            else
                nexIndex = CurrentPosition + 1;

            return nexIndex;
        }

        public List<IPlayableItem> GetStreamingItems(IStreamingService service)
        {
            //   List<IPlayableItem> streamingItems = this.Current
            throw new NotImplementedException();
        }

        public async void Next()
        {
            var cmd = "next";

            await RunCommand(cmd).ConfigureAwait(false);
        }

        public async void Pause()
        {
            var cmd = "pause";

            await RunCommand(cmd).ConfigureAwait(false);
        }

        public async void Play()
        {
            var cmd = "playone";

            await RunCommand(cmd).ConfigureAwait(false);
        }

        public async void Play(IPlayableItem item)
        {
            int index = queue.IndexOf(item);

            await PlayAsync(index).ConfigureAwait(false);
        }

        public async void Play(int index)
        {
            await PlayAsync(index).ConfigureAwait(false);
        }

        private async Task PlayAsync(int index)
        {
            var cmd = "play";

            await RunCommand(cmd, index.ToString()).ConfigureAwait(false);
        }

        public async void Previous()
        {
            var cmd = "previous";

            await RunCommand(cmd).ConfigureAwait(false);
        }

        public async Task<bool> LoadPlaylistNamesAsync()
        {
            List<String> newList = new List<string>();

            using (MPDConnection cn = new MPDConnection())
            {
                bool result = false;
                var isconnected = await cn.InitAsync(this.end).ConfigureAwait(false);

                if (isconnected)
                {
                    var cmdResult = await cn.SendCommandAsync("listplaylists").ConfigureAwait(false);

                    if (!cmdResult.IsOk)
                    {
                        this.EP(Tag, $"load playlist failed : {cmdResult.ErrorMessage}");
                        return false;
                    }
                    else
                    {
                        result = cmdResult.IsOk;
                        //playlist: Cafe Sounds - Café Sounds
                        //Last-Modified: 2017-10-24T01:40:03Z

                        String pattern = "playlist: (.*)";
                        Regex regex = new Regex(pattern);
                        foreach (String line in cmdResult.ResponseLines)
                        {
                            var matches = regex.Match(line);
                            if (matches.Success)
                            {
                                String playlistName = matches.Groups[1].Value;
                                newList.Add(playlistName);
                            }
                        }
                    }
                }
                else
                {
                    this.EP(Tag, "Failed to get connected for load playlist");
                    return false;
                }
            }

            newList.Sort();
            this.PlaylistNames = newList;

            return true;
        }

        public async Task ReplaceQueueWithPlaylistAsync(string name)
        {
            var cmd = $"load \"{name}\"";

            var builder = new StringBuilder();
            builder.AppendLine("command_list_begin");
            builder.AppendLine("clear");
            builder.AppendLine(cmd);
            builder.AppendLine("command_list_end");

            await RunCommand(builder.ToString()).ConfigureAwait(false);
        }

        public async Task AddPlaylistToQueueAsync(string name)
        {
            var cmd = "load";

            await RunCommand(cmd, $"\"{name}\"").ConfigureAwait(false);
        }
        
        public void Save(String name)
        {
            Current.Save(name);
        }

        public async void SeekCurrent(int sec)
        {
            var command = new StringBuilder($"seekcur {sec}\n");

            if (Status.IsRandom)
            {
                command.Insert(0, "command_list_begin\n" + "random 0\n");
                command.AppendLine("random 1\n" + "command_list_end\n");
            }

            var result = await RunCommand(command.ToString()).ConfigureAwait(false);
        }

        public async void ShuffleItems()
        {
            var cmd = "shuffle";

            await RunCommand(cmd).ConfigureAwait(false);
        }

        public async void ToNextRandomMode()
        {
            var cmd = "random";

            await RunCommand(cmd, Status.IsRandom ? "0" : "1").ConfigureAwait(false);
        }
        public async void SetConsume(bool consume)
        {
            var cmd = "consume";

            await RunCommand(cmd, consume ? "1" : "0").ConfigureAwait(false);
        }

        public async void SetRandom(bool random)
        {
            var cmd = "random";

            await RunCommand(cmd, random ? "1" : "0").ConfigureAwait(false);
        }

        public async void ToNextRepeatMode()
        {
            var nextRepeat = Status.RepeatMode.NextRepeatMode();
            var cmd = nextRepeat.CommandForMpd();

            await RunCommand(cmd).ConfigureAwait(false);
        }

        public async void SetRepeat(OptionForRepeat newReaat)
        {
            var cmd = newReaat.CommandForMpd();

            await RunCommand(cmd).ConfigureAwait(false);
        }

        private async Task<Boolean> RunCommand(string cmd, params string[] arguments)
        {
            using (MPDConnection cn = new MPDConnection())
            {
                bool result = false;
                var isconnected = await cn.InitAsync(this.end).ConfigureAwait(false);

                if (isconnected)
                {
                    var cmdResult = await cn.SendCommandAsync(cmd, arguments).ConfigureAwait(false);

                    if (!cmdResult.IsOk)
                    {
                        this.EP(Tag, $"\"{cmd}\" failed : {cmdResult.ErrorMessage}");
                    }
                    result = cmdResult.IsOk;
                }
                else
                {
                    this.EP(Tag, $"Failed to open connection for \"{cmd}\"");
                }

                return result;
            }
        }

        public void Add(IPlayableItem item)
        {
            this.AddAsync(item, OptionForAddPosition.UserDefault).ConfigureAwait(false);
        }

        public async Task ClearExceptCurrentlyPlayingAsync()
        {
            if (Current is MPDQueue queue)
            {
                queue.ClearExceptCurrentlyPlaying(Status.CurrentSong);

                var command = new StringBuilder("clearEplaying\n");

                if (Status.IsRandom)
                {
                    command.Insert(0, "command_list_begin\n" + "random 0\n");
                    command.AppendLine("random 1\n" + "command_list_end\n");
                }

                var result = await RunCommand(command.ToString()).ConfigureAwait(false);
                
            }
        }

        public bool Contains(IPlayableItem item)
        {
            return Current.Contains(item);
        }

        public void CopyTo(IPlayableItem[] array, int arrayIndex)
        {
            Current.CopyTo(array, arrayIndex);
        }

        public bool Remove(IPlayableItem item)
        {
            RunCommand($"delete {queue.IndexOf(item)}").ConfigureAwait(false);
            UpdateQueueAsync().ConfigureAwait(false);
            return Current.Remove(item);
        }

        public void RemoveAt(int index)
        {
            Current.RemoveAt(index);
            if (Current == this.queue)
            {
                var result = RunCommand($"delete {index}").ConfigureAwait(false);
                UpdateQueueAsync().ConfigureAwait(false);
            }
        }

        public async Task RemoveRangeAsync(IEnumerable<IPlayableItem> items)
        {
            var command = new StringBuilder("command_list_begin\n");
            foreach (var item in items)
            {
                int index = Current.IndexOf(item);
                if (index == -1)
                {
                    // TODO: Fix
                    // when remove songs after reordering 
                    // indexof always return -1
                    IARLogStatic.Error("Queue", "Fail to remove songs when editing the queue.");
                    return;
                }

                command.AppendLine($"delete {index}");
                Current.Remove(item);
            }
            command.AppendLine("command_list_end\n");

            var cmd = command.ToString();
            await RunCommand(cmd).ConfigureAwait(false);
            await UpdateQueueAsync().ConfigureAwait(false);
        }

        public void Move(int oldIndex, int newIndex)
        {
            MoveAsync(oldIndex, newIndex).ConfigureAwait(false);
        }

        public async Task MoveAsync(int oldIndex, int newIndex)
        {
            Current.Move(oldIndex, newIndex);
            await RunCommand($"move {oldIndex} {newIndex}").ConfigureAwait(false);
            await UpdateQueueAsync().ConfigureAwait(false);
        }

        public IEnumerator<IPlayableItem> GetEnumerator()
        {
            return Current.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return Current.GetEnumerator();
        }

        public int IndexOf(IPlayableItem item)
        {
            return Current.IndexOf(item);
        }

        public void Insert(int index, IPlayableItem item)
        {
            Current.Insert(index, item);
            if (Current == this.queue)
            {
                var result = RunCommand($"addid \"{item.ItemPath}\" {index}").ConfigureAwait(false);
                UpdateQueueAsync().ConfigureAwait(false);
            }
        }

        public async Task<bool> Load(string nameOfPlaylist)
        {
            bool result = false;
            var cmd = "load";

            if (IsQueue)
            {
                await RunCommand(cmd, nameOfPlaylist).ConfigureAwait(false);

                result = true;
            }
            else
            {
                result = await this.Current.Load(nameOfPlaylist);
            }

            return result;
        }

        public void ReloadPlaylist()
        {
            MPDStatus mStatus = (MPDStatus)this.Status;

            mStatus.ResetPlaylistVersion();
        }

        public async Task ClearAsync()
        {
            Current.Clear();
            if (Current == this.queue)
            {
                var result = await RunCommand("clear").ConfigureAwait(false);
                await UpdateQueueAsync().ConfigureAwait(false);

                queue.CurrentPosition = -1;
                //                result.Wait();
            }
        }

        void ICollection<IPlayableItem>.Clear()
        {
            ClearAsync().Wait(); 
        }

        public void ReplaceItem(IStreamingTrack track)
        {
            throw new NotSupportedException();
        }

        //TODO: delete cache is not working
        public void DeleteCachedFile(string path)
        {
            var singleQuote = "'";
            var doubleQuote = '"';

            if (path.Contains(singleQuote))
                path = $"{doubleQuote}{path}{doubleQuote}";

            if (path.Contains(doubleQuote))
                path = $"{singleQuote}{path}{singleQuote}";

            path.Replace("'", "'\'");
            path.Replace('"', '\"');
            
            RunCommand($"cache_rm", path).ConfigureAwait(false);
        }
    }
}
