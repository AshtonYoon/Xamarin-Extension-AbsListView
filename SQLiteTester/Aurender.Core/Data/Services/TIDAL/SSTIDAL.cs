using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Aurender.Core.Contents;
using Aurender.Core.Contents.Streaming;
using Aurender.Core.Setting;
using Aurender.Core.Utility;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Aurender.Core.Data.Services.TIDAL
{

    internal class SSTIDAL : StreamingServiceBase
    {
        internal SSTIDAL() : base(ContentType.TIDAL, "TIDAL", "http://tidal.com", "http://tidal.com/try-now", "wimpUser")
        {
            this.Capability = new HashSet<StreamingServiceFeatures>()
            {
                StreamingServiceFeatures.AlbumDetail,

                StreamingServiceFeatures.SimilarArtist,
                StreamingServiceFeatures.RelatedArtistsForArtist,
                StreamingServiceFeatures.ArtistTopTracks,

                StreamingServiceFeatures.FavoriteAlbum,
                StreamingServiceFeatures.FavoriteArtist,
                StreamingServiceFeatures.FavoritePlaylist,
                StreamingServiceFeatures.FavoriteTrack,

                StreamingServiceFeatures.Moods,

                StreamingServiceFeatures.PlaylistCreateion,
                StreamingServiceFeatures.PlaylistSearch,
            };
            ProtectedLogout();
            PrepareFavorites();
        }

        private const String tidalToken = "bS3_s0ZgdlFjSCT4";
        private String userID = String.Empty;
        private String sessionID = String.Empty;
        private String countryCode = String.Empty;
        private Dictionary<String, Object> profileInfo;
        private String defaultParam;

        // TODO: set AvailableStreamingQuality 
        public override IList<string> AvailableStreamingQuality => supportedStreamingQuality;

        static readonly IList<string> supportedStreamingQuality = new[] { "Normal", "High", "HiFi"/*, "HD"*/ };
        public override IList<string> SupportedStreamingQuality => supportedStreamingQuality;

        protected override void ProtectedLogout()
        {
            this.defaultParam = $"token={tidalToken}&countryCode=US";
        }

        public override async Task<Tuple<bool, string>> TryLoginAsync(IDictionary<StreamingServiceLoginInformation, string> information)
        {
            String url = $"https://api.tidalhifi.com/v1/login/username";

            if (information != null 
                && information.ContainsKey(StreamingServiceLoginInformation.UserID) 
                && information.ContainsKey(StreamingServiceLoginInformation.UserPassword))
            {

                List<KeyValuePair<String, String>> postData = new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("username", information[StreamingServiceLoginInformation.UserID]),
                    new KeyValuePair<string, string>("password", information[StreamingServiceLoginInformation.UserPassword]),
                    new KeyValuePair<string, string>("token", tidalToken)
                };

                var t = await WebUtil.PostDataAndDownloadContentsAsync(url, postData).ConfigureAwait(false);

                bool sucess = t.Item1;

                if (sucess)
                {
                    var sInfo = JToken.Parse(t.Item2);

                    this.userID = sInfo["userId"].ToString();
                    this.sessionID = sInfo["sessionId"].ToString();
                    this.countryCode = sInfo["countryCode"].ToString();

                    this.defaultParam = $"token={tidalToken}&sessionId={sessionID}&countryCode={countryCode}";

                    sucess = await LoadProfileAsync(information);

                    if (sucess)
                    {
                        ServiceInformation = information;
                        await LoadCatalog();
                        await LoadFavorites();
                        PrepareSearchResult();

                        NotifyServiceLogin(true);
                    }

                    return new Tuple<bool, string>(sucess, LogInMessage);
                }
                else
                {
                    this.userID = this.sessionID = this.countryCode = String.Empty;
                    this.loginMessages.Add("Login failed");
                    return new Tuple<bool, string>(sucess, "Login failed");
                }
            }
            else
            {
                return new Tuple<bool, string>(false, "No user information.");
            }
        }

        private async Task<bool> LoadProfileAsync(IDictionary<StreamingServiceLoginInformation, string> information)
        {
            String url = $"https://api.tidalhifi.com/v1/users/{userID}/subscription?{defaultParam}";

            Tuple<bool, string> t = await WebUtil.DownloadContentsAsync(url);

            if (t.Item1)
            {
                Dictionary<String, Object> sInfo =  Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(t.Item2);
                this.profileInfo = sInfo;

                Object obj = this.profileInfo["status"];

                if (obj != null)
                {
                    String status = obj.ToString();

                    if (status == "ACTIVE")
                    {
                        obj = this.profileInfo["subscription"];
                        if (obj != null)
                        {
                            JObject json = obj as JObject;

                            if (json != null && json.First != null)
                            {
                                String serviceType = json["type"].Value<String>();

                                information.Add(StreamingServiceLoginInformation.SubscriptionInfo, serviceType);

                                return true;
                            }
                            else
                                this.loginMessages.Add("Failed to get Subscription detail.");
                        }
                        else
                            this.loginMessages.Add("No Subscription is active.");
                    }
                    else
                        this.loginMessages.Add("Subscription is not active");
                    
                    return false;
                }
                else
                    this.loginMessages.Add("Failed to get subscription");
            }
            else
            {
                this.loginMessages.Add("Failed to get profile");
                this.EP("TIDAL-login", $"Failed to get profile : {t.Item2}");
            }

            this.profileInfo = null;
            return false;
        }

        internal override String URLFor(String partialPath, String paramString = "")
        {
            String str;
            if (paramString.Length > 0 )
                str = $"https://api.tidal.com/v1/{partialPath}?{defaultParam}&{paramString}";
            else 
                str = $"https://api.tidal.com/v1/{partialPath}?{defaultParam}";

            return str;
        }
        internal override String URLForWithoutDefaultParam(String partialPath)
        {
            String str = $"https://api.tidal.com/v1/{partialPath}";
            return str;
        }


        public override async Task<IStreamingAlbum> GetAlbumWithIDAsync(string albumID)
        {
            String url = URLFor($"albums/{albumID}");

            using (var t = await WebUtil.GetResponseAsync(url).ConfigureAwait(false))
            {

                IStreamingAlbum artist = null;

                if (t.IsSuccessStatusCode)
                {
                    var str = await t.Content.ReadAsStringAsync();

                    JToken jtoken = JToken.Parse(str);

                    artist = new TIDALAlbum(jtoken);
                }

                return artist;
            }
        }

        public override async Task<IStreamingArtist> GetArtistWithIDAsync(string artistmID)
        {
                String url = URLFor($"artists/{artistmID}");

            using (var t = await WebUtil.GetResponseAsync(url).ConfigureAwait(false))
            {
                IStreamingArtist artist = null;
                if (t.IsSuccessStatusCode)
                {
                    var str = await t.Content.ReadAsStringAsync();

                    JToken jtoken = JToken.Parse(str);

                    artist = new TIDALArtist(jtoken);
                }

                return artist;
            }
        }

        public override async Task<IStreamingPlaylist> GetPlaylistWithIDAsync(string playlistID)
        {
            String url = URLFor($"playlists/{playlistID}");

            using (var t = await WebUtil.GetResponseAsync(url).ConfigureAwait(false))
            {

                IStreamingPlaylist playlist = null;
                if (t.IsSuccessStatusCode)
                {
                    var str = await t.Content.ReadAsStringAsync();

                    JToken jtoken = JToken.Parse(str);

                    playlist = new TIDALPlaylist(jtoken);
                }

                return playlist;
            }
        }

        protected override async Task<IStreamingTrack> TrackWithIDAsync(string trackID)
        {
            String url = URLFor($"tracks/{trackID}");

            using (var t = await WebUtil.GetResponseAsync(url).ConfigureAwait(false))
            {

                if (t.IsSuccessStatusCode)
                {
                    var json = await t.Content.ReadAsStringAsync().ConfigureAwait(false);

                    Object obj = JsonConvert.DeserializeObject(json);

                    if (obj is JToken)
                    {
                        TIDALTrack track = new TIDALTrack(obj as JToken);

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

            String itemField;
            String itemType;
            String remove = favorite ? "" : item.StreamingID;


            switch (item.StreamingItemType)
            {
                case StreamingItemType.Track:
                    itemField = "trackId";
                    itemType = "tracks";
                    break;

                case StreamingItemType.Album:
                    itemField = "albumId";
                    itemType = "albums";
                    break;
                case StreamingItemType.Aritst:
                    itemField = "artistId";
                    itemType = "artists";
                    break;

                case StreamingItemType.Playlist:
                    itemField = "uuids";
                    itemType = "playlists";
                    break;


                case StreamingItemType.Genre:
                default:
                    itemField = "";
                    itemType = "";
                    Debug.Assert(false, $"Shouldn't be reached here : TIDAL::UpdateFavorite(IStreamingFavoritable, bool)");
                    break;
            }

            String url = URLFor($"users/{userID}/favorites/{itemType}/{remove}");

            Tuple<bool, String> t;

            if (favorite)
            {
                List<KeyValuePair<string, string>> postData = new List<KeyValuePair<string, string>>()
                {
                    new KeyValuePair<string, string>("userId", this.userID),
                    new KeyValuePair<string, string>(itemField, item.StreamingID)
                };

                t = await WebUtil.PostDataAndDownloadContentsAsync(url, postData).ConfigureAwait(false);
            }
            else
                t = await WebUtil.DeleteDataAndDownloadContentsAsync(url).ConfigureAwait(false);

            bool sucess = t.Item1;

            if (sucess)
            {
                if (t.Item2 != null && t.Item2.Length > 0)
                    NotifyFavoriteUpdateFailed(item);
                else
                {
                    UpdateFavoriteChached(item, favorite);                    
                    NotifyFavoriteUpdated(item, favorite);
                }
            }
            else
                NotifyFavoriteUpdateFailed(item);
        }

        protected override async Task LoadCatalog()
        {
            /// Load Tracks
            /// Load Albums
            /// Load Artists
            /// Load Genres/Moods
            /// Load Playlists
            String url = URLFor("featured");

            using (var response = await WebUtil.GetResponseAsync(url).ConfigureAwait(false))
            {
                if (response == null) return;

                if (response.IsSuccessStatusCode)
                {
                    var jsonString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                    var sInfo = JArray.Parse(jsonString);            //TIDALFeaturedTracks tracks = new TIDALFeaturedTracks("New", "featured/new/tracks");
                    foreach (JToken token in sInfo)
                    {
                        if (token != null)
                        {
                            String title = token["name"].ToObject<String>();
                            String path = token["path"].ToObject<String>();

                            if (token["hasPlaylists"].ToObject<bool>())
                            {
                                var playlists = new TIDALFeaturedPlaylists(title, $"featured/{path}/playlists");
                                this.collectionsForPlaylists.Add(title, playlists);
                            }

                            if (token["hasAlbums"].ToObject<bool>())
                            {
                                var albums = new TIDALFeaturedAlbums(title, $"featured/{path}/albums");
                                this.collectionsForAlbums.Add(title, albums);
                            }

                            if (token["hasTracks"].ToObject<bool>())
                            {
                                var tracks = new TIDALFeaturedTracks(title, $"featured/{path}/tracks");
                                this.collectionsForTracks.Add(title, tracks);
                            }

                            if (token["hasArtists"].ToObject<bool>())
                            {
                                var artists = new TIDALFeaturedArtists(title, $"featured/{path}/artists");
                                this.collectionsForArtists.Add(title, artists);
                            }
                        }

                        if(token != null && sInfo.IndexOf(token) == 0)
                        {
                            var albums2 = new TIDALFeaturedAlbums("Masters", "master/recommended/albums");
                            this.collectionsForAlbums.Add(albums2.Title, albums2);

                            albums2 = new TIDALFeaturedAlbums("Discovery", "discovery/new/albums");
                            this.collectionsForAlbums.Add(albums2.Title, albums2);

                            albums2 = new TIDALFeaturedAlbums("Rising", "rising/new/albums");
                            this.collectionsForAlbums.Add(albums2.Title, albums2);
                        }
                    }
                }
            }

            IStreamingObjectCollection<IStreamingTrack> tracks2 = new TIDALFeaturedTracks("Rising", "rising/new/tracks");
            this.collectionsForTracks.Add(tracks2.Title, tracks2);
            
            IStreamingObjectCollection<IStreamingPlaylist> playlist2 = new TIDALFeaturedPlaylists("Masters", "master/recommended/playlists");
            this.collectionsForPlaylists.Add(playlist2.Title, playlist2);

            tracks2 = new TIDALFeaturedTracks("Discovery", "discovery/new/tracks");
            this.collectionsForTracks.Add(tracks2.Title, tracks2);
            
            var genres = new TIDALFeaturedGenres("Genres", "genres");
            this.collectionsForGenres.Add(genres.Title, genres);

            genres = new TIDALFeaturedMoods("Moods", "moods");
            this.collectionsForGenres.Add(genres.Title, genres);
        }


        protected override async Task LoadFavorites()
        {
            var config = UserSetting.Setting[ServiceType];
            var favorite = ServiceManager.Favorite;
            var star = ServiceManager.Star;
            var myPlaylist = ServiceManager.MyPlaylist;

            var order = config.Get(FieldsForServiceCache.OrderForFavoriteTracks, Ordering.AddedDateDesc);

            var tracks = new TIDALFavoriteTracks(favorite, userID, order);
            this.collectionsForTracks.Add(collectionsForTracks.Count >= 5 ? star : favorite, tracks);
            await tracks.LoadNextAsync().ConfigureAwait(false);

            order = config.Get(FieldsForServiceCache.OrderForFavoriteAlbums, Ordering.AddedDateDesc);
            var albums = new TIDALFavoriteAlbums(favorite, userID, order); 
            this.collectionsForAlbums.Add(collectionsForAlbums.Count >= 5 ? star : favorite, albums);

            order = config.Get(FieldsForServiceCache.OrderForFavoriteArtists, Ordering.AddedDateDesc);
            var artists = new TIDALFavoriteArtists(favorite, userID, order);
            this.collectionsForArtists.Add(collectionsForArtists.Count >= 5 ? star : favorite, artists);

            order = config.Get(FieldsForServiceCache.OrderForFavoritePlaylists, Ordering.AddedDateDesc);
            StreamingCollectionBase<IStreamingPlaylist> playlists = new TIDALFavoritePlaylists(favorite, userID, order);
            this.collectionsForPlaylists.Add(collectionsForPlaylists.Count >= 5 ? star : favorite, playlists);

            await prepareFavoriteCachedIDs().ConfigureAwait(false);
        }

        readonly string[] tags = { "TRACK", "ALBUM", "ARTIST", "PLAYLIST" };
        private async Task prepareFavoriteCachedIDs()
        {
            String url = URLFor($"users/{userID}/favorites/ids");

            String etag = config.Get<String>(FieldsForServiceCache.ETagsForFavoriteIDs, "");

            using (var response = await WebUtil.GetResponseAsync(url, etag: etag))
            {
                if (response.IsSuccessStatusCode)
                {
                    String responseEtag = response.ETag();

                    if (etag != responseEtag || etag == "")
                    {
                        var jsonString = await response.Content.ReadAsStringAsync();

                        JObject result = JObject.Parse(jsonString);
                        if (result != null)
                        {
                            for (int i = 0; i < tags.Length; i++)
                            {
                                var items = result[tags[i]] as JArray;
                                var ids = new HashSet<string>(items.Select(x => x.ToString()));

                                this.cachedFavoriteIDs[i] = ids;
                                this.LP($"{ServiceType}", $"Loaded cached {tags[i]} : {ids.Count:n0}");
                            }
                        }
                        this.LP($"{ServiceType}", $"Loaded cached tags");

                        config[FieldsForServiceCache.ETagsForFavoriteIDs] = responseEtag;
                        this.SaveStatus();
                        UserSetting.Setting.Save();
                    }
                    else
                        this.LP($"{ServiceType}", $"ETag matched so returned empty");
                }
                else
                    this.EP($"{ServiceType}", $"Failed to get cached IDs : {response}, {response?.StatusCode.ToString() ?? "null"} ");
            }
        }

        protected override void PrepareSearchResult()
        {
            var tracks = new TIDALSearchResultForTracks(Search);
            this.collectionsForTracks.Add(collectionsForTracks.Count >= 5 ? MagnifyingGlass : Search, tracks);

            var albums = new TIDALSearchResultForAlbums(Search);
            this.collectionsForAlbums.Add(collectionsForAlbums.Count >= 5 ? MagnifyingGlass : Search, albums);

            var artists = new TIDALSearchResultForArtists(Search);
            this.collectionsForArtists.Add(collectionsForArtists.Count >= 5 ? MagnifyingGlass : Search, artists);

            var playlists = new TIDALSearchResultForPlaylists(Search);
            this.collectionsForPlaylists.Add(collectionsForPlaylists.Count >= 5 ? MagnifyingGlass : Search, playlists);
        }
 

        internal override bool IsUserUpdatable(IStreamingPlaylist playlist)
        {
            var tidalPlaylist = playlist as TIDALPlaylist;

            if (tidalPlaylist != null)
                return tidalPlaylist.creatorID == this.userID;

            return false;
        }

        #region Playlist save/update/delete
        public override async Task<IStreamingPlaylist> CreatePlaylistAsync(String title, IList<IStreamingTrack> tracks, string description = "")
        {
            String url = URLFor($"users/{userID}/playlists");

            var postData = new List<KeyValuePair<String, String>>()
            {
                new KeyValuePair<String, String>("userId", userID),
                new KeyValuePair<String, String>("title", title.URLEncodedString()),
            };

            if (description.Length > 0)
                postData.Add(new KeyValuePair<String, String>("description", description?.Replace("\"", "'").URLEncodedString() ?? ""));

            using (var t = await WebUtil.GetResponseByPostDataAsync(url, postData))
            {
                if (t.IsSuccessStatusCode)
                {
                    JToken token = JToken.Parse(await t.Content.ReadAsStringAsync().ConfigureAwait(false));
                    if (token != null)
                    {
                        var eTag = t.ETag();

                        var playlist = new TIDALPlaylistForUserCreated(token, eTag);

                        var crated = await this.UpdatePlaylistAsync(playlist, title, tracks, description);

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

            String url = URLFor($"playlists/{playlist.StreamingID}");

            var t = await WebUtil.DeleteDataAndDownloadContentsAsync(url).ConfigureAwait(false);


            return t.Item1;
        }

        public override async Task<bool> UpdatePlaylistAsync(IStreamingPlaylist playlist, String newTitle, IList<IStreamingTrack> tracks, string description = "")
        {
            if (playlist.ServiceType != this.ServiceType)
            {
                return false;
            }

            String url = URLFor($"playlists/{playlist.StreamingID}");

            var userPlaylist = playlist as TIDALPlaylistForUserCreated;
            String eTag;

            if (userPlaylist != null)
            {
                eTag = userPlaylist.etag;
            }
            else
            {
                using (var t = await WebUtil.GetResponseAsync(url))
                {

                    if (t.IsSuccessStatusCode)
                    {
                        eTag = t.ETag();
                    }
                    else
                        eTag = String.Empty;
                }
            }

            if (playlist.SongCount > 0)
            {
                ///delete
                var range = Enumerable.Range(1, playlist.SongCount - 1);

                StringBuilder idsToDelete = new StringBuilder("0");
                range.All(id =>
                {
                    idsToDelete.Append($",{id}");
                    return true;
                });

                url = URLFor($"playlists/{playlist.StreamingID}/items/{idsToDelete.ToString()}");

                using (var deleteT = await WebUtil.GetResponseByDeleteAsync(url, eTag).ConfigureAwait(false))
                {
                    this.LP($"{ServiceType}", $"Result after delete playlist {deleteT.Content.ReadAsStringAsync()}");

                    eTag = deleteT.ETag();
                }
            }

            String ids = tracks.SeperatedItemIDsBySeparator();
            this.LP($"{ServiceType}", $"Now trying to add {tracks.Count:n0} tracks to {playlist}");

            newTitle = newTitle.Replace("\"", "'");
            description = description.Replace("\"", "'");

            var postData = new List<KeyValuePair<String, String>>()
                {
                    new KeyValuePair<string, string>("uuid", playlist.StreamingID),
                    new KeyValuePair<string, string>("itemIds", ids.ToString()),
                    new KeyValuePair<string, string>("toIndex", "0"),
                    new KeyValuePair<string, string>("title", newTitle),
                };

            if (!newTitle.Equals(playlist.Name))
            {
                //postData.Add(new KeyValuePair<string, string>("title", newTitle));
            }
            if (!description.Equals(playlist.Description))
            {
                postData.Add(new KeyValuePair<string, string>("description", description));
            }

            url = URLFor($"playlists/{playlist.StreamingID}/tracks");

            using (var u = await WebUtil.GetResponseByPostDataAsync(url, postData, eTag))
            {

                if (u.IsSuccessStatusCode)
                {
                    String responseString = await u.Content.ReadAsStringAsync().ConfigureAwait(false);
                    this.LP($"{ServiceType}", $"Update playlist response : {responseString}");

                    if (userPlaylist != null)
                        userPlaylist.etag = u.ETag();

                    return responseString.Length == 0;
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
            this.cachedTracks = config.GetCachedTracks<TIDALTrack>();
        }

    }

}