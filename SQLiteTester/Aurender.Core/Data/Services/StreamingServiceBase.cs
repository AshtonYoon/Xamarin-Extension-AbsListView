using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Aurender.Core.Contents.Streaming;
using Aurender.Core.Player;
using Aurender.Core.Player.mpd;
using Aurender.Core.Setting;
using Aurender.Core.Utility;

namespace Aurender.Core.Data.Services
{
    internal abstract class StreamingServiceBase : StreamingServiceImplementation, IStreamingService, IARLog
    {

        #region IARLog
        private bool LogAll = true;
        bool IARLog.IsARLogEnabled { get { return LogAll; } set { LogAll = value; } }
        #endregion
        internal protected StreamingServiceBase(ContentType cType, String name, String siteURL, String joinURL, String accountInfoLink)
        {
            this.cookie = new CookieContainer();
            this.ServiceType = cType;
            this.Name = name;
            this.ServiceSiteURL = siteURL;
            this.ServiceJoinURL = joinURL;
            this.credentialLink = accountInfoLink;

            this.collectionsForTracks = new Dictionary<String, IStreamingObjectCollection<IStreamingTrack>>();
            this.collectionsForAlbums = new Dictionary<String, IStreamingObjectCollection<IStreamingAlbum>>();
            this.collectionsForArtists = new Dictionary<String, IStreamingObjectCollection<IStreamingArtist>>();
            this.collectionsForGenres = new Dictionary<String, IStreamingObjectCollection<IStreamingGenre>>();
            this.collectionsForPlaylists = new Dictionary<String, IStreamingObjectCollection<IStreamingPlaylist>>();

            PrepareCachedTracks();
            this.cachedFavoriteIDs = new List<HashSet<string>>(4);

            PrepareFavorites();

            //this.LP($"{ServiceType}", $"this");
        }

        public static string Search = LocaleUtility.Translate("Search");
        public static string MagnifyingGlass = "🔍";

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder($"Streaming Service : {ServiceType}");
            /*            sb.AppendLine();
                        sb.AppendLine($"Cached  tracks : {cachedTracks?.Count ?? 0}");
                        sb.AppendLine($"Tracks  Collections : {TitlesForTracks}");
                        sb.AppendLine($"Albums  Collections : {TitlesForAlbums}");
                        sb.AppendLine($"Artists Collections : {TitlesForArtists}");
                        sb.AppendLine($"Genres  Collections : {TitlesForGenres}");
                        sb.AppendLine($"Playlists Collections : {TitlesForPlaylists}");
                        sb.AppendLine($"FavoriteIDs tracks    : {cachedFavoriteIDs[0].Count}");
                        sb.AppendLine($"FavoriteIDs albums    : {cachedFavoriteIDs[1].Count}");
                        sb.AppendLine($"FavoriteIDs artists   : {cachedFavoriteIDs[2].Count}");
                        sb.AppendLine($"FavoriteIDs playlists : {cachedFavoriteIDs[3].Count}");
                        */

            return sb.ToString();
        }
        public IDictionary<StreamingServiceLoginInformation, String> ServiceInformation { get; protected set; }
        public String SelectedStreamingQuality
        {
            get
            {
                if (ServiceInformation != null)
                {
                    if (ServiceInformation.ContainsKey(StreamingServiceLoginInformation.StreamingQuality))
                        return ServiceInformation[StreamingServiceLoginInformation.StreamingQuality];
                    return "N/A";
                }
                else
                    return "N/A";
            }
            set
            {
                if (this.SupportedStreamingQuality.Contains(value))
                {
                    this.ServiceInformation[StreamingServiceLoginInformation.StreamingQuality] = value;
                    SaveServiceInformation(AurenderBrowser.GetCurrentAurender().ConnectionInfo);
                }
                else
                    this.EP($"{ServiceType}", $"Failed set service quality {value} since it is not supported type");
            }
        }

        protected CookieContainer cookie;
        public ContentType ServiceType { get; private set; }
        public string Name { get; private set; }

        public string ServiceSiteURL { get; private set; }

        public string ServiceJoinURL { get; private set; }
        public string SubscriptionInfo
        {
            get
            {
                var key = StreamingServiceLoginInformation.SubscriptionInfo;

                if (ServiceInformation.ContainsKey(key))
                    return ServiceInformation[key];
                else
                    return string.Empty;
            }
        }

        public HashSet<StreamingServiceFeatures> Capability { get; protected set; }

        public bool IsLoggedIn
        {
            get;
            protected set;
        }

        public String LogInMessage
        {
            get
            {
                if (loginMessages.Count > 0)
                {
                    return loginMessages.Last();
                }
                else
                    return String.Empty;
            }
        }


        public abstract IList<string> AvailableStreamingQuality { get; }
        public abstract IList<string> SupportedStreamingQuality { get; }


        public IStreamingObjectCollection<IStreamingTrack> TracksForTitle(string title)
        {
            return CollectionForTitle(collectionsForTracks, title);
        }
        public IStreamingObjectCollection<IStreamingAlbum> AlbumsForTitle(string title)
        {
            return CollectionForTitle(collectionsForAlbums, title);
        }
        public IStreamingObjectCollection<IStreamingArtist> ArtistsForTitle(string title)
        {
            return CollectionForTitle(collectionsForArtists, title);
        }
        public IStreamingObjectCollection<IStreamingGenre> GenresForTitle(string title)
        {
            return CollectionForTitle(collectionsForGenres, title);
        }
        public IStreamingObjectCollection<IStreamingPlaylist> PlaylistsForTitle(string title)
        {
            return CollectionForTitle(collectionsForPlaylists, title);
        }

        IStreamingObjectCollection<T> CollectionForTitle<T>(IDictionary<string, IStreamingObjectCollection<T>> collections, string title)
            where T : IStreamingServiceObject
        {
            if (collections.TryGetValue(title, out var collection))
            {
                return collection;
            }

            this.EP($"{ServiceType}", "Fail to get collection", logs: new Dictionary<string, string>
            {
                { "Collection type", typeof(T).ToString() },
                { "Title", title },
            });
            return null;
        }

        public virtual IList<String> TitlesForCollectionForGenre()
        {
            this.Info($"{ServiceType}", "Doesn't support CollectionForGenreTitle");
            return new List<string>();
        }

        public virtual IStreamingObjectCollection<U> CollectionForGenreTitle<U>(String title)
            where U : IStreamingServiceObject
        {
            this.Info($"{ServiceType}", "Doesn't support CollectionForGenreTitle");
            return null;
        }

        public string ArtistWithAlbumForTrack(IStreamingTrack track)
        {
            if (track == null)
                return String.Empty;

            String album = track.AlbumTitle.Trim();
            String artist = track.ArtistName.Trim();

            if (album.Length == 0 && artist.Length == 0)
            {
                return $"N/A";
            }
            else if (album.Length == 0)
            {
                return album;
            }
            else if (artist.Length == 0)
            {
                return artist;
            }

            return $"{artist} - {album}";
        }


        public abstract Task<Tuple<bool, string>> TryLoginAsync(IDictionary<StreamingServiceLoginInformation, string> information);

        public virtual async Task CheckServiceAndLoginIfAvailableAsync(IAurenderEndPoint aurender)
        {
                try
                {
                    var info = await LoadLoginInformation(aurender);

                    if (info == null)
                    {
                        this.Info($"{ServiceType}", "Failed to get login information from aurender, so we don't try to login.");
                        if (IsLoggedIn)
                        {
                            Logout();
                        }
                        return;
                    }

                    if (this.IsLoggedIn)
                    {
                        if (info[StreamingServiceLoginInformation.UserID] == this.ServiceInformation[StreamingServiceLoginInformation.UserID])
                        {
                            this.Info($"{ServiceType}", "Already logged in with same ID, so we skip.");
                            return;
                        }
                        else
                            Logout();
                    }
                    this.ServiceInformation = info;

                    var result = await TryLoginAsync(this.ServiceInformation).ConfigureAwait(false);
                
                    if (result.Item1 == false)
                    {
                        NotifyServiceAlert(result.Item2);
                    }
                    else
                    {
                        this.NotifyServiceLogin(true);
                    }
                }
                catch (Exception ex)
                {
                    this.EP($"{ServiceType}", "Failed ", ex);
                }
        }

        public void Logout()
        {
            this.collectionsForTracks?.Clear();
            this.collectionsForAlbums?.Clear();
            this.collectionsForArtists?.Clear();
            this.collectionsForGenres?.Clear();
            this.collectionsForPlaylists?.Clear();

            ProtectedLogout();
            SaveStatus();

            this.ServiceInformation?.Clear();

            NotifyServiceLogin(false);
        }


        public void SaveStatus()
        {
            try
            {
                config.SetCashedTracks(this.cachedTracks);
                if (this.cachedFavoriteIDs.Count > 0)
                config[FieldsForServiceCache.FavoriteTracks] = this.cachedFavoriteIDs[0];
                if (this.cachedFavoriteIDs.Count > 1)
                config[FieldsForServiceCache.FavoriteAlbums] = this.cachedFavoriteIDs[1];
                if (this.cachedFavoriteIDs.Count > 2)
                config[FieldsForServiceCache.FavoriteArtists] = this.cachedFavoriteIDs[2];
                if (this.cachedFavoriteIDs.Count > 3)
                config[FieldsForServiceCache.FavoritePlaylists] = this.cachedFavoriteIDs[3];

            }
            catch (Exception ex)
            {
                this.EP($"{ServiceType}", "Failed while SaveStatus ", ex);
            }
        }

        public abstract object IconForQueue(bool isPlaying);

        public object ImageForFavorite(bool isFavorite)
        {
            return ServiceType.GetFavoriteImage(isFavorite);
        }

        public object IconFor(bool selected)
        {
            return ServiceType.GetServiceIcon(selected);
        }

        public virtual object IconForSetting { get; }

        public virtual object IconForAddToMyLibraryProcessing { get; }

        public Task SearchForAsync(EViewType viewType, string keyword, LoadAsyncCompletion callback)
        {
            switch (viewType)
            {
                case EViewType.Artists:
                case EViewType.Composers:
                case EViewType.Conductors:
                    var searchResult = SearchResultForViewType<IStreamingArtist>(viewType);
                    return searchResult.SearchAsync(keyword);

                case EViewType.Albums:
                    var searchResult2 = SearchResultForViewType<IStreamingAlbum>(viewType);
                    return searchResult2.SearchAsync(keyword);

                case EViewType.Genres:
                    var searchResult3 = SearchResultForViewType<IStreamingGenre>(viewType);
                    return searchResult3.SearchAsync(keyword);

                case EViewType.Folders:
                    var searchResult5 = SearchResultForViewType<IStreamingPlaylist>(viewType);
                    return searchResult5.SearchAsync(keyword);

                case EViewType.Songs:
                default:
                    var searchResult4 = SearchResultForViewType<IStreamingTrack>(viewType);
                    return searchResult4.SearchAsync(keyword);
            }
        }

        public IStreamgingSearchResult<T> SearchResultForViewType<T>(EViewType viewType) where T : IStreamingServiceObject
        {
            var col = SearchResultCollectionForViewType<T>(viewType);

            Func<KeyValuePair<String, IStreamingObjectCollection<T>>, bool> checker = delegate (KeyValuePair<String, IStreamingObjectCollection<T>> searchResult)
            {
                if (searchResult.Value is IStreamgingSearchResult<T>)
                {
                    return true;
                }
                return false;
            };

            if (col == null)
            {
                return null;
            }

            if (!this.Capability.Contains(StreamingServiceFeatures.PlaylistSearch) 
                && viewType == EViewType.Folders)
            {
                return null;
            }

            var result = col.First(checker);

            return result.Value as IStreamgingSearchResult<T>;
        }


        public IList<string> TitlesForViewType(EViewType type)
        {
            IList<String> result = null;

            switch (type)
            {

                case EViewType.Artists:
                case EViewType.Composers:
                case EViewType.Conductors:
                    result = TitlesForArtists;
                    break;

                case EViewType.Albums:
                    result = TitlesForAlbums;
                    break;

                case EViewType.Genres:
                    result = TitlesForGenres;
                    break;

                case EViewType.Folders:
                    result = TitlesForPlaylists;
                    break;

                default:
                case EViewType.Songs:
                    result = TitlesForTracks;
                    break;
            }

            return result;
        }



        public abstract Task<IStreamingPlaylist> CreatePlaylistAsync(string title, IList<IStreamingTrack> tracks, string description = "");


        public virtual IStreamingTrack GetTrackWithPath(String filePath)
        {
            String streamingID = StreamingSerivceTypeMethods.StreamingIDFromPath(filePath);
            if ((streamingID?.Length ?? 0) == 0)
                return null;



            IStreamingTrack t = GetTrackFromCachedWithID(streamingID);
            if (t == null)
            {
                Task.Run(async () =>
                {
                    var track = await GetTrackWithIDAsync(streamingID).ConfigureAwait(false);

                    if (track == null)
                    {
                        this.Info($"{ServiceType}", $"Failed to load track for {filePath}");
                        return;
                    };
                    AddToCache(track);
                    NotifyTrackLoaded(track);
                });
            }

            return t;
        }

        static readonly SemaphoreSlim asyncMutax = new SemaphoreSlim(10, 10);
        public async Task<IStreamingTrack> GetTrackWithIDAsync(string trackID)
        {
            IStreamingTrack cachedTrack = GetTrackFromCachedWithID(trackID);
            if (cachedTrack == null)
            {
                try
                {
                    await asyncMutax.WaitAsync();
                    cachedTrack = await TrackWithIDAsync(trackID).ConfigureAwait(false);
                    //this.LP($"{ServiceType}", $"Track comes from network");
                }
                catch (Exception ex)
                {
                    this.EP($"{ServiceType}", "Failed to get cached track", ex);
                }
                finally
                {
                    asyncMutax.Release();
                }
            }
            return cachedTrack;
        }

        public abstract Task<IStreamingAlbum> GetAlbumWithIDAsync(string albumID);

        public abstract Task<IStreamingArtist> GetArtistWithIDAsync(string artistmID);

        public abstract Task<IStreamingPlaylist> GetPlaylistWithIDAsync(string playlistID);


        public abstract Task<bool> DeletePlaylistAsync(IStreamingPlaylist playlist);
        public abstract Task<bool> UpdatePlaylistAsync(IStreamingPlaylist playlist, string newTitle, IList<IStreamingTrack> tracks, string description = "");


        protected List<String> loginMessages = new List<string>();

        internal List<String> TitlesForAlbums => this.collectionsForAlbums?.Keys.ToList<String>() ?? new List<String>();
        internal List<String> TitlesForTracks
        {
            get
            {
                var result = new List<string>();
                if (collectionsForTracks != null)
                {
                    result.AddRange(collectionsForTracks.Keys);
                    if (ServiceType == ContentType.InternetRadio)
                    {
                        result.Insert(result.Count - 1, collectionsForGenres.Keys.First());
                    }
                }

                return result;
            }
        }
        internal List<String> TitlesForArtists => this.collectionsForArtists?.Keys.ToList<String>() ?? new List<String>();
        internal List<String> TitlesForPlaylists => this.collectionsForPlaylists?.Keys.ToList<String>() ?? new List<String>();
        internal virtual List<String> TitlesForGenres => this.collectionsForGenres?.Keys.ToList<String>() ?? new List<String>();

        public void AddToCache(IStreamingTrack track)
        {
            if (track.ServiceType == this.ServiceType)
            {
                lock (lockForCachedTracks)
                {
                    if (cachedTracks.Contains(track))
                    {
                        this.cachedTracks.Remove(track);
                    }

                    this.cachedTracks.AddLast(track);

                    if (cachedTracks.Count > 100)
                    {
                        cachedTracks.RemoveFirst();
                    }
                }
                //DescribeCachedTracks();
            }
            else
            {
                this.EP($"{ServiceType}", $"You added to wrong service. {track}");
            }
        }



        protected readonly String credentialLink;


        protected IDictionary<String, IStreamingObjectCollection<IStreamingAlbum>> collectionsForAlbums;
        protected IDictionary<String, IStreamingObjectCollection<IStreamingTrack>> collectionsForTracks;
        protected IDictionary<String, IStreamingObjectCollection<IStreamingArtist>> collectionsForArtists;
        protected IDictionary<String, IStreamingObjectCollection<IStreamingGenre>> collectionsForGenres;
        protected IDictionary<String, IStreamingObjectCollection<IStreamingPlaylist>> collectionsForPlaylists;

        protected List<HashSet<String>> cachedFavoriteIDs;
        protected LinkedList<IStreamingTrack> cachedTracks;
        private Object lockForCachedTracks = new object();

        public event StreamingServiceEventHandler OnServiceLoginStatusChanged;
        public event StreamingServiceEventHandler<IStreamingTrack> OnStreamingTrackLoaded;
        public event StreamingServiceEventHandler<string> GetMessageFromService;
        public event StreamingServiceEventHandler<IStreamingFavoritable, bool> OnFavoriteItemStatusChanged;
        public event StreamingServiceEventHandler<IStreamingFavoritable> OnFavoriteItemStatusChangeFailed;

        protected abstract void ProtectedLogout();

        protected IDictionary<String, IStreamingObjectCollection<T>> SearchResultCollectionForViewType<T>(EViewType viewType) where T : IStreamingServiceObject
        {
            IDictionary<string, IStreamingObjectCollection<T>> result = null;

            switch (viewType)
            {

                case EViewType.Artists:
                case EViewType.Composers:
                case EViewType.Conductors:
                    result = collectionsForArtists as IDictionary<string, IStreamingObjectCollection<T>>;
                    break;

                case EViewType.Albums:
                    result = collectionsForAlbums as IDictionary<String, IStreamingObjectCollection<T>>;
                    break;

                case EViewType.Genres:
                    result = collectionsForGenres as IDictionary<string, IStreamingObjectCollection<T>>;
                    break;

                case EViewType.Folders:
                    result = collectionsForPlaylists as IDictionary<string, IStreamingObjectCollection<T>>;
                    break;

                default:
                case EViewType.Songs:
                    result = collectionsForTracks as IDictionary<string, IStreamingObjectCollection<T>>;
                    break;
            }

            return result;

        }
        protected void NotifyServiceLogin(bool newStatus)
        {
            this.IsLoggedIn = newStatus;
            if (newStatus)
            {
                SaveStatus();

                var connectionInfo = AurenderBrowser.GetCurrentAurender().ConnectionInfo;
                SaveServiceInformation(connectionInfo);
            }
            Task.Run(() => OnServiceLoginStatusChanged?.Invoke(this)).ContinueWith(task =>
            {
                task.Exception.Handle(ex =>
                {
                    IARLogStatic.Error("Exception in Event", $"For {ServiceType} Service.OnServiceLoginStatusChanged.", ex);
                    return true;
                });

            }, TaskContinuationOptions.OnlyOnFaulted);
        }


        protected void NotifyServiceAlert(string item2)
        {
            this.Info($"{ServiceType}", $"Alert : {item2}");
            Task.Run(() => GetMessageFromService?.Invoke(this, item2)).ContinueWith(task =>
            {
                task.Exception.Handle(ex =>
                {
                    IARLogStatic.Error("Exception in Event", $"For {ServiceType} Service.GetMessageFromService.", ex);
                    return true;
                });

            }, TaskContinuationOptions.OnlyOnFaulted);
        }

        protected void NotifyTrackLoaded(IStreamingTrack track)
        {
            //this.Info($"{ServiceType}", $"Track is loaded : {track.Title}");
            Task.Run(() => OnStreamingTrackLoaded?.Invoke(this, track)).ContinueWith(task =>
            {
                task.Exception.Handle(ex =>
                {
                    IARLogStatic.Error("Exception in Event", $"For {ServiceType} Service.OnStreamingTrackLoaded.", ex);
                    return true;
                });

            }, TaskContinuationOptions.OnlyOnFaulted);
        }


        protected void NotifyFavoriteUpdated(IStreamingFavoritable item, bool favorite)
        {
            this.Info($"{ServiceType}", $"Favorite updated : {item}, isFavorite:{favorite}");
            Task.Run(() => OnFavoriteItemStatusChanged?.Invoke(this, item, favorite)).ContinueWith(task =>
            {
                task.Exception.Handle(ex =>
                {
                    IARLogStatic.Error("Exception in Event", $"For {ServiceType} Service.OnFavoriteItemStatusChanged.", ex);
                    return true;
                });

            }, TaskContinuationOptions.OnlyOnFaulted);
        }

        protected void NotifyFavoriteUpdateFailed(IStreamingFavoritable item)
        {
            this.Info($"{ServiceType}", $"Favorite update failed : {item}");
            Task.Run(() => OnFavoriteItemStatusChangeFailed?.Invoke(this, item)).ContinueWith(task =>
            {
                task.Exception.Handle(ex =>
                {
                    IARLogStatic.Error("Exception in Event", $"For {ServiceType} Service.OnFavoriteItemStatusChangeFailed.", ex);
                    return true;
                });

            }, TaskContinuationOptions.OnlyOnFaulted);
        }

        protected abstract Task LoadCatalog();
        protected abstract Task LoadFavorites();
        protected abstract void PrepareSearchResult();

        protected abstract Task<IStreamingTrack> TrackWithIDAsync(string trackID);

        #region StreamingServiceImplementation
        //internal abstract bool IsUserUpdatable(IStreamingPlaylist playlist);
        //internal abstract String URLFor(String partialPath, String paramString = "");
        //internal abstract string URLForWithoutDefaultParam(string partialPath);
        //internal abstract Task UpdateFavorite(IStreamingFavoritable item, bool favorite);
        internal override bool IsFavorite(IStreamingFavoritable item)
        {
            int index = (int)item.StreamingItemType;
            if (cachedFavoriteIDs.Count <= index) return false;

            var cached = cachedFavoriteIDs[index];

            return cached?.Contains(item.StreamingID) ?? false;
        }
        #endregion

        protected async Task<Dictionary<StreamingServiceLoginInformation, String>> LoadLoginInformation(IAurenderEndPoint aurender)
        {
            String url = $"http://{aurender.IPV4Address}:{aurender.WebPort}/php/{credentialLink}?args=query";

            var result = await WebUtil.DownloadContentsAsync(url);

            using (Stream str = GenerateStreamFromString(result.Item2))
            {
                using (StreamReader sr = new StreamReader(str))
                {
                    Dictionary<StreamingServiceLoginInformation, String> info = new Dictionary<StreamingServiceLoginInformation, string>();

                    var decoded = Utility.UUEncodeDecode.UUDecode(sr);

                    String information = Encoding.UTF8.GetString(decoded, 0, decoded.Length);

                    const string FIELD_USER = "user=";
                    const string FIELD_PASSWORD = "passwd=";
                    const string FIELD_QUALITY = "quality=";
                    const string FIELD_TOKEN = "token=";


                    using (StringReader strReader = new StringReader(information))
                    {
                        String line = strReader.ReadLine();

                        while (line != null)
                        {
                            if (line.StartsWith(FIELD_USER))
                            {
                                string value = line.Substring(FIELD_USER.Length);
                                info[StreamingServiceLoginInformation.UserID] = value;
                            }
                            else if (line.StartsWith(FIELD_PASSWORD))
                            {
                                string value = line.Substring(FIELD_PASSWORD.Length);
                                info[StreamingServiceLoginInformation.UserPassword] = value;

                            }
                            else if (line.StartsWith(FIELD_QUALITY))
                            {
                                string value = line.Substring(FIELD_QUALITY.Length);
                                info[StreamingServiceLoginInformation.StreamingQuality] = value;
                            }
                            else if (line.StartsWith(FIELD_TOKEN))
                            {
                                string value = line.Substring(FIELD_TOKEN.Length);
                                info[StreamingServiceLoginInformation.LoginToken] = value.URLEncodedString();
                            }

                            line = strReader.ReadLine();
                        }
                    }
                    if (info.Keys.Count >= 3)
                        return info;
                    else
                    {
                        return null;
                    }
                }
            }
        }

        static readonly HashSet<StreamingServiceLoginInformation> keys = new HashSet<StreamingServiceLoginInformation>
        {
            StreamingServiceLoginInformation.UserID,
            StreamingServiceLoginInformation.UserPassword,
            StreamingServiceLoginInformation.StreamingQuality
        };

        protected async void SaveServiceInformation(IAurenderEndPoint aurender)
        {
            if (aurender == null) return;

            if (ServiceType == ContentType.InternetRadio) return;

            String url = $"http://{aurender.IPV4Address}:{aurender.WebPort}/php/{credentialLink}?args=query";

            List<KeyValuePair<String, String>> postData = new List<KeyValuePair<string, string>>();

            foreach (var kv in ServiceInformation)
            {
                if (keys.Contains(kv.Key))
                {
                    KeyValuePair<String, String> data = new KeyValuePair<string, string>(kv.Key.TitelInFile(), kv.Value);
                    postData.Add(data);
                }
            }

            await WebUtil.PostDataAndDownloadContentsAsync(url, postData).ConfigureAwait(false);
        }

        protected abstract void PrepareCachedTracks();
        protected virtual void PrepareFavorites()
        {
            if (Capability == null)
            {
                return;
            }

            var serviceCache = UserSetting.Setting[this.ServiceType];
            HashSet<String> cachedID;

            var features = new List<KeyValuePair<StreamingServiceFeatures, FieldsForServiceCache>>()
            {
                new KeyValuePair<StreamingServiceFeatures, FieldsForServiceCache>(StreamingServiceFeatures.FavoriteTrack, FieldsForServiceCache.FavoriteTracks),
                new KeyValuePair<StreamingServiceFeatures, FieldsForServiceCache>(StreamingServiceFeatures.FavoriteAlbum, FieldsForServiceCache.FavoriteAlbums),
                new KeyValuePair<StreamingServiceFeatures, FieldsForServiceCache>(StreamingServiceFeatures.FavoriteArtist, FieldsForServiceCache.FavoriteArtists),
                new KeyValuePair<StreamingServiceFeatures, FieldsForServiceCache>(StreamingServiceFeatures.FavoritePlaylist, FieldsForServiceCache.FavoritePlaylists),
            };

            foreach (var item in features)
            {
                if (Capability.Contains(item.Key))
                    cachedID = serviceCache.Get(item.Value, new HashSet<string>());
                else
                    cachedID = new HashSet<string>();

                this.cachedFavoriteIDs.Add(cachedID);
            }

        }
        protected void UpdateFavoriteChached(IStreamingFavoritable item, bool favorite)
        {
            IStreamingObjectCollection<IStreamingServiceObject> collection = null;

            switch (item.StreamingItemType)
            {
                case StreamingItemType.Track:
                    collection = this.TracksForTitle(ServiceManager.Favorite);
                    break;

                case StreamingItemType.Album:
                    collection = this.AlbumsForTitle(ServiceManager.Favorite);
                    break;

                case StreamingItemType.Aritst:
                    collection = this.ArtistsForTitle(ServiceManager.Favorite);
                    break;

                case StreamingItemType.Playlist:
                    collection = this.PlaylistsForTitle(ServiceManager.Favorite);
                    break;

                case StreamingItemType.Genre:
                default:
                    break;
            }

            if (collection != null)
            {
                collection.Reset();
                collection.LoadNextAsync();
            }

            if (item.StreamingItemType <= StreamingItemType.Playlist)
            {
                HashSet<String> cached = this.cachedFavoriteIDs[(int)item.StreamingItemType];
                if (favorite)
                    cached.Add(item.StreamingID);
                else
                    cached.Remove(item.StreamingID);
            }
            else
            {
                Debug.Assert(false, $"Shouldn't be reached here : TIDAL::UpdateFavoriteChached(IStreamingFavoritable, bool)");
            }
        }


        protected CacheForStreamingService config => UserSetting.Setting[ServiceType];


        private static Stream GenerateStreamFromString(string s)
        {
            MemoryStream stream = new MemoryStream();
            StreamWriter writer = new StreamWriter(stream);
            writer.Write(s);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }

        public static LinkedList<IStreamingTrack> Clone(LinkedList<IStreamingTrack> linkedList)
        {
            var t = new LinkedList<IStreamingTrack>();
            var firxt = linkedList.First;
            if (firxt != null)
            {
                var node = t.AddFirst(firxt.Value);

                while (firxt.Next != null)
                {
                    t.AddAfter(node, firxt.Next.Value);
                    firxt = firxt.Next;
                }
            }
            return t;
        }

        private IStreamingTrack GetTrackFromCachedWithID(String trackID)
        {
            try
            {
                //var cloned = new LinkedList<IStreamingTrack>(cachedTracks); 
                var firxt = cachedTracks.First;

                while (firxt != null)
                {
                    if (firxt.Value.StreamingID == trackID)
                    {
                        //this.LP($"{ServiceType}", $"Track comes from Cached");
                        return firxt.Value;
                    }
                    firxt = firxt.Next;
                }

            }
            catch (Exception ex)
            {
                this.EP($"{ServiceType}", "Failed to get cached track 2", ex);
            }


            return null;
        }

        [Conditional("DEBUG")]
        private void DescribeCachedTracks()
        {
            this.LP($"{ServiceType}", $"Cached tracks count : {this.cachedTracks.Count}");
        }

        public async Task<IStreamingTrack> GetTrackWithPathAsync(string filePath)
        {
            String id = StreamingSerivceTypeMethods.StreamingIDFromPath(filePath);

            if (id != null && id.Length > 0)
            {
                return await this.GetTrackWithIDAsync(id).ConfigureAwait(false);
            }
            else
                return null;
        }

        public IList<(string title, EViewType type)> TitlesForViewType()
        {
            var list = new List<(String, EViewType)>
            {
                (LocaleUtility.Translate("Song"), EViewType.Songs),
                (LocaleUtility.Translate("Artist"), EViewType.Artists),
                (LocaleUtility.Translate("Album"), EViewType.Albums),
                (ServiceType == ContentType.TIDAL ? "Genre/Moods" : LocaleUtility.Translate("Genre"), EViewType.Genres),
                (LocaleUtility.Translate("Playlist"), EViewType.Folders),
            };

            return list;
        }

        internal virtual async Task<HttpResponseMessage> GetResponseByPostDataAsync(string url, List<KeyValuePair<String, String>> postDat = null)
        {
            if (postDat != null) 
            return await WebUtil.GetResponseByPostDataAsync(url, postDat, cookies: this.cookie); 

            return await WebUtil.GetResponseByPostDataAsync(url, new List<KeyValuePair<String, String>>(), cookies: this.cookie); 
        }
        
        internal virtual async Task<HttpResponseMessage> GetResponseByGetDataAsync(string url, List<KeyValuePair<String, String>> postDat = null)
        {
            if (postDat != null)
            return await WebUtil.GetResponseAsync(url, postDat, cookies: this.cookie); 

            return await WebUtil.GetResponseAsync(url,  new List<KeyValuePair<String, String>>(), cookies: this.cookie); 
        }

    }

    internal static class StreamingServiceLoginInformationMethod
    {
        internal static String TitelInFile(this StreamingServiceLoginInformation type)
        {
            String result;
            switch (type)
            {

                case StreamingServiceLoginInformation.UserID:
                    result = "user";
                    break;
                case StreamingServiceLoginInformation.UserPassword:
                    result = "passwd";
                    break;

                case StreamingServiceLoginInformation.StreamingQuality:
                default:
                    result = "quality";
                    break;
            }

            return result;
        }
    }
}
