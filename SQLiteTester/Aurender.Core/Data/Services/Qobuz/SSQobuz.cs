using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Aurender.Core.Contents.Streaming;
using Aurender.Core.Setting;
using Aurender.Core.Utility;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Aurender.Core.Data.Services.Qobuz
{
    class SSQobuz : StreamingServiceBase
    {
        internal SSQobuz() : base(ContentType.Qobuz, "Qobuz", "http://qobuz.com", "http://www.qobuz.com/signup", "qobuzUser")
        {
            this.Capability = new HashSet<StreamingServiceFeatures>()
            {
                StreamingServiceFeatures.AlbumDetail,

                StreamingServiceFeatures.SimilarArtist,

                StreamingServiceFeatures.FavoriteAlbum,
                StreamingServiceFeatures.FavoriteArtist,
                StreamingServiceFeatures.FavoriteTrack,
               // StreamingServiceFeatures.FavoritePlaylist,


                StreamingServiceFeatures.PlaylistCreateion,
                StreamingServiceFeatures.PlaylistSearch,
            };

            cookie = new CookieContainer();

            ProtectedLogout();
            PrepareFavorites();

            this.extraHeader = new List<KeyValuePair<string, string>>();
        }



        private const String qobuzToken = "722123041";
        internal String userID { private set; get; } = String.Empty;

        // TODO: set AvailableStreamingQuality 
        public override IList<string> AvailableStreamingQuality => supportedStreamingQuality;

        static readonly IList<string> supportedStreamingQuality = new[] { "MP3 - 320kbps", "CD - 16bits / 44.1kHz", "HiRes - 24bits / up to 96kHz", "HiRes - 24bits / up to 192kHz" };
        public override IList<string> SupportedStreamingQuality => supportedStreamingQuality;

        private String sessionID = String.Empty;
        private JToken profileInfo;
        private String defaultParam;

        private CookieContainer cookie;
        private List<KeyValuePair<String, String>> extraHeader;


  
        protected override void ProtectedLogout()
        {
            this.defaultParam = $"app_id={qobuzToken}";
        }

        public override IList<String> TitlesForCollectionForGenre()
        {
            return this.TitlesForGenres;
        }
        public override IStreamingObjectCollection<U> CollectionForGenreTitle<U>(String title)
        {
            if (typeof(IStreamingGenre).IsAssignableFrom(typeof(U)))
            {
                IStreamingObjectCollection<IStreamingGenre> collection = null;

                if (collectionsForGenres.ContainsKey(title))
                    collection = collectionsForGenres[title];

                return collection as IStreamingObjectCollection<U>;
            }
            return null;
        }


        public override async Task<Tuple<bool, string>> TryLoginAsync(IDictionary<StreamingServiceLoginInformation, string> information)
        {
            profileInfo = null;

            if (information != null
                && information.ContainsKey(StreamingServiceLoginInformation.UserID)
                && information.ContainsKey(StreamingServiceLoginInformation.UserPassword))
            {
                extraHeader.Clear();
                extraHeader.Add(new KeyValuePair<string, string>("X-App-Id", qobuzToken));

                List<KeyValuePair<String, String>> postData = new List<KeyValuePair<string, string>>();

                String username = information[StreamingServiceLoginInformation.UserID];
                String password = information[StreamingServiceLoginInformation.UserPassword];
                String url = $"http://www.qobuz.com/api.json/0.2/user/login?{defaultParam}&username={username}&password={password}";


                var t = await WebUtil.DownloadContentsAsync(url, cookies: cookie).ConfigureAwait(false);

                bool sucess = t.Item1;

                if (sucess)
                {
                    JToken sInfo = JToken.Parse(t.Item2);

                    var user = sInfo["user"];

                    sucess = false;

                    if (user != null && user["credential"] != null)
                    {
                        JToken userToken = user["credential"] as JToken;

                        this.userID = user["id"].ToString();
                        object id = userToken["id"];
                        if (id != null)
                        {
                            this.sessionID = sInfo["user_auth_token"].ToString();

                            extraHeader.Add(new KeyValuePair<string, string>("X-User-Auth-Token", this.sessionID));

                            profileInfo = sInfo;

                            this.defaultParam = $"app_id={qobuzToken}";

                            var parameters = userToken["parameters"];
                            if (parameters != null)
                            {
                                var subsscriptionInfo = parameters["label"].Value<string>();
                                information.Add(StreamingServiceLoginInformation.SubscriptionInfo, subsscriptionInfo);
                                sucess = true;
                            }

                            //sucess = LoadProfile();

                            if (sucess)
                            {
                                this.defaultParam = $"app_id={qobuzToken}";
                                ServiceInformation = information;
                                await LoadCatalog();
                                await LoadFavorites();
                                PrepareSearchResult();

                                NotifyServiceLogin(true);
                            }
                        }

                    }


                    return new Tuple<bool, string>(sucess, LogInMessage);
                }
                else
                {
                    this.userID = this.sessionID = String.Empty;
                    this.loginMessages.Add("Login failed");
                    return new Tuple<bool, string>(sucess, "Login failed");
                }
            }
            else
            {
                return new Tuple<bool, string>(false, "No user information.");
            }
        }

        private bool LoadProfile()
        {
            return true;
        }

        internal override String URLFor(String partialPath, String paramString = "")
        {
            String str;
            if (paramString.Length > 0)
            {
                if (partialPath.IndexOf("?") > 0)
                    str = $"http://www.qobuz.com/api.json/0.2/{partialPath}&{defaultParam}&{paramString}";
                else
                    str = $"http://www.qobuz.com/api.json/0.2/{partialPath}?{defaultParam}&{paramString}";
            }
            else
            {
                if (partialPath.IndexOf("?") > 0)
                    str = $"http://www.qobuz.com/api.json/0.2/{partialPath}&{defaultParam}";
                else
                    str = $"http://www.qobuz.com/api.json/0.2/{partialPath}?{defaultParam}";
            }

            return str;
        }
        internal override String URLForWithoutDefaultParam(String partialPath)
        {
            String str = $"http://www.qobuz.com/api.json/0.2/{partialPath}";
            return str;
        }


        public override async Task<IStreamingAlbum> GetAlbumWithIDAsync(string albumID)
        {
            String url = URLFor("album/get", $"album_id={albumID}");

            using (var t = await GetResponseByGetDataAsync(url, null).ConfigureAwait(false))
            {

                IStreamingAlbum artist = null;

                if (t.IsSuccessStatusCode)
                {
                    var str = await t.Content.ReadAsStringAsync();

                    JToken jtoken = JToken.Parse(str);

                    artist = new QobuzAlbum(jtoken);
                }

                return artist;
            }
        }

        public override async Task<IStreamingArtist> GetArtistWithIDAsync(string artistID)
        {
            String url = URLFor("artist/get", $"artist_id={artistID}");

            using (var t = await GetResponseByGetDataAsync(url, null).ConfigureAwait(false))
            {

                IStreamingArtist artist = null;
                if (t != null && t.IsSuccessStatusCode)
                {
                    var str = await t.Content.ReadAsStringAsync();

                    JToken jtoken = JToken.Parse(str);

                    artist = new QobuzArtist(jtoken);
                }

                return artist;
            }
        }

        public override async Task<IStreamingPlaylist> GetPlaylistWithIDAsync(string playlistID)
        {
            String url = URLFor("playlist/get", $"playlist_id={playlistID}");

            using (var t = await GetResponseByGetDataAsync(url, null))
            {

                IStreamingPlaylist playlist = null;
                if (t.IsSuccessStatusCode)
                {
                    var str = await t.Content.ReadAsStringAsync();

                    JToken jtoken = JToken.Parse(str);

                    playlist = new QobuzPlaylist(jtoken);
                }

                return playlist;
            }
        }

        protected override async Task<IStreamingTrack> TrackWithIDAsync(string trackID)
        {
            String url = URLFor("track/get", $"track_id={trackID}");

            using (var t = await GetResponseByGetDataAsync(url, null).ConfigureAwait(false))
            {

                if (t.IsSuccessStatusCode)
                {
                    var json = await t.Content.ReadAsStringAsync().ConfigureAwait(false);

                    Object obj = JsonConvert.DeserializeObject(json);

                    if (obj is JToken)
                    {
                        var track = new QobuzTrack(obj as JToken);

                        return track;
                    }
                    else
                        this.EP($"{ServiceType}", $"Failed to get track response : {json}");
                }
                else
                {
                    var json = await t.Content.ReadAsStringAsync().ConfigureAwait(false);
                    this.EP($"{ServiceType}", $"Failed to get track info : {json},\n {url}");
                }
            }
            return null;
        }

        public override object IconForQueue(bool isPlaying)
        {
            IARLogStatic.Error($"{this}", "Doesn't support yet");
            return null;
        }

        internal override async Task UpdateFavorite(IStreamingFavoritable item, bool favorite)
        {
            if (!Capability.Contains(item.StreamingItemType.FavoriteFeature()))
            {
                this.Info($"{ServiceType}", $"Does not support favorite for {item}");
                return;
            }

            String itemField = "";
            String action = favorite ? "create" : "delete";


            switch (item.StreamingItemType)
            {
                case StreamingItemType.Track:
                    itemField = "track_ids";
                    break;

                case StreamingItemType.Album:
                    itemField = "album_ids";
                    break;
                case StreamingItemType.Aritst:
                    itemField = "artist_ids";
                    break;

                case StreamingItemType.Playlist:
                    await UpdataeFavoriteForPlaylist(item as QobuzPlaylist, favorite).ConfigureAwait(false);
                    return;

                case StreamingItemType.Genre:
                default:
                    this.EP($"{ServiceType}", "Doesn't support playlist,genre  favorite");
                    break;
            }

            String url = URLFor($"favorite/{action}", $"user_auth_tokeh={this.sessionID}&{itemField}={item.StreamingID}");

            using (var t = await GetResponseByGetDataAsync(url, null).ConfigureAwait(false))
            {

                if (t.IsSuccessStatusCode)
                {
                    UpdateFavoriteChached(item, favorite);
                    NotifyFavoriteUpdated(item, favorite);
                }
                else
                {
                    var response = await t.Content.ReadAsStringAsync().ConfigureAwait(false);
                    this.EP("Qobuz", $"Failed to update favorite [{item}]\nResponse : [{response}]");

                    NotifyFavoriteUpdateFailed(item);
                }
            }
        }

        private async Task UpdataeFavoriteForPlaylist(QobuzPlaylist qobuzPlaylist, bool favorite)
        {
            String action = favorite ? "subscribe" : "unsubscribe";
            String url = URLFor($"playlist/{action}", $"user_auth_token={this.sessionID}&playlist_id={qobuzPlaylist.StreamingID}");

            using (var result = await GetResponseByGetDataAsync(url, null).ConfigureAwait(false))
            {

                if (result.IsSuccessStatusCode)
                {
                    UpdateFavoriteChached(qobuzPlaylist, favorite);
                    NotifyFavoriteUpdated(qobuzPlaylist, favorite);
                }
                else
                {
                    var response = await result.Content.ReadAsStringAsync();

                    this.EP("Qobuz", $"Failed to update favorite [{qobuzPlaylist}]\nResponse : [{response}]");

                    NotifyFavoriteUpdateFailed(qobuzPlaylist);
                }
            }
        }

        protected override async Task LoadCatalog()
        {
            Dictionary<String, string> albumCollections = new Dictionary<string, string>();
            albumCollections.Add("Download Charts", "best-sellers");
            albumCollections.Add("Streaming Charts", "most-streamed");
            albumCollections.Add("New release", "new-releases");
            albumCollections.Add("By the Media", "press-awards");
            albumCollections.Add("By Qobuz", "editor-picks");

            /// Load Artists
            /// Load Genres/Moods
            await this.LoadGenres(albumCollections).ConfigureAwait(false);


            /// Load Playlists
            String purchased = "Purchased";
            String myMusic = "My Music";

            /// Load Tracks
            IStreamingObjectCollection<IStreamingTrack> tracks = new QobuzPurchasedTracks(purchased, sessionID);
            this.collectionsForTracks.Add(tracks.Title, tracks);


            /// Load Albums
            IStreamingObjectCollection<IStreamingAlbum> albums;

            foreach (var kv in albumCollections)
            {
                albums = new QobuzFeaturedAlbums(kv.Key, $"album/getFeatured?type={kv.Value}");
                this.collectionsForAlbums.Add(albums.Title, albums);
            }


            albums = new QobuzPurchasedAlbums(myMusic, sessionID);
            this.collectionsForAlbums.Add(albums.Title, albums);


            IStreamingObjectCollection<IStreamingPlaylist> playlists = new QobuzFeaturedPlaylists("Qobuz Playlists", "playlist/getFeatured?type=editor-picks");
            this.collectionsForPlaylists.Add(playlists.Title, playlists);

        }

        private async Task LoadGenres(Dictionary<String, String> sections)
        {
               QobuzGenreCollection genres = new QobuzGenreCollection();

               await genres.LoadNextAsync().ConfigureAwait(false);

               var downloadTasksQuery =
                   from section in sections select CreateGenreSection(section.Key, section.Value, genres);

               foreach (var col in downloadTasksQuery)
               {
                   this.collectionsForGenres.Add(new KeyValuePair<String, IStreamingObjectCollection<IStreamingGenre>>(col.Title, col));
               }
        }

        private  IStreamingObjectCollection<IStreamingGenre> CreateGenreSection(string title, string path, QobuzGenreCollection genres)
        {
            QobuzGenreCollectionForSection genre = new QobuzGenreCollectionForSection(genres, title, path);
            return genre;
        }

        private async Task<JToken> TokenGetResponseFromURL(String url)
        {
            using (var response = await GetResponseByGetDataAsync(url, null))
            {
                if (response.IsSuccessStatusCode)
                {
                    var jsonString = await response.Content?.ReadAsStringAsync();
                    JToken token = JsonConvert.DeserializeObject<JToken>(jsonString);

                    return token;
                }
                else
                {
                    var a = await response.Content?.ReadAsStringAsync();
                    this.LP("Qobuz", $"Failed to get token from ({url}) : {a}");
                }
            }
            return null;
        }

        protected override async Task LoadFavorites()
        {
            var favorite = ServiceManager.Favorite;
            var star = ServiceManager.Star;

            string url = URLFor($"favorite/getUserFavorites?user_auth_token={this.sessionID}&limit=2000");

            JToken token = await TokenGetResponseFromURL(url);
            JToken t = token["tracks"];

            if (t != null)
            {
                var tracks = new QobuzFavoriteTracks(favorite, url, t);
                if (tracks != null)
                {
                    this.collectionsForTracks.Add(collectionsForTracks.Count >= 5 ? star : favorite, tracks);
                    await tracks.LoadNextAsync().ConfigureAwait(false);
                    this.cachedFavoriteIDs[0] = tracks.AllIDs();
                }
            }

            t = token["albums"];
            if (t != null)
            {
                var albums = new QobuzFavoriteAlbums(favorite, url, t);
                if (albums != null)
                {
                    this.collectionsForAlbums.Add(collectionsForAlbums.Count >= 5 ? star : favorite, albums);
                    await albums.LoadNextAsync().ConfigureAwait(false);
                    this.cachedFavoriteIDs[1] = albums.AllIDs();
                }
            }


            t = token["artists"];
            if (t != null)
            {
                var artists = new QobuzFavoriteArtists(favorite, url, t);
                if (artists != null)
                {
                    this.collectionsForArtists.Add(collectionsForArtists.Count >= 5 ? star : favorite, artists);
                    await artists.LoadNextAsync().ConfigureAwait(false);
                    this.cachedFavoriteIDs[2] = artists.AllIDs();
                }
            }

            var playlists = new QobuzMyPlaylists(ServiceManager.MyPlaylist);
            this.collectionsForPlaylists.Add(playlists.Title, playlists);

            this.SaveStatus();

            UserSetting.Setting.Save();
        }


        protected override void PrepareSearchResult()
        {
            var tracks = new QobuzSearchResultForTracks(Search);
            this.collectionsForTracks.Add(collectionsForTracks.Count >= 5 ? MagnifyingGlass : Search, tracks);

            var albums = new QobuzSearchResultForAlbums(Search);
            this.collectionsForAlbums.Add(collectionsForAlbums.Count >= 5 ? MagnifyingGlass : Search, albums);

            var artists = new QobuzSearchResultForArtists(Search);
            this.collectionsForArtists.Add(collectionsForArtists.Count >= 5 ? MagnifyingGlass : Search, artists);

            var playlists = new QobuzSearchResultForPlaylists(Search);
            this.collectionsForPlaylists.Add(collectionsForPlaylists.Count >= 5 ? MagnifyingGlass : Search, playlists);
        }


        internal override bool IsUserUpdatable(IStreamingPlaylist playlist)
        {
            var tidalPlaylist = playlist as QobuzPlaylist;

            if (tidalPlaylist != null)
                return tidalPlaylist.creatorID == this.userID;

            return false;
        }

        #region Playlist save/update/delete
        public override async Task<IStreamingPlaylist> CreatePlaylistAsync(String title, IList<IStreamingTrack> tracks, string description = "")
        {
            title = title.Replace("\"", "'");
            description = description.Replace("\"", "'");


            String ids = tracks.SeperatedItemIDsBySeparator();

            String url = URLFor($"playlist/create", $"name={title.URLEncodedString()}&description={description.URLEncodedString()}&track_ids={ids}");

            using (var t = await GetResponseByGetDataAsync(url, null))
            {
                if (t.IsSuccessStatusCode)
                {
                    JToken token = JToken.Parse(await t.Content.ReadAsStringAsync().ConfigureAwait(false));
                    if (token != null)
                    {
                        var playlist = new QobuzPlaylist(token);

                        return playlist;
                    }
                }
            }

            return null;
        }

        public override async Task<bool> DeletePlaylistAsync(IStreamingPlaylist playlist)
        {
            if (playlist.ServiceType != this.ServiceType)
                return false;

            String url = URLFor($"playlist/delete", $"playlist_id={playlist.StreamingID}");

            using (var t = await GetResponseByGetDataAsync(url, null).ConfigureAwait(false))
            {

                return t.IsSuccessStatusCode;
            }
        }

        public override async Task<bool> UpdatePlaylistAsync(IStreamingPlaylist playlist, String newTitle, IList<IStreamingTrack> tracks, string description = "")
        {
            QobuzPlaylist userPlaylist = playlist as QobuzPlaylist;


            if (playlist.ServiceType != this.ServiceType || userPlaylist == null)
            {
                this.Info("Qobuz", $"Playlist to update is not what we expected : {playlist}");
                return false;
            }

            newTitle = newTitle.Replace("\"", "'");
            description = description.Replace("\"", "'");


            String ids = tracks.SeperatedItemIDsBySeparator();
            String url;

            if (!playlist.Name.Equals(newTitle) && !playlist.Description.Equals(description))
                url = URLFor($"playlist/update", $"playlist_id={playlist.StreamingID}&name={newTitle.URLEncodedString()}&description={description.URLEncodedString()}&track_ids={ids}");
            else if (!playlist.Name.Equals(newTitle))
                url = URLFor($"playlist/update", $"playlist_id={playlist.StreamingID}&name={newTitle.URLEncodedString()}&&track_ids={ids}");
            else if (!playlist.Name.Equals(newTitle))
                url = URLFor($"playlist/update", $"playlist_id={playlist.StreamingID}&description={description.URLEncodedString()}&track_ids={ids}");
            else
                url = URLFor($"playlist/update", $"playlist_id={playlist.StreamingID}&track_ids={ids}");


            using (var t = await GetResponseByGetDataAsync(url, null))
            {
                if (t.IsSuccessStatusCode)
                {
                    userPlaylist.UpdateTitleAndDescriptionAndResetSongs(newTitle, description);
                    return true;
                }
                else
                {
                    this.EP($"{ServiceType}", $"Failed to update playlist : {playlist}");
                    return false;
                }
            }
        }

        #endregion
        protected override void PrepareCachedTracks()
        {
            this.cachedTracks = config.GetCachedTracks<QobuzTrack>();
        }


        internal override async Task<HttpResponseMessage> GetResponseByPostDataAsync(string url, List<KeyValuePair<String, String>> postDat = null)
        {
            if (postDat != null) 
            return await WebUtil.GetResponseByPostDataAsync(url, postDat, cookies: this.cookie,extraHeader: this.extraHeader); 

            return await WebUtil.GetResponseByPostDataAsync(url, new List<KeyValuePair<String, String>>(), cookies: this.cookie, extraHeader: this.extraHeader); 
        }
        
        internal override async Task<HttpResponseMessage> GetResponseByGetDataAsync(string url, List<KeyValuePair<String, String>> postDat = null)
        {
            if (postDat != null)
            return await WebUtil.GetResponseAsync(url, postDat, cookies: this.cookie,extraHeader: this.extraHeader); 

            return await WebUtil.GetResponseAsync(url,  new List<KeyValuePair<String, String>>(), cookies: this.cookie,extraHeader: this.extraHeader); 
        }


    }
}
