using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aurender.Core.Contents.Streaming;
using Aurender.Core.Player.mpd;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Net.Http;
using Aurender.Core.Utility;
using Aurender.Core.Setting;
using System.IO;
using System.Diagnostics;
using System.Net;

namespace Aurender.Core.Data.Services.Melon
{
    internal class SSMelon : StreamingServiceBase
    {
        protected internal SSMelon() : base(ContentType.Melon, "멜론", "http://www.melon.co.kr", "https://www.melon.com/muid/web/join/stipulationagreement_inform.htm", "melonUser")
        {
             this.Capability = new HashSet<StreamingServiceFeatures>()
            {
                StreamingServiceFeatures.ArtistTopTracks,

                StreamingServiceFeatures.FavoriteAlbum,
                StreamingServiceFeatures.FavoriteArtist,
                StreamingServiceFeatures.FavoriteTrack,
                StreamingServiceFeatures.FavoritePlaylist,
            };
            
      //      ProtectedLogout();
       //     PrepareFavorites();
        }

        public SSMelon(bool dummy) : this() {

        }

        private const String basePath = "https://alliance.melon.com:4600/";
        private static Uri basePathUri = new Uri("https://alliance.melon.com:4600/");
        private const String melonKey = "ARDR";
        private const String melonID  = "LA79";

        private String userID = String.Empty;
        private String subscriptionName = String.Empty;
        private String subscriptionExpire = String.Empty;
        private String MAC = String.Empty;

        private String defaultParam;
        private bool isAdult = false;
        private String UserAgent;
        private JToken tokenInfo;
        private Dictionary<String, dynamic> loginDictionary;
        private Dictionary<String, dynamic> rightDictionary;

        private static String CONTENT_TYPE = /*Uri.EscapeDataString(*/"application/x-www-form-urlencoded; charset=utf-8";//);

        public override IList<string> AvailableStreamingQuality => supportedStreamingQuality;

        static readonly IList<string> supportedStreamingQuality = new[] { "AAC+", "MP3 192kbps", "MP3 320kbps", "FLAC" };
        public override IList<string> SupportedStreamingQuality => supportedStreamingQuality;

        protected override void ProtectedLogout()
        {
            this.defaultParam = $"cpId={melonID}&cpKey={melonKey}";
        }

        private void PrepareBaseInfoForMelon()
        {
            var aurender = AurenderBrowser.GetCurrentAurender();
            var model = "Aurender";
            if (aurender != null)
            {
                model = "X100U";
                MAC = aurender.MAC;
            }
            string os = "Android 8.0";// Xamarin.Forms.Device.RuntimePlatform;
            string version = "3.0";
            this.UserAgent = Uri.EscapeDataString($" {melonID}; {os}; {version}; {model} ");

/*            Dictionary<String, String> headers = new Dictionary<string, string>();
            headers.Add("Accept-Charset", "utf8");
            headers.Add("Accept-Encoding", "deflate");
            */
        }

        public override async Task<Tuple<bool, string>> TryLoginAsync(IDictionary<StreamingServiceLoginInformation, string> information)
        {
            PrepareBaseInfoForMelon();
            this.cookie = new CookieContainer();

            var serviceToken = config.GetStringOrDefault(FieldsForServiceCache.ServiceToken);
            if (serviceToken != "")
                information[StreamingServiceLoginInformation.LoginToken] = serviceToken;

            if (information != null && information.ContainsKey(StreamingServiceLoginInformation.UserID))
            {
                string id = information[StreamingServiceLoginInformation.UserID];

                if(false)
                //if (information.ContainsKey(StreamingServiceLoginInformation.LoginToken))
                {
                    string token = information[StreamingServiceLoginInformation.LoginToken];

                    //when is invalid
                    if (await IsTokenValid(id, token).ConfigureAwait(false) == false)
                    {
                        if (information.ContainsKey(StreamingServiceLoginInformation.UserPassword))
                        {
                            this.L("LOGIN", "There is password info, so we will try with password");
                        }
                        else
                        {
                            this.userID = id;
                            this.subscriptionName = String.Empty;
                            this.isAdult = false;
                            string error = "비밀번호변경,\n로그인유효기간만료 등으로 인해\n로그아웃 되었습니다.\n보안을 위해 다시 로그인해주세요.";
                            loginMessages.Add(error);
                            return new Tuple<bool, string>(false, "Login failed");
                        }
                    }
                    else
                    {
                        if (await LoginWithToken(id, token).ConfigureAwait(false))
                        {
                            await DoAfterLoginProcess(information);
                            return new Tuple<bool, string>(true, LogInMessage);
                        }
                        // try login again
                        else if(await LoginWithToken(id, config.GetStringOrDefault(FieldsForServiceCache.ServiceToken)).ConfigureAwait(false))
                        {
                            serviceToken = config.GetStringOrDefault(FieldsForServiceCache.ServiceToken);
                            if (serviceToken != "")
                                information[StreamingServiceLoginInformation.LoginToken] = serviceToken;

                            await DoAfterLoginProcess(information);
                            return new Tuple<bool, string>(true, LogInMessage);
                        }
                        else
                        {
                            string error = "비밀번호변경,\n로그인유효기간만료 등으로 인해\n로그아웃 되었습니다.\n보안을 위해 다시 로그인해주세요.";
                            loginMessages.Add(error);
                            return new Tuple<bool, string>(false, LogInMessage);
                        }
                    }
                }

                if(true)
                //if (information.ContainsKey(StreamingServiceLoginInformation.UserPassword) &&
                //    information[StreamingServiceLoginInformation.UserPassword] != string.Empty)
                {
                    string pw = information[StreamingServiceLoginInformation.UserPassword];
                    if(await LoginWithPassword("fdsafagds", "asdfasgwea").ConfigureAwait(false))
                    //if (await LoginWithPassword(id, pw).ConfigureAwait(false))
                    {
                        ServiceInformation[StreamingServiceLoginInformation.UserID] = id;
                        
                        string token = this.loginDictionary["token"].ToString();

                        ServiceInformation[StreamingServiceLoginInformation.LoginToken] = token;
                        config[FieldsForServiceCache.ServiceToken] = token;

                        await DoAfterLoginProcess(information);

                        UserSetting.Setting.Save();

                        return new Tuple<bool, string>(true, LogInMessage);
                    }
                    else
                        this.L("LOGIN", "Password login failed");
                }
                else
                {
                    string error = "비밀번호변경,\n로그인유효기간만료 등으로 인해\n로그아웃 되었습니다.\n보안을 위해 다시 로그인해주세요.";
                    loginMessages.Add(error);
                }
            }
            else
            {
                String error = "No user information";
                loginMessages.Add(error);
            }
            return new Tuple<bool, string>(false, LogInMessage);
        }

        private async Task DoAfterLoginProcess(IDictionary<StreamingServiceLoginInformation, string> information)
        {
            var aurender = AurenderBrowser.GetCurrentAurender();

            string mac;
              if (aurender != null && aurender.MAC != null)
                mac = aurender.MAC.Replace(".", "%3A");
            else
                mac = "00:23:f2:00:4a:b5";

            Cookie cookie = new Cookie("PCID", mac);
            this.cookie.Add(new Uri("https://alliance.melon.com:4600/"), cookie);
            ServiceInformation = information;
            PrepareFavorites();
            await LoadSubscription();
            await LoadCatalog().ConfigureAwait(false);
            await LoadFavorites().ConfigureAwait(false);
            PrepareSearchResult();

            NotifyServiceLogin(true);
        }

        private (String code, String message) GetResultCode(Dictionary<String, dynamic> dictionary)
        {
            String code = "";
            String message = "";

            if (dictionary.ContainsKey("resultCode")) {
                var resultCode = dictionary["resultCode"] as String;
                if (resultCode != null)
                    code = resultCode;
            }
            if (dictionary.ContainsKey("errorMesg"))
            {
                if (dictionary["errorMesg"] is String errorMsg)
                    message = errorMsg;
            }
            return (code, message);
        }

        private async Task LoadSubscription()
        {
            String url = URLFor("muid/alliance/melon_productInfo.json");


            using (var response = await this.GetResponseByPostDataAsync(url))
            {
                this.rightDictionary = null;

                this.subscriptionName = "사용중인 이용권이 없습니다.\n이용권은 http://www.melon.co.kr에서 구매 가능합니다.";

                if (response.IsSuccessStatusCode)
                {
                    var t2 = response.Content.ReadAsStringAsync().ConfigureAwait(false);

                    var resultString = t2.GetAwaiter().GetResult();

                    var subscriptionResult = JsonConvert.DeserializeObject<Dictionary<String, dynamic>>(resultString);

                    var melonResult = GetResultCode(subscriptionResult);

                    this.rightDictionary = subscriptionResult;

                    if (this.rightDictionary.ContainsKey("prodName"))
                    {
                        var product = this.rightDictionary["prodName"].ToString();

                        if (product.Length != 0)
                            this.subscriptionName = product;
                    }
                    else
                        this.L("AfterLogin", "Failed t");
                }
                else
                    this.L("AfterLogin", "Failed to get usbscription");
            }
        }

        private async Task<bool> LoginWithPassword(string id, string pw)
        {
            String url = "muid/login/alliance/login_login.json";

            List<KeyValuePair<String, String>> postData = new List<KeyValuePair<String, String>>
            {
                new KeyValuePair<string, string>("v", "1.0"),
                new KeyValuePair<string, string>("memberId", id),
                new KeyValuePair<string, string>("memberPwd", pw),
                new KeyValuePair<string, string>("loginType", "1")
            };

            return await Login(url, postData).ConfigureAwait(false);
        }

        private async Task<bool> LoginWithToken(string id, string token)
        {
            String url = this.URLFor("muid/login/alliance/login_login.json", defaultParam);

            List<KeyValuePair<String, String>> postData = new List<KeyValuePair<String, String>>();
            postData.Add(new KeyValuePair<string, string>("v", "1.0"));
            postData.Add(new KeyValuePair<string, string>("memberId", id));
            postData.Add(new KeyValuePair<string, string>("token", token));
            postData.Add(new KeyValuePair<string, string>("loginType", "3"));

            return await Login(url, postData).ConfigureAwait(false);
        }

        private async Task<bool> Login(String url, List<KeyValuePair<String, String>> postData)
        {
            using (var result = await this.GetResponseByPostDataAsync(url, postData))
            {
                if (result.IsSuccessStatusCode)
                {
                    var resultString = await result.Content.ReadAsStringAsync().ConfigureAwait(false);

                    var loginResult = JsonConvert.DeserializeObject<Dictionary<String, dynamic>>(resultString);
                    if (loginResult["resultCode"] is string resultCode)
                    {
                        if (resultCode == "000000")
                        {
                            this.L("LOGIN", "Token login is sucessful");
                            loginDictionary = loginResult;
                            return true;
                        }

                        String errorMessage = String.Empty;
                        if (loginResult.ContainsKey("errorMesg"))
                        {
                            String msg = loginResult["errorMesg"];
                            this.loginMessages.Add(msg);
                        }
                        else
                            this.loginMessages.Add(ERROR_LOGIN_FAILED);
                        
                        this.L("LOGIN", "Faile to login");
                    }
                    else
                        this.L("LOGIN", "Failed to parse login result");
                }
                else
                    this.L("LOGIN", "Failed to send request for login");

            }
            return false;
        }



        private const string ERROR_LOGIN_FAILED = "로그인에 실패했습니다.\n자세한 내용은 Melon사이트에서 확인해주세요.";
        private const  string ERROR_TRY_LATER = "나중에 다시 이용해주세요.";

        private string GetErrorCode(String resultCode)
        {
            switch (resultCode)
            {
                case "ERL002":
                    return "비밀번호가 맞지 않습니다.";


                case "ERL003":
                    return "비밀번호 5회 이상 초과 오류 하였습니다.D21";


                case "ERL006":
                    return "회원정보가 존재 하지 않습니다.";


                case "ERL069":
                    return "비밀번호변경,로그인유효기간만료 등으로 인해 로그아웃 되었습니다. 보안을 위해 다시 로그인해 주세요.";


                case "ERS002":
                    return "일시적인 장애 입니다. 잠시 후에 이용해 주세요.";


                case "3001":
                    return "곡 좋아요 최대개수 초과";

                case "3021":
                    return "많이 들은곡 비공개";

                case "4001":
                    return "필수 입력 값 오류";

                case "4002":
                    return "회원키 조회 오류";

                case "4003":
                    return "회원정보 없음";

                case "4004":
                    return "탈퇴한 회원";

                case "4005":
                    return "버전정보 오류";

                case "5001":
                    return "서비스 오류";

                case "-1":
                    return "서버 점검";
                   
                default:
                    return ERROR_TRY_LATER;
            }
        }

        public (String code, String message, String errorMessag) GetStatusInfoFromResult(Dictionary<String, dynamic> dictionary)
        {
            String code = "";
            String message = "";
            String errorMsg = "";

            if (dictionary.ContainsKey("STATUS"))
            {
                code = dictionary["STATUS"].ToString();

                code = GetErrorCode(code);
            }
            if (dictionary.ContainsKey("ERRORMSG"))
            {
                errorMsg = dictionary["ERRORMSG"].ToString();
            }

            return (code, message, errorMsg);
        }

        private async Task<bool> IsTokenValid(string id, string token)
        {
            string postData = $"{defaultParam}&memberId={id.URLEncodedString()}&token={token.URLEncodedString()}";

            string uri = this.URLFor("muid/alliacnce/tokenvalid_infor.json", postData);

            using (var result = await GetResponseByPostDataAsync(uri))
            {
                this.tokenInfo = null;

                if (result.IsSuccessStatusCode)
                {
                    String message = await result.Content.ReadAsStringAsync().ConfigureAwait(false);

                    var resultDictionary = JsonConvert.DeserializeObject<Dictionary<String, dynamic>>(message);
                    if (resultDictionary != null)
                    {
                        var resultCode = resultDictionary["resultCode"];
                        if (resultCode != null && resultCode is String)
                        {
                            if (resultCode == "000000")
                            {
                                this.L("LOGIN-Token", "Token is valid");
                                this.tokenInfo = resultDictionary["tokenInfo"];
                                return true;
                            }
                        }
                        else
                            this.L("LOGIN-Token", "TokenValid failed to get resultCode");
                    }
                    else
                    {
                        this.L("LOGIN-Token", "TokenValid failed to get result json");
                    }

                    return false;
                }
                else
                {
                    this.L("LOGIN", "Token is not valid");
                    return false;
                }
            }
        }


        internal void LogOut()
        {
            String url = this.URLFor("muid/alliance/logout_logout.json");

            using (var t = this.GetResponseByPostDataAsync(url))
            {
                t.Wait();
                using (var res = t.Result)
                {

                }

            }
        }

        internal override String URLFor(String partialPath, String paramString ="")
        {
            String str;
            if (paramString.Length > 0)
            {
                if (partialPath.IndexOf("?") > 0)
                    str = $"{basePath}{partialPath}&{paramString}";
                else
                    str = $"{basePath}{partialPath}?{paramString}";
            }
            else
                str = $"{basePath}{partialPath}"; // URLForWithoutDefaultParam(partialPath);               
            return str;
        }

        internal override String URLForWithoutDefaultParam(String partialPath)
        {
            String str;
            if (partialPath.IndexOf("?") > 0)
            {
                str = $"{basePath}/{partialPath}";
                str = str.Replace("?", $"?{defaultParam}&");
            }
            else
                str = $"{basePath}/{partialPath}?{defaultParam}";

            return str;
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



        #region Playlist which is not supported
        public override Task<IStreamingPlaylist> CreatePlaylistAsync(string title, IList<IStreamingTrack> tracks, string description = "")
        {
            throw new NotImplementedException();
        }

        public override Task<bool> DeletePlaylistAsync(IStreamingPlaylist playlist)
        {
            throw new NotImplementedException();
        }
        public override Task<bool> UpdatePlaylistAsync(IStreamingPlaylist playlist, string newTitle, IList<IStreamingTrack> tracks, string description = "")
        {
            throw new NotImplementedException();
        }
        #endregion

        protected override async Task<IStreamingTrack> TrackWithIDAsync(string trackID)
        {
            String url = URLFor($"cds/delivery/alliance/song_info.json?cId={trackID}&cType=1&imgW=300&imgH=300");
            IStreamingTrack track = null;

            JToken token = await GetResponseJTken(url);
            if (token != null)
            {
                JToken array = token["CONTENTSINFO"];

                if (array.Type == JTokenType.Array)
                {
                    foreach (var d in array)
                    {
                        if (d["CID"].ToObject<String>() == trackID)
                        {
                            track = new MelonTrack(d);
                            break;
                        }
                    }
                }
            }

            if (track == null)
                this.EP("Melon", $"Failed to get track : {trackID}");

            return track;
        }



        public override async Task<IStreamingAlbum> GetAlbumWithIDAsync(string albumID)
        {
            String url = URLFor($"detail/infoAlbum.json?albumId={albumID}");

            IStreamingAlbum album = null;

            JToken token = await GetResponseJTken(url);
            if (token != null)
                album = new MelonAlbum(token);
            else
                this.EP("Melon", $"Failed to get album : {albumID}");

            return album;
        }

        public override async Task<IStreamingArtist> GetArtistWithIDAsync(string artistmID)
        {
            String url = URLFor($"detail/artistInfo.json?artistId={artistmID}");

            IStreamingArtist artist = null;

            JToken token = await GetResponseJTken(url);
            if (token != null)
                artist = new MelonArtist(token);
            else
                this.EP("Melon", $"Failed to get artist : {artistmID}");

            return artist;
        }

        public override async Task<IStreamingPlaylist> GetPlaylistWithIDAsync(string playlistID)
        {
            String url = URLFor($"melondj/playListInform.json?plylstSeq={playlistID}&imgW=300&imgH=300");
            IStreamingPlaylist playlist = null;

            JToken  token = await GetResponseJTken(url);
            if (token != null)
                playlist = new MelonDJAlbum(token);
            else
                this.EP("Melon", $"Failed to get artist : {playlistID}");

            return playlist;
        }

        public override object IconForQueue(bool isPlaying)
        {
            throw new NotImplementedException();
        }

        protected override async Task LoadCatalog()
        {
            List<(String title, String link)> songs = new List<(string title, string link)>()
            {
                ("실시간 차트",  "chart/listRealTimeChart.json?pageSize=100&startIndex=1"),
                ("일간 차트",    "chart/listDailyChart.json?pageSize=100&startIndex=1"),
                ("주간 차트",    "chart/listWeeklyChart.json?pageSize=100&startIndex=1"),
                ("최신곡 국내",  "alliance/newmusic/newsong_list.json"),
                ("최신곡 해외",  "alliance/newmusic/newsong_list.json?areaFlg=O"),
//              ("최근 들은 곡", "mymusic/recentPlaySongList.json"),
                ("많이 들은 곡", "mymusic/manyPlaySongList.json?dateType=1M")
            };

            foreach(var feature in songs)
            {
                var tracks = new MelonFeaturedTracks(feature.title, feature.link);
                this.collectionsForTracks.Add(new KeyValuePair<String, IStreamingObjectCollection<IStreamingTrack>>(feature.title, tracks));
            }
            
            List<(String title, String link)> albums = new List<(string title, string link)>() {
                ("앨범 차트" ,  "chart/listAlbumChart.json?imgW=300&imgH=300"), 
                ("최신 국내" ,  "alliance/newmusic/newalbum_list.json?imgW=300&imgH=300"), 
                ("최신 해외" ,  "alliance/newmusic/newalbum_list.json?imgW=300&imgH=300&areaFlg=O"), 
            };

            foreach (var feature in albums)
            {
                var tracks = new MelonFeaturedAlbums(feature.title, feature.link);
                this.collectionsForAlbums.Add(new KeyValuePair<String, IStreamingObjectCollection<IStreamingAlbum>>(feature.title, tracks));
            }

            List<(String title, String link)> playlists = new List<(string title, string link)>() {
               ("명예의 전당",    "melondj/playListCategoryWhc.json?cateType=H&itemSize=30"),
               ("오늘은 뭘 듣지", "melondj/playListCategoryWhc.json?cateType=W"),
               ("DJ앨범 차트",    "melondj/playListCategoryWhc.json?cateType=C"),
            };

            foreach (var feature in playlists)
            {
                var tracks = new MelonFeaturedPlaylists(feature.title, feature.link);
                this.collectionsForPlaylists.Add(new KeyValuePair<String, IStreamingObjectCollection<IStreamingPlaylist>>(feature.title, tracks));
            }


            var genre1 = ("한국대중음악", new List<(String title, String link)>() {
		                  ("발라드" ,  "GN0100"),
		                  ("댄스" , "GN0200"),
		                  ("랩/힙합" , "GN0300"),
		                  ("R&B/Soul" , "GN0400"),
		                  ("인디음악" , "GN0501"),
		                  ("록/메탈" , "GN0600"),
		                  ("트로트" , "GN0701"),
		                  ("포크/블루스 " , "GN0800"),});

            var genre2 = ("해외POP음악", new List<(String title, String link)>(){
		  		  		  ("Pop" , "GN0901"),
		  		  		  ("록/메탈" , "GN1001"),
		  		  		  ("일렉트로니카" , "GN1101"),
		  		  		  ("랩/힙합" , "GN1201"),
		  		  		  ("R&B/Soul" , "GN1301"),
		  		  		  ("포크/블루스/컨트리" , "GN1401"), });


            var genre3 = ("어린이/태교", new List<(String title, String link)>(){
		  		  		  ("유아동요" , "GN2201"),
		  		  		  ("어린이동요" , "GN2202"),
		  		  		  ("영어동요"  , "GN2203"),
		  		  		  ("한국동화"  , "GN2204"),
		  		  		  ("세계동화"  , "GN2205"),
		  		  		  ("영어동화"  , "GN2206"),
		  		  		  ("만화"   , "GN2207"),
		  		  		  ("자장가"  , "GN2208"),
		  		  		  ("엄마,아빠와함께" , "GN2209"),
		  		  		  ("아가를 위한 클래식" , "GN2210"),
		  		  		  ("릴렉싱&힐링" , "GN2211"),});

            var genre4 = ("그 외 인기장르", new List<(String title, String link)>(){
		  		  		("OST" , "GN1501"),
		  		  		("클래식" , "GN1601"),
		  		  		("재즈" , "GN1701"),
		  		  		("뉴에이지" , "GN1801"),
		  		  		("J-POP" , "GN1901"),
		  		  		("월드뮤직" , "GN2001"),
		  		  		("CCM" , "GN2101"),
		  		  		("종교음악-가톨릭음악" , "GN2301"),
		  		  		("종교음악-불교음악" , "GN2302"),
		  		  		("국악" , "GN2401"),});


            List<(String title, List<(String title, String link)> subGenre)> genres = new List<(string title, List<(string title, string link)> subGenre)>()
            {
               genre1, genre2, genre3, genre4
            };

            foreach (var data in genres)
            {
                var collection = new MelonGenreCollection(data.Item1, data.Item2);
                this.collectionsForGenres.Add(new KeyValuePair<String, IStreamingObjectCollection<IStreamingGenre>>(collection.Title, collection));
            }

            await Task.Delay(0).ConfigureAwait(false);
        }

        protected override async Task LoadFavorites()
        {
            var config = UserSetting.Setting[ServiceType];
            var favorite = ServiceManager.Favorite;
            var star = ServiceManager.Star;
            var myPlaylist = ServiceManager.MyPlaylist;
            var albumsOrder = config.Get(FieldsForServiceCache.OrderForFavoriteAlbums, "NEW");

            var tracks = new MelonFavoriteTracks(favorite);
            this.collectionsForTracks.Add(favorite, tracks);
            await tracks.LoadNextAsync().ConfigureAwait(false);
            HashSet<String> trackIDs = tracks.AllIDs();
            this.cachedFavoriteIDs.Add(trackIDs);

            var albums = new MelonFavoriteAlbums(favorite, albumsOrder);
            this.collectionsForAlbums.Add(collectionsForAlbums.Count >= 5 ? star : favorite, albums);

            var artists = new MelonFavoriteArtists(favorite);
            this.collectionsForArtists.Add(collectionsForArtists.Count >= 5 ? star : favorite, artists);

            IStreamingObjectCollection<IStreamingPlaylist> playlists = new MelonFavoritePlaylists(favorite);
            this.collectionsForPlaylists.Add(collectionsForPlaylists.Count >= 5 ? star : favorite, playlists);

            playlists = new MelonMyPlaylists(myPlaylist);
            this.collectionsForPlaylists.Add(myPlaylist, playlists);


            prepareFavoriteCachedIDs();
        }

        private void prepareFavoriteCachedIDs()
        {
            String url = URLFor($"mymusic/likeInfo.json?imgW=300&imgH=300");

            using (var t = this.GetResponseJTken(url))
            {
                t.Wait();

                JToken result = t.Result;

                if (result != null)
                {
                    List<String> tags = new List<string>()
                        {
                            "ALBUM", "ARTIST", "PLAYLIST",
                        };

                    for (int i = 0; i < tags.Count; i++)
                    {
                        HashSet<String> strings = new HashSet<string>();
                        var jj = result[tags[i]];
                        if (jj != null)
                        {
                            JArray items = jj as JArray;
                            if (items != null)
                            {

                                foreach (var id in items)
                                {
                                    strings.Add(id.ToString());
                                }
                                this.LP($"{ServiceType}", $"Loaded cached {tags[i]} : {strings.Count:n0}");
                            }
                        }
                        this.cachedFavoriteIDs.Add(strings);
                    }
                }
                this.LP($"{ServiceType}", $"Loaded cached tags");
            }
            this.SaveStatus();
            UserSetting.Setting.Save();

        }


        protected override void PrepareCachedTracks()
        {
            this.cachedTracks = config.GetCachedTracks<MelonTrack>();
        }

        protected override void PrepareSearchResult()
        {
            var tracks = new MelonSearchResultForTracks(Search);
            this.collectionsForTracks.Add(collectionsForTracks.Count >= 5 ? MagnifyingGlass : Search, tracks);

            var albums = new MelonSearchResultForAlbums(Search);
            this.collectionsForAlbums.Add(collectionsForAlbums.Count >= 5 ? MagnifyingGlass : Search, albums);
            
            var artists = new MelonSearchResultForArtists(Search);
            this.collectionsForArtists.Add(collectionsForArtists.Count >= 5 ? MagnifyingGlass : Search, artists);
        }


        internal override bool IsUserUpdatable(IStreamingPlaylist playlist)
        {
            return false;
        }

        internal override async Task UpdateFavorite(IStreamingFavoritable item, bool favorite)
        {
            if (!Capability.Contains(item.StreamingItemType.FavoriteFeature()))
            {
                this.Info($"{ServiceType}", $"Does not support favorite for {item}");
                return;
            }

            String itemType;
            String likes = favorite ? "N" : "Y";


            switch (item.StreamingItemType)
            {
                case StreamingItemType.Track:
                    itemType = "SONG";
                    break;

                case StreamingItemType.Album:
                    itemType = "ALBUM";
                    break;
                case StreamingItemType.Aritst:
                    itemType = "ARTIST";
                    break;

                case StreamingItemType.Playlist:
                    itemType = "DJPLAYLIST";
                    break;


                case StreamingItemType.Genre:
                default:
                    itemType = "";
                    Debug.Assert(false, $"Shouldn't be reached here : TIDAL::UpdateFavorite(IStreamingFavoritable, bool)");
                    break;
            }

            String url = URLFor($"mymusic/updateContentsLike.json?contsTypeCode={itemType}&contentsId={item.StreamingID}&existUserLike={likes}&menuId=");

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
                            var resultCode = token["STATUS"];
                            if (resultCode !=null && resultCode.ToString() == "0")
                            {
                                UpdateFavoriteChached(item, favorite);
                                NotifyFavoriteUpdated(item, favorite);
                                return;
                            }
                            else
                            {
                                this.EP("Melon", $"Failed to update favorite with {token.ToString()} for {item}");
                                NotifyFavoriteUpdateFailed(item);
                            }
                        }
                        else
                            this.EP("Melon", $"Failed to update favorite {item}, \n{responseString}");
                    }
                    else
                        this.EP("Melon", $"Failed to update favorite {item}");
                }
            }
            catch (Exception ex)
            {
                this.EP("Melon", $"Faield to update {item}", ex);
            }
        }

        internal static  JToken GetTokenFromURL(HttpResponseMessage t)
        {
            JToken token = null;

            if (t.IsSuccessStatusCode)
            {
                var str = t.Content.ReadAsStringAsync();
                str.Wait();

                JToken jtoken = JToken.Parse(str.Result);
                var result = MelonUtility.IsRequestSucess(jtoken);
                if (result)
                    token = jtoken;
            }

            return token;
        } 
        internal override async Task<HttpResponseMessage> GetResponseByPostDataAsync(string url, List<KeyValuePair<String, String>> postData = null)
        {
            using (HttpClientHandler handler = new HttpClientHandler())
            {
                handler.CookieContainer = cookie;
                using (HttpClient client = new HttpClient(handler))
                {
                    client.BaseAddress = basePathUri;

                    client.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);
                    Console.WriteLine(client.DefaultRequestHeaders.UserAgent);

                    client.DefaultRequestHeaders.Add("Accept-Charset", "utf-8");
                    client.DefaultRequestHeaders.Add("Accept-Encoding", "deflate");

                    List<KeyValuePair<String, String>> data = new List<KeyValuePair<string, string>>();

                    data.Add(new KeyValuePair<string, string>("cpId", melonID));
                    data.Add(new KeyValuePair<string, string>("cpKey", melonKey));

                    if (postData != null)
                    {
                        foreach (var kv in postData)
                            data.Add(kv);
                    }

                    var formData = new FormUrlEncodedContent(data);
                    //formData.Headers.Add("Accept-Charset", "utf-8");
                    //formData.Headers.Add("Accept-Encoding", "deflate"); 

                    //client.DefaultRequestHeaders.TryAddWithoutValidation("Content-Type", CONTENT_TYPE);
                    try
                    {
                        formData.Headers.Clear();
                        bool cty = formData.Headers.TryAddWithoutValidation("Content-Type", CONTENT_TYPE);

                        await formData.ReadAsStringAsync().ConfigureAwait(false);

                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                    var result = await client.PostAsync(url, formData).ConfigureAwait(false);

                    return result;
                }
            }

        }
        
        internal override async Task<HttpResponseMessage> GetResponseByGetDataAsync(string url, List<KeyValuePair<String, String>> postDat = null)
        {
            using(HttpClientHandler handler = new HttpClientHandler())
            {
                handler.CookieContainer = cookie;
                using(HttpClient client = new HttpClient(handler)) {
                    client.BaseAddress = basePathUri;
                   
                    client.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);

                 //   client.DefaultRequestHeaders.Add("Accept-Charset", "utf-8");
                 //   client.DefaultRequestHeaders.Add("Accept-Encoding", "deflate");

                    String key;
                    if (url.Contains("v=1")) {
                        key = $"?cpId={melonID}&cpKey={melonKey}&";
                    }
                    else
                    {
                        if (url.Contains("playListInfom.json"))
                            key = $"?cpId={melonID}&cpKey={melonKey}&v=1.1&";
                        else 
                            key = $"?cpId={melonID}&cpKey={melonKey}&v=1.0&";
                    }

                    if (postDat != null)
                    {
                        foreach (var kv in postDat)
                            Console.WriteLine($"param must be in the url not postDat : {kv}");
                    }
                    var newUrl = url.Replace("?", key);
                    bool cty = client.DefaultRequestHeaders.TryAddWithoutValidation("Content-Type", CONTENT_TYPE);

                    var relative = newUrl.StartsWith(basePath) ? newUrl.Replace(basePath, "") : newUrl;

                    try
                    {
                        client.DefaultRequestHeaders.Add("Connection", "close");
                         var result = await client.GetAsync(relative);

                        return result;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                    return null;
                }
            } 
        }

        internal async Task<JToken> GetResponseJTken(string url)
        {
            var t = await GetResponseStringByGetDataAsync(url).ConfigureAwait(false);

            if (t.Length != 0)
            {
                JToken jtoken = JToken.Parse(t);

                return jtoken;

            }
            return null;
        }

        internal async Task<String> GetResponseStringByGetDataAsync(string url, List<KeyValuePair<String, String>> postDat = null)
        {
            //   client.DefaultRequestHeaders.Add("Accept-Charset", "utf-8");
            //   client.DefaultRequestHeaders.Add("Accept-Encoding", "deflate");

            String key;
            if (url.Contains("v=1"))
            {
                key = $"?cpId={melonID}&cpKey={melonKey}&";
            }
            else
            {
                if (url.Contains("playListInform.json"))
                    key = $"?cpId={melonID}&cpKey={melonKey}&v=1.1&";
                else
                    key = $"?cpId={melonID}&cpKey={melonKey}&v=1.0&";
            }

            if (postDat != null)
            {
                foreach (var kv in postDat)
                    Console.WriteLine($"param must be in the url not postDat : {kv}");
            }
            var newUrl = url.Replace("?", key);

            var relative = newUrl.StartsWith(basePath) ? newUrl.Replace(basePath, "") : newUrl;

            HttpWebRequest req = (HttpWebRequest)WebRequest.Create($"{basePath}{relative}");
            req.KeepAlive = false;
            req.Timeout = 60 * 1000;
            req.AllowWriteStreamBuffering = false;
            req.Method = "GET";

            try
            {
                req.ContentType = CONTENT_TYPE;
                req.UserAgent = UserAgent;
                using (var response = await Task.Factory.FromAsync(req.BeginGetResponse, req.EndGetResponse, null))

                    if (response != null)
                    {
                        using (var stream = response.GetResponseStream())
                        using (var streamReader = new StreamReader(stream))
                        {
                            var text = await streamReader.ReadToEndAsync();
                            return text;
                        }
                    }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return string.Empty;
        }
    }
}
