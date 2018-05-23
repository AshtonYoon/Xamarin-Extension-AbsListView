using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Aurender.Core.Contents.Streaming;
using Aurender.Core.Setting;
using Aurender.Core.Utility;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Aurender.Core.Data.Services.Bugs
{

    enum BugsFieldType
    {
        Track = 1,
        Album = 2,
        Artist = 3,
        Playlist = 4
    }

    internal class SSBugs : StreamingServiceBase
    {
        public const int FavoriteBucketSize = 1000;
        internal SSBugs() : base(ContentType.Bugs, "벅스뮤직", "http://www.bugs.co.kr", "http://www.bugs.co.kr/try-now", "bugsUser")
        {
            this.Capability = new HashSet<StreamingServiceFeatures>()
            {
                StreamingServiceFeatures.AlbumDetail,

                StreamingServiceFeatures.ArtistTopTracks,

                StreamingServiceFeatures.FavoriteAlbum,
                StreamingServiceFeatures.FavoriteArtist,
                StreamingServiceFeatures.FavoriteTrack,

                StreamingServiceFeatures.PlaylistCreateion,
                StreamingServiceFeatures.PlaylistSearch,
            };
            cookie = new CookieContainer();

            ProtectedLogout();
            PrepareFavorites();
        }

        private CookieContainer cookie;
        private Dictionary<String, IStreamingObjectCollection<IStreamingAlbum>> collectionsForAlbumsForGenre = new Dictionary<string, IStreamingObjectCollection<IStreamingAlbum>>();

        private const String bugsToken = "d599f56c5b28481189c06ff83777d5f0";
        private String userID = String.Empty;
        private String subscriptionName = String.Empty;
        private String subscriptionExpire = String.Empty;


        private bool isAdult = false;
        private bool isPremiumUser = false;
        //private Dictionary<String, Object> profileInfo;
        private String defaultParam;

        internal override List<String> TitlesForGenres => this.collectionsForAlbumsForGenre?.Keys.ToList<String>() ?? new List<String>();

        // TODO: set AvailableStreamingQuality 
        public override IList<string> AvailableStreamingQuality => supportedStreamingQuality;

        static readonly IList<string> supportedStreamingQuality = new[] { "AAC+ 96kbps", "MP3 192kbps", "MP3 320kbps", "FLAC 16bit/44.1khz" };
        public override IList<string> SupportedStreamingQuality => supportedStreamingQuality;

        internal Task<HttpResponseMessage> GetResponseByPostDataAsync(string url, ICollection<KeyValuePair<String, String>> postData)
        {
            return WebUtil.GetResponseByPostDataAsync(url, postData, cookies: this.cookie); 
        }
        internal Task<HttpResponseMessage> GetResponseByGetDataAsync(string url, ICollection<KeyValuePair<String, String>> postData)
        {
            return WebUtil.GetResponseAsync(url, postData, cookies: this.cookie); 
        }


        protected override void ProtectedLogout()
        {
            this.collectionsForAlbumsForGenre.Clear();

            this.defaultParam = $"api_key={bugsToken}";
        }

        public override async Task<Tuple<bool, string>> TryLoginAsync(IDictionary<StreamingServiceLoginInformation, string> information)
        {
            if (information != null
                && information.ContainsKey(StreamingServiceLoginInformation.UserID)
                && information.ContainsKey(StreamingServiceLoginInformation.UserPassword))
            {
                string id = information[StreamingServiceLoginInformation.UserID];
                string pw = information[StreamingServiceLoginInformation.UserPassword].URLEncodedString();
                string device = ServiceManager.CurrentMAC.URLEncodedString();
                string model = $"{ServiceManager.OS}Aurender";
                var postData = new Dictionary<string, string>
                {
                    { "api_key", bugsToken },
                    { "userid", id },
                    { "passwd", pw },
                    { "device_id", device },
                    { "device_model", model }
                };

                String url = $"https://secure.bugs.co.kr/openapi/1/login";

                this.isAdult = false;
                this.isPremiumUser = false;
                this.subscriptionName = string.Empty;
                this.subscriptionExpire = string.Empty;

                using (var response = await GetResponseByPostDataAsync(url, postData).ConfigureAwait(false))
                {
                    if (response.IsSuccessStatusCode)
                    {
                        bool sucess = false;

                        String responseString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                        var sInfo = JToken.Parse(responseString);

                        //TODO: 왜 죽지
                        if (sInfo["result"] is JToken result)
                        {
                            this.userID = result["msrl"].ToString();
                            var right = result["right"] as JToken;
                            if (right != null)
                            {
                                var protect = right["protect"];
                                if (protect?.Type != JTokenType.Null)
                                    this.isAdult = protect["adult"]?.ToObject<bool>() ?? false;

                                var product = right["product"];
                                if (product?.Type != JTokenType.Null)
                                {
                                    this.subscriptionExpire = product["date_expire"].ToString();
                                    long.TryParse(subscriptionExpire, out long expire);
                                    expire /= 1000;
                                    TimeSpan t = DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1));

                                    long timestamp = (long)t.TotalSeconds;

                                    long day = (expire - timestamp) / 86400;
                                    if (day > 0)
                                    {
                                        this.subscriptionName = product["name"].ToString() + $"\n남은기간: {day:n0}일";
                                    }
                                    else
                                    {
                                        this.subscriptionName = product["name"].ToString() + "\n기간이 만료되었습니다.";
                                    }
                                }
                                else
                                {
                                    this.subscriptionName = "사용중인 이용권이 없습니다.\n이용권은 http://www.bugs.co.kr 에서 구매 가능합니다.";
                                }

                                information.Add(StreamingServiceLoginInformation.SubscriptionInfo, subscriptionName);

                                var stream = right["stream"] as JToken;
                                if (stream?.Type != JTokenType.Null)
                                {
                                    if (stream["is_flac_premium"] is JToken premium)
                                        this.isPremiumUser = premium.ToObject<bool>();
                                }
                            }

                            sucess = true;
                        }

                        this.defaultParam = $"api_key={bugsToken}";

                        //bool sucess = LoadProfile();

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
                        this.userID = this.subscriptionName = String.Empty;
                        this.isAdult = false;
                        this.loginMessages.Add("Login failed");
                        return new Tuple<bool, string>(false, "Login failed");
                    }
                }
            }
            else
            {
                return new Tuple<bool, string>(false, "No user information.");
            }
        }


        internal override String URLFor(String partialPath, String paramString = "")
        {
            String str;
            if (paramString.Length > 0)
                str = $"http://openapi.bugs.co.kr/1/{partialPath}?{defaultParam}&{paramString}";
            else
                str = $"http://openapi.bugs.co.kr/1/{partialPath}?{defaultParam}";

            return str;
        }
        internal override String URLForWithoutDefaultParam(String partialPath)
        {
            String str = $"http://secure.bugs.co.kr/1/{partialPath}";
            return str;
        }

        public override IList<String> TitlesForCollectionForGenre()
        {
            return this.collectionsForAlbumsForGenre.Keys.ToList<String>();
        }

        public override IStreamingObjectCollection<U> CollectionForGenreTitle<U>(String title)
        {
            if (typeof(U) == typeof(IStreamingAlbum) && collectionsForAlbumsForGenre.TryGetValue(title, out var collection))
            {
                return collection as IStreamingObjectCollection<U>;
            }

            return null;
        }

        public override async Task<IStreamingAlbum> GetAlbumWithIDAsync(string albumID)
        {
            String url = URLFor($"albums/{albumID}");


            var t = await GetResponseByGetDataAsync(url, null).ConfigureAwait(false);

            IStreamingAlbum artist = null;

            if (t.IsSuccessStatusCode)
            {
                var str = await t.Content.ReadAsStringAsync();

                JToken jtoken = JToken.Parse(str);
                var result = BugsUtilty.IsRequestSucess(jtoken);
                if (result.Item1)
                    artist = new BugsAlbum(jtoken["result"]);
                else
                    this.EP("Bugs", $"Failed to get album : {albumID}");
            }

            return artist;
        }

        public override async Task<IStreamingArtist> GetArtistWithIDAsync(string artistmID)
        {
            String url = URLFor($"artists/{artistmID}");

            using (var t = await GetResponseByGetDataAsync(url, null).ConfigureAwait(false))
            {

                IStreamingArtist artist = null;
                if (t.IsSuccessStatusCode)
                {
                    var str = await t.Content.ReadAsStringAsync();

                    JToken jtoken = JToken.Parse(str);
                    var result = BugsUtilty.IsRequestSucess(jtoken);
                    if (result.Item1)
                        artist = new BugsArtist(jtoken["result"]);
                    else
                        this.EP("Bugs", $"Failed to get artist : {artistmID}");
                }

                return artist;
            }
        }

        public override Task<IStreamingPlaylist> GetPlaylistWithIDAsync(string playlistID)
        {
            this.EP($"{ServiceType}", "Bugs doesn't support direct playlist loading");
            return Task.FromResult(default(IStreamingPlaylist));
        }

        protected override async Task<IStreamingTrack> TrackWithIDAsync(string trackID)
        {
            String url = URLFor($"tracks/{trackID}");

            using (var t = await GetResponseByGetDataAsync(url, null).ConfigureAwait(false))
            {

                if (t.IsSuccessStatusCode)
                {
                    var json = await t.Content.ReadAsStringAsync().ConfigureAwait(false);

                    Object obj = JsonConvert.DeserializeObject(json);

                    if (obj is JToken)
                    {
                        JToken token = obj as JToken;
                        var result = BugsUtilty.IsRequestSucess(token);
                        if (result.Item1)
                        {
                            var track = new BugsTrack(token["result"]);

                            return track;
                        }
                        else
                            this.EP("Bugs", $"Failed to get track {trackID} : {result.Item2}");
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
            IARLogStatic.Error($"{ServiceType}", $"Doesn't support this");
            return null;
        }

        internal override async Task UpdateFavorite(IStreamingFavoritable item, bool favorite)
        {
            if (!Capability.Contains(item.StreamingItemType.FavoriteFeature()))
            {
                this.Info($"{ServiceType}", $"Does not support favorite for {item}");
                return;
            }

            String itemType;
            String likes = favorite ? "like" : "dislike";


            switch (item.StreamingItemType)
            {
                case StreamingItemType.Track:
                    itemType = "tracks";
                    break;

                case StreamingItemType.Album:
                    itemType = "albums";
                    break;
                case StreamingItemType.Aritst:
                    itemType = "artists";
                    break;

                case StreamingItemType.Playlist:
                    itemType = "esalbums";
                    break;


                case StreamingItemType.Genre:
                default:
                    itemType = "";
                    Debug.Assert(false, $"Shouldn't be reached here : TIDAL::UpdateFavorite(IStreamingFavoritable, bool)");
                    break;
            }

            String url = URLFor($"{itemType}/{item.StreamingID}/{likes}");

            //            Tuple<bool, String> t = await WebUtil.PostDataAndDownloadContentsAsync(url, new List<KeyValuePair<String, String>>()).ConfigureAwait(false);
            //            bool sucess = t.Item1;
            try
            {
                using (var t = await GetResponseByPostDataAsync(url, null).ConfigureAwait(false))
                {
                    String responseString = await t.Content.ReadAsStringAsync().ConfigureAwait(false);
                    JObject obj = JsonConvert.DeserializeObject<JObject>(responseString);

                    if (obj != null)
                    {
                        JToken token = obj as JToken;
                        if (token != null)
                        {
                            var result = BugsUtilty.IsRequestSucess(token);
                            if (result.Item1)
                            {
                                UpdateFavoriteChached(item, favorite);
                                NotifyFavoriteUpdated(item, favorite);
                                return;
                            }
                            else
                            {
                                this.EP("Bugs", $"Failed to update favorite with {result.Item2} for {item}");
                                NotifyFavoriteUpdateFailed(item);
                            }
                        }
                        else
                            this.EP("Bugs", $"Failed to update favorite {item}, \n{responseString}");
                    }
                    else
                        this.EP("Bugs", $"Failed to update favorite {item}");
                }
            }
            catch(Exception ex)
            {
                this.EP("Bugs", $"Faield to update {item}", ex);
            }
        }

        void AddSections<Y>(IDictionary<String,IStreamingObjectCollection<Y>> collection, Dictionary<String, String> sectionInfo, Type c )
            where Y : IStreamingServiceObject
        {

            foreach (var section in sectionInfo)
            {
                var tracks = (IStreamingObjectCollection<Y>) Activator.CreateInstance(c, section.Key, section.Value);
                collection.Add(section.Key, tracks);
              //  var t = tracks.LoadNextAsync();
               // t.Wait();
            }
            sectionInfo.Clear();
        }

        protected override async Task LoadCatalog()
        {
            /// Load Tracks
            var sections = new Dictionary<String, String>();
                        sections.Add("실시간 차트", "charts/track/realtime");
                        sections.Add("주간 차트", "charts/track/top1000");
                        sections.Add("일간 차트", "charts/track/daily");
                        sections.Add("최신 곡", "charts/track/total");

                        AddSections(this.collectionsForTracks, sections, typeof(BugsFeaturedTracks));


                        /// Load Albums
                        sections.Add("최신 앨범", "albums/new/total");
                        sections.Add("앨범 차트", "charts/album/daily");

                        AddSections(this.collectionsForAlbums, sections, typeof(BugsFeaturedAlbums));


                        /// Load Genres/Moods
                        sections.Add("가요", "kpop");
                        sections.Add("팝송", "pop");
                        sections.Add("OST", "ost");
                        sections.Add("클래식", "classic");
                        sections.Add("재즈", "jazz");
                        AddSections(this.collectionsForAlbumsForGenre, sections, typeof(BugsFeaturedGenres));


            /// Load Playlists
            //sections.Add("인기 뮤직PD 앨범", "musicpd/chart");
            //AddSections(this.collectionsForPlaylists, sections, typeof(BugsFeaturedPlaylist));

            var tracks = new BugsFeaturedPlaylist("인기 뮤직PD 앨범", "musicpd/chart");
            collectionsForPlaylists.Add(tracks.Title, tracks);
//            var t = tracks.LoadNextAsync();
            //t.Wait();


            sections.Add("최신 뮤직PD 앨범", "musicpd");
            //      AddSections(this.collectionsForPlaylists, sections, typeof(BugsFeaturedNewPlaylist));
            var tracks2 = new BugsFeaturedNewPlaylist("최신 뮤직PD 앨범", "musicpd");
            await tracks2.LoadNextAsync().ConfigureAwait(false);

            collectionsForPlaylists.Add(tracks2.Title, tracks2);
        }


        protected override async Task LoadFavorites()
        {
            var config = UserSetting.Setting[ServiceType];
            var favorite = ServiceManager.Favorite;
            var star = ServiceManager.Star;
            var myPlaylist = ServiceManager.MyPlaylist;

            var tracks = new BugsFavoriteTracks(favorite);
            this.collectionsForTracks.Add(collectionsForTracks.Count >= 5 ? star : favorite, tracks);
            await tracks.LoadNextAsync().ConfigureAwait(false);
            this.cachedFavoriteIDs[0] = tracks.AllIDs();

            var albums = new BugsFavoriteAlbums(favorite);
            this.collectionsForAlbums.Add(collectionsForAlbums.Count >= 5 ? star : favorite, albums);

            var artists = new BugsFavoriteArtists(favorite);
            this.collectionsForArtists.Add(collectionsForArtists.Count >= 5 ? star : favorite, artists);

            var playlists = new BugsMyPlaylists(myPlaylist);
            this.collectionsForPlaylists.Add(myPlaylist, playlists);

            //var favoritePlaylist = new BugsFavoritePlaylist(favorite);
            //this.collectionsForPlaylists.Add(favorite, favoritePlaylist);
        }

        protected override void PrepareSearchResult()
        {
            var tracks = new BugsSearchResultForTracks(Search);
            this.collectionsForTracks.Add(collectionsForTracks.Count >= 5 ? MagnifyingGlass : Search, tracks);

            var albums = new BugsSearchResultForAlbums(Search);
            this.collectionsForAlbums.Add(collectionsForAlbums.Count >= 5 ? MagnifyingGlass : Search, albums);
            
            var artists = new BugsSearchResultForArtists(Search);
            this.collectionsForArtists.Add(collectionsForArtists.Count >= 5 ? MagnifyingGlass : Search, artists);
            
            var playlists = new BugsSearchResultForPlaylist(Search);
            this.collectionsForPlaylists.Add(collectionsForPlaylists.Count >= 5 ? MagnifyingGlass : Search, playlists);
        }


        internal override bool IsUserUpdatable(IStreamingPlaylist playlist)
        {
            var tidalPlaylist = playlist as BugsPlaylist;

            if (tidalPlaylist != null)
                return tidalPlaylist.creatorID == this.userID;

            return false;
        }

        #region Playlist save/update/delete
        public override async Task<IStreamingPlaylist> CreatePlaylistAsync(String title, IList<IStreamingTrack> tracks, string description = "")
        {
            String url = URLFor($"myalbums/create");

            String ids = tracks.SeperatedItemIDsBySeparator('|');

            var postData = new List<KeyValuePair<String, String>>()
            {
                new KeyValuePair<String, String>("title", title.URLEncodedString()),
                new KeyValuePair<String, String>("open_yn", "N"),
                new KeyValuePair<String, String>("track_ids", ids),
            };

            using (var t = await GetResponseByPostDataAsync(url, postData).ConfigureAwait(false))
            {
                if (t.IsSuccessStatusCode)
                {
                    String resultString = await t.Content.ReadAsStringAsync().ConfigureAwait(false);

                    JToken token = JToken.Parse(resultString);
                    if (token != null)
                    {

                        var result = BugsUtilty.IsRequestSucess(token);
                        if (result.Item1)
                        {
                            this.reloadMyPlaylist();
                            String newPlaylistID = token["result"].ToString();

                            try
                            {
                                var playlist = this.collectionsForPlaylists[ServiceManager.MyPlaylist].First(kv => kv.StreamingID == newPlaylistID);
                                return playlist;
                            }
                            catch (InvalidOperationException ex)
                            {
                                this.EP($"{ServiceType}", $"Create playlist succeed bug can't find from my playlist : {resultString}", ex);
                            }
                        }
                        else
                        {
                            this.EP($"{ServiceType}", $"Create playlist failed and result : {result.Item2}");
                        }
                    }
                }
            }
            return null;
        }

        private void reloadMyPlaylist()
        {
            var list = this.collectionsForPlaylists[ServiceManager.MyPlaylist];
            if (list != null) { 
                list.Reset();
                list.LoadNextAsync().Wait(); 
            }
        }

        public override async Task<bool> DeletePlaylistAsync(IStreamingPlaylist playlist)
        {
            if (playlist.ServiceType != this.ServiceType)
                return false;

            String url = URLFor($"myalbums/{playlist.StreamingID}/delete");

            //var t = await WebUtil.PostDataAndDownloadContentsAsync(url, new List<KeyValuePair<String, String>>()).ConfigureAwait(false);
            using (var t = await GetResponseByPostDataAsync(url, null).ConfigureAwait(false))
            {
                String responseString = await t.Content.ReadAsStringAsync().ConfigureAwait(false);

                JToken token = JToken.Parse(responseString);
                if (token != null)
                {
                    var result = BugsUtilty.IsRequestSucess(token);
                    if (result.Item1)
                    {
                        reloadMyPlaylist();
                        return true;
                    }
                    else
                        this.EP($"{ServiceType}", $"Delete playlist failed and result : {result.Item2}");
                }
                else
                    this.EP($"{ServiceType}", $"Delete playlist failed and result : {responseString}");
            }
            return false;
        }

        public override async Task<bool> UpdatePlaylistAsync(IStreamingPlaylist playlist, String newTitle, IList<IStreamingTrack> tracks, string description = "")
        {
            if (playlist.ServiceType != this.ServiceType)
            {
                return false;
            }

            String url = URLFor($"myalbums/{playlist.StreamingID}/modify");

            var userPlaylist = playlist as BugsPlaylist;

            String ids = tracks.SeperatedItemIDsBySeparator('|');
            
            this.LP($"{ServiceType}", $"Now trying to add {tracks.Count:n0} tracks to {playlist}");

            newTitle = newTitle.Replace("\"", "'");
            description = description.Replace("\"", "'");

            var postData = new List<KeyValuePair<String, String>>()
            {
                new KeyValuePair<String, String>("title", newTitle.URLEncodedString()),
                new KeyValuePair<String, String>("open_yn", "N"),
                new KeyValuePair<String, String>("modify_tracks_yn", "Y"),
                new KeyValuePair<String, String>("track_ids", ids.ToString()),
            };

            if (description.Length > 0)
                postData.Add(new KeyValuePair<String, String>("description", description?.Replace("\"", "'").URLEncodedString() ?? ""));


            using (var u = await GetResponseByPostDataAsync(url, postData).ConfigureAwait(false))
            {
                if (u.IsSuccessStatusCode)
                {
                    String resultString = await u.Content.ReadAsStringAsync().ConfigureAwait(false);

                    JToken token = JToken.Parse(resultString);
                    if (token != null)
                    {
                        var result = BugsUtilty.IsRequestSucess(token);
                        if (result.Item1)
                            return true;
                        else
                            this.EP($"{ServiceType}", $"Failed to update playlist : {result.Item2}");
                    }
                    else
                        this.EP($"{ServiceType}", $"Failed to update playlist : {resultString}");

                }
                else
                    this.EP($"{ServiceType}", $"Failed to update playlist : {playlist}");
            }
            return false;
        }

        #endregion
        protected override void PrepareCachedTracks()
        {
            this.cachedTracks = config.GetCachedTracks<BugsTrack>();
        }
        
    }
}
