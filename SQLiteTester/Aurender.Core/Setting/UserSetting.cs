using Acr.Settings;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Aurender.Core.Setting
{
    public class UserSetting
    {
        private const string KEY_FOR_APP      = "AurenderApp";
        private const string KEY_FOR_DEVICES  = "AurenderList";
        private const string KEY_FOR_SERVICES = "CacheForStream";

        static Lazy<UserSetting> instance = new Lazy<UserSetting>(() => new UserSetting());
        public static UserSetting Setting => instance.Value;

        ISettings settings = Settings.Current;
        public UserSetting()
        {
            this.App = settings.Get(KEY_FOR_APP, new ConfigForApp());
            this.Devices = settings.Get(KEY_FOR_DEVICES, new Dictionary<string, ConfigForDevice>());
            this.Services = settings.Get(KEY_FOR_SERVICES, new Dictionary<string, CacheForStreamingService>());

            DescribeSetting();
        }

        [Conditional("DEBUG")]
        private void DescribeSetting()
        {
            Debug.WriteLine("---------------------------------------------------------------");
            Debug.WriteLine("---------------------------APP---------------------------------");
            App.DescribeSelf();
            Debug.WriteLine("---------------------------------------------------------------");
            Debug.WriteLine("");
            Debug.WriteLine("---------------------------Devices-----------------------------");
            foreach (var device in Devices)
            {
                Debug.WriteLine($"\t-------------------------{device.Key} : {device.Value.Name}---------------------");
                device.Value.DescribeSelf();
                Debug.WriteLine("\t---------------------------------------------------------------");
            }
            Debug.WriteLine("---------------------------------------------------------------");
            Debug.WriteLine("");

            Debug.WriteLine("---------------------------Services----------------------------");
             foreach (var service in Services)
            {
                Debug.WriteLine($"\t-------------------------{service.Key}---------------------");
                service.Value.DescribeSelf();
                Debug.WriteLine("\t---------------------------------------------------------------");
            }
            Debug.WriteLine("---------------------------------------------------------------");
        }

        public ConfigForApp App;
        private Dictionary<String, ConfigForDevice> Devices;
        private Dictionary<String, CacheForStreamingService> Services;
     
        public void Clear()
        {
            App = new ConfigForApp();
            Devices = new Dictionary<string, ConfigForDevice>();
            Services = new Dictionary<string, CacheForStreamingService>();

            Save();
        }

        public void Save()
        {
            try
            {
                //Debug.WriteLine("-------------------Before save---------------------------------");
                //DescribeSetting();
                //Debug.WriteLine("---------------------------------------------------------------");
                settings.Set(KEY_FOR_APP, App);
                settings.Set(KEY_FOR_DEVICES, Devices);

                //TODO: handling error when save services
                settings.Set(KEY_FOR_SERVICES, Services);
            }
            catch(Exception ex)
            {
                IARLogStatic.Error("UserSetting", $"Failed to save settings.", ex);
            }
        }

        public CacheForStreamingService this[ContentType service]
        {
            get
            {
                String key = service.ToString();
                if (!Services.ContainsKey(key))
                {
                    this.Services[key] = new CacheForStreamingService(service);
                }

                return Services[key];
            }
        }

        public IList<ConfigForDevice> GetConfigsForConnectedDevices()
        {
            return Devices.Values.ToList();
        }

        public bool HasConfigForAurenderMAC(String aurenderMAC)
        {
            return Devices.ContainsKey(aurenderMAC);
        }

        public ConfigForDevice ConfigForAurenderMAC(String aurenderMAC)
        {
            if (Devices.ContainsKey(aurenderMAC))
            {
                return Devices[aurenderMAC];
            }

            var config = new ConfigForDevice(aurenderMAC);

            this.Devices[aurenderMAC] = config;

            return config;
        }

        public bool HasConfigForAurenderName(String aurenderName)
        {
            try
            {
                return Devices.Values.Any(x => x.Name == aurenderName);
            }
            catch
            {
                return false;
            }
        }

        public ConfigForDevice ConfigForAurenderByName(String aurenderName)
        {
            var config = Devices.Values.FirstOrDefault(x => x.Name == aurenderName);

            return config;
        }
    }
}
