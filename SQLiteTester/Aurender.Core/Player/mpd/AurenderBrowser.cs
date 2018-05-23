using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tmds.MDns;

namespace Aurender.Core.Player.mpd
{
    public class AurenderBrowser : IAurenderDiscoverySerivce
    {
        static Lazy<AurenderBrowser> instance = new Lazy<AurenderBrowser>(() => new AurenderBrowser());
        public static IAurenderDiscoverySerivce Browser => instance.Value;

        public static Func<IAurender> GetCurrentAurender { get; set; }

        ServiceBrowser browser = new ServiceBrowser();

        private AurenderBrowser()
        {
            browser.ServiceAdded += Browser_ServiceAdded;
        }

        public IEnumerable<IAurenderEndPoint> DiscoveredAurenders => GetEndPoints(browser);

        public void StartDiscovery()
        {
            browser.StartBrowse("_aurender._tcp");
        }

        public void StopDiscovery()
        {
            browser.StopBrowse();
        }

        public async Task<IEnumerable<IAurenderEndPoint>> DiscoverAsync(int timeout = 1000)
        {
            if (browser.IsBrowsing)
            {
                browser.StopBrowse();
                await Task.Delay(1000);
            }

            browser.StartBrowse("_aurender._tcp", false);

            await Task.Delay(timeout);

            var endPoints = GetEndPoints(browser);
            int count = 0;
            try { count = endPoints.Count(); } catch { }
            IARLogStatic.Log("AurenderBrowser", $"Count: {count}");

            return endPoints;
        }

        IEnumerable<IAurenderEndPoint> GetEndPoints(ServiceBrowser browser)
        {
            var endPoints = from service in browser.Services
                            orderby service.Hostname
                            select GetEndPoint(service);

            return endPoints;
        }

        IAurenderEndPoint GetEndPoint(ServiceAnnouncement service)
        {
            string name = service.Hostname;
            string ip = service.Addresses.FirstOrDefault(x => x.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork).ToString();
            int port = service.Port;

            var endPoint = new AurenderEndPoint(name, ip, webPort: port);
            return endPoint;
        }

        private void Browser_ServiceAdded(object sender, ServiceAnnouncementEventArgs e)
        {
            IARLogStatic.Log("AurenderBrowser", $"Found: {e.Announcement.Hostname}");
        }
    }
}
