#if !DEBUG
#define ENABLE_SERVICE
#endif

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aurender.Core.Contents.Streaming;
using Aurender.Core.Data.Services.Qobuz;
using Aurender.Core.Player;
using Aurender.Core.Player.mpd;
using Aurender.Core.Setting;
using Aurender.Core.Utility;

namespace Aurender.Core.Data.Services
{
    public class ServiceManager : IStreamingServiceManager
    {
        private static volatile ServiceManager instance;
        private static object syncRoot = new Object();
        static ServiceManager()
        {
            // TODO: get device os
            OS = "Android";

            Favorite = LocaleUtility.Translate("Favorites");
            Star = "⭐";
            MyPlaylist = LocaleUtility.Translate("MyPlaylist");
        }

        static Lazy<IStreamingServiceManager> current = new Lazy<IStreamingServiceManager>(() =>
        {
            instance = new ServiceManager();
            instance.InitiateServices();

            return instance;
        });

        public static IStreamingServiceManager it => current.Value;

        private ServiceManager()
        {
            services = new Dictionary<ContentType, IStreamingService>();
        }

        private IDictionary<ContentType, IStreamingService> services;

        public event StreamingServiceEventHandler OnServiceLoginStatusChanged;
        public event StreamingServiceEventHandler<IStreamingTrack> OnStreamingTrackLoaded;
        public event StreamingServiceEventHandler<string> GetMessageFromService;
        public event StreamingServiceEventHandler<IStreamingFavoritable, bool> OnFavoriteItemStatusChanged;
        public event StreamingServiceEventHandler<IStreamingFavoritable> OnFavoriteItemStatusChangeFailed;

        public IStreamingService this[ContentType type]
        {
            get
            {
                if (services.ContainsKey(type))
                    return services[type];
                else
                    return null;
            }
        }
        public IStreamingService CurrentService { get; private set; }
        public static string CurrentMAC { get => AurenderBrowser.GetCurrentAurender()?.MAC ?? string.Empty; }
        public static readonly String OS;
        public static readonly string MyPlaylist;
        public static readonly string Favorite;
        public static readonly string Star;

        public static string MyMusic;
        
        public IEnumerable<IStreamingService> AvailableServices()
        {
            IEnumerable<IStreamingService> loggedOnServices = services.Values.AsEnumerable<IStreamingService>();

            return loggedOnServices;
        }

        public void CheckServicesFor(IAurenderEndPoint aurender)
        {
            foreach (var s in services.Values)
            {
                s.CheckServiceAndLoginIfAvailableAsync(aurender);
            }
        }

        public void SaveSettings()
        {
            foreach (var s in services.Values)
            {
                s.SaveStatus();
            }
            UserSetting.Setting.Save();
        }

        public IEnumerable<IStreamingService> LoggedInServices()
        {
            IEnumerable<IStreamingService> loggedOnServices = services.Values.Where<IStreamingService>(s => s.IsLoggedIn);
            return loggedOnServices;
        }

        internal static SSQobuz Qobuz()
        {
            return (SSQobuz)  it[ContentType.Qobuz];
        }

        public void PrepareSearch(string keyword, EViewType viewType)
        {
            services.Values.Where<IStreamingService>(delegate (IStreamingService service)
            {

                Task.Run(delegate ()
                {
                    service.SearchForAsync(viewType, keyword);
                });

                return false;
            });
        }

        public void SelectService(ContentType serviceToSelect)
        {
            this.CurrentService = services[serviceToSelect];
        }

        internal static StreamingServiceImplementation Service(ContentType type)
        {
            if (instance.services.ContainsKey(type))
                return instance.services[type] as StreamingServiceImplementation;
            else
                return null;
        }




        private void InitiateServices()
        {
            // create TIDAL
            IStreamingService service = new TIDAL.SSTIDAL();
            AddService(service);

            // add Qobuz by country. UK, France, Germany, Spain, Italy, Netherland, Belgium, Luxembourg, Portugal, Swiss and Austria
#if ENABLE_SERVICE
            if (TimeZoneUtility.IsSupportQobuz())
#endif
            {
                service = new Qobuz.SSQobuz();
                AddService(service);
            }

            // Add Korean service
#if ENABLE_SERVICE
            if (TimeZoneUtility.IsKoreanTimeZone())
#endif
            {
                service = new Bugs.SSBugs();
                AddService(service);

                service = new Melon.SSMelon();
                AddService(service);
            }

#if ENABLE_SERVICE
            if (TimeZoneUtility.IsSupportInternetRadio())
#endif
            {
                service = new Shoutcast.SSShoutcast();
                AddService(service);
            }


        }

        private void AddService(IStreamingService service)
        {
            services.Add(service.ServiceType, service);

            service.OnFavoriteItemStatusChanged += Service_OnFavoriteItemStatusChanged;
            service.OnFavoriteItemStatusChangeFailed += Service_OnFavoriteItemStatusChangeFailed;
            service.OnServiceLoginStatusChanged += Service_OnServiceLoginStatusChanged;
            service.OnStreamingTrackLoaded += Service_OnStreamingTrackLoaded;
            service.GetMessageFromService += Service_GetMessageFromService;
        }

        private void Service_GetMessageFromService(IStreamingService sender, string t)
        {
            GetMessageFromService?.Invoke(sender, t);
        }

        private void Service_OnStreamingTrackLoaded(IStreamingService sender, IStreamingTrack t)
        {
            OnStreamingTrackLoaded?.Invoke(sender, t);
        }

        private void Service_OnServiceLoginStatusChanged(IStreamingService sender)
        {
            OnServiceLoginStatusChanged?.Invoke(sender);
        }

        private void Service_OnFavoriteItemStatusChangeFailed(IStreamingService sender, IStreamingFavoritable t)
        {
            OnFavoriteItemStatusChangeFailed?.Invoke(sender, t);
        }

        private void Service_OnFavoriteItemStatusChanged(IStreamingService sender, IStreamingFavoritable t, bool u)
        {
            OnFavoriteItemStatusChanged?.Invoke(sender, t, u);
        }

        public async Task<IPlayableItem> GetItemForPathAsync(String path, ContentType service)
        {
            IPlayableItem item = null;
            if (services.ContainsKey(service))
            {
                item = await services[service].GetTrackWithPathAsync(path).ConfigureAwait(false);
            }
            else
            {
                IARLogStatic.Info("ServiceManager", $"Doesn't support path yet : {path}");
            }
            return item;
        }

        public IStreamingTrack GetItemForPath(String path, ContentType service)
        {
            IStreamingTrack item = null;
            if (services.ContainsKey(service))
            {
                item = services[service].GetTrackWithPath(path);
            }
            else
            {
                IARLogStatic.Info("ServiceManager", $"Doesn't support path yet : {path}");
            }
            return item;
        }

        internal static Bugs.SSBugs Bugs()
        {
            return (Bugs.SSBugs)  it[ContentType.Bugs];
        }
        internal static Melon.SSMelon Melon()
        {
            return (Melon.SSMelon)  it[ContentType.Melon];
        }
    }


    public static class IStreamingTypeUtility
    {
        public static IStreamingService GetService(this IStreamingServiceObject serviceObj)
        {
            IStreamingService service = ServiceManager.it[serviceObj.ServiceType];

            return service;
        }
    }
}
