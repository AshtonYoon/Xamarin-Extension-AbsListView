using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aurender.Core.Contents.Streaming;
using Aurender.Core.Player;
using Aurender.Core.Player.mpd;
using Aurender.Core.Utility;

namespace Aurender.Core.Data.Services.Shoutcast
{
    internal class SSShoutcast : StreamingServiceBase
    {
        public override IList<string> AvailableStreamingQuality => throw new NotImplementedException();

        public override IList<string> SupportedStreamingQuality => throw new NotImplementedException();

        internal SSShoutcast() : base(ContentType.InternetRadio, "Internet Radio", "", "", "shoutcast")
        {
            this.Capability = new HashSet<StreamingServiceFeatures>()
            {
                StreamingServiceFeatures.FavoriteTrack,
            };
        }

        internal override bool IsFavorite(IStreamingFavoritable item)
        {
            return false;
        }

        public override async Task CheckServiceAndLoginIfAvailableAsync(IAurenderEndPoint aurender)
        {
                try
                {
                    this.IsLoggedIn = true;

                    this.ServiceInformation = new Dictionary<StreamingServiceLoginInformation, string>();

                    await this.LoadCatalog();
                    await this.LoadFavorites();
                    this.PrepareSearchResult();
                    

                    this.NotifyServiceLogin(true);
                }
                catch (Exception ex)
                {
                    this.EP($"{ServiceType}", "Failed ", ex);
                }
        }

        public async override Task<Tuple<bool, string>> TryLoginAsync(IDictionary<StreamingServiceLoginInformation, string> information)
        {
            await LoadCatalog();
            NotifyServiceLogin(true);
            await Task.Delay(10);
            return new Tuple<bool, string>(true, string.Empty);
        }

        protected override async Task LoadCatalog()
        {
            String url = GetURLForRecommendedStations();
            if (url != null && url.Length > 0)
            {
                var name = LocaleUtility.Translate("Recommended");

                SCRecommendedStionCollection recommended = new SCRecommendedStionCollection(name, url);
                this.collectionsForTracks.Add(name, recommended);
            }

            SCStationCollection stations = new SCStationCollection("Top 500 stations", "legacy/Top500", 500);
         //   await stations.LoadNextAsync().ConfigureAwait(false);
            this.collectionsForTracks.Add(stations.Title, stations);

            SCGenreCollection genres = new SCGenreCollection("genre/primary?f=json");
            await genres.LoadNextAsync().ConfigureAwait(false);
            this.collectionsForGenres.Add(genres.Title, genres);
        }

        private string GetURLForRecommendedStations()
        {
            string url = TimeZoneUtility.GetInternetRadioUrl();

            return url;
        }

        protected override async Task LoadFavorites()
        {
            var aurender = AurenderBrowser.GetCurrentAurender();
            if (aurender.IsConnected())
            {
                var stations = await aurender.GetFavoriteStations().ConfigureAwait(false);

                this.collectionsForTracks.Add(stations.Title, stations);
            }
        }

        public override Task<IStreamingPlaylist> CreatePlaylistAsync(string title, IList<IStreamingTrack> tracks, string description = "")
        {
            throw new NotImplementedException();
        }

        public override Task<bool> DeletePlaylistAsync(IStreamingPlaylist playlist)
        {
            throw new NotImplementedException();
        }

        public override Task<IStreamingAlbum> GetAlbumWithIDAsync(string albumID)
        {
            // this task will return null
            return Task.FromResult(default(IStreamingAlbum));
        }

        public override Task<IStreamingArtist> GetArtistWithIDAsync(string artistmID)
        {
            throw new NotImplementedException();
        }

        public override Task<IStreamingPlaylist> GetPlaylistWithIDAsync(string playlistID)
        {
            throw new NotImplementedException();
        }

        public override object IconForQueue(bool isPlaying)
        {
            throw new NotImplementedException();
        }

        public override Task<bool> UpdatePlaylistAsync(IStreamingPlaylist playlist, string newTitle, IList<IStreamingTrack> tracks, string description = "")
        {
            throw new NotImplementedException();
        }
        
        protected override void PrepareCachedTracks()
        {
           
        }

        protected override void PrepareSearchResult()
        {
            SCStationSearchCollection stations = new SCStationSearchCollection(Search);
            this.collectionsForTracks.Add(collectionsForTracks.Count >= 5 ? MagnifyingGlass : Search, stations);
        }

        protected override void ProtectedLogout()
        {
            this.collectionsForTracks.Clear();
        }

        internal override bool IsUserUpdatable(IStreamingPlaylist playlist)
        {
            return false;
        }

        public override IStreamingTrack GetTrackWithPath(String filePath)
        {
            if (filePath.StartsWith("shout://"))
            {
                String line = filePath.Substring(8);
                IStreamingTrack t = new SCCustomStation(line);
				
                return t;
            }
            return null;
        }

        internal override async Task UpdateFavorite(IStreamingFavoritable track, bool toFavorite)
        {
            var aurender = AurenderBrowser.GetCurrentAurender();
            if (aurender != null)
            {
                var stations = getFavoriteStations();
                if (track is SCStation station)
                {
                    if (!toFavorite)
                    {
                        await aurender.SCRemoveStation(stations, station).ConfigureAwait(false);
                    }
                    else
                    {
                        string url;
                        if (station is SCCustomStation)
                        {
                            this.EP("Shoutcast", "Can't add CustomStation directly");
                        }
                        else
                        {
                            // get url from shoutcast
                            String requestURL = $"http://yp.shoutcast.com{station.TuneIn}?id={station.StreamingID}";
                            var result = await WebUtil.DownloadContentsAsync(requestURL).ConfigureAwait(false);

                            if (result.Item1)
                            {
                                url = result.Item2;
                                await aurender.SCAddStation(stations, station.Title, url).ConfigureAwait(false);

                                this.NotifyFavoriteUpdated(station, toFavorite);
                            }
                            else
                                this.EP("Shoutcast", "Faeild to get real URL to add favorite");
                        }
                    }
                }
            }
            else
            {
                this.EP("Shoutcast", "Failed to add station to favorite. No Aurender");
            }
        }

        public async Task<IStreamingObjectCollection<IStreamingTrack>> AddCustomStation(String title, String url)
        {
            var stations = getFavoriteStations();
            var aurender = AurenderBrowser.GetCurrentAurender();
            if (aurender != null)
            {
                await aurender.SCAddStation(stations, title, url).ConfigureAwait(false);
            }
            return stations;
        }

        private IStreamingObjectCollection<IStreamingTrack> getFavoriteStations()
        {
            return collectionsForTracks["Favorite"];
        }

        protected override Task<IStreamingTrack> TrackWithIDAsync(string trackID)
        {
            throw new NotImplementedException();
        }

        const String KEY = "nxNUtnPx6a5AMkaa";

        internal override string URLFor(string partialPath, string paramString = "")
        {
            return URLForShoutcast(partialPath, paramString);
        }

        internal override string URLForWithoutDefaultParam(string partialPath)
        {
            throw new NotImplementedException();
        }

        internal static String URLForShoutcast(String partialPath, string paramString = "")
        {
             String result;
            bool hasPararmeter = partialPath.Contains("?");
            string newParam = $"?k={KEY}&{paramString}";
            if (hasPararmeter)
                newParam = $"&k={KEY}&{paramString}";

            if (partialPath.Contains("legacy/"))
            {
                result = $"http://api.shoutcast.com/{partialPath}{newParam}";
            }
            else
            {
                result = $"http://api.shoutcast.com/{partialPath}{newParam}&f=json";
            }

            return result;
        }
    }
}