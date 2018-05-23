using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Acr.Settings;
using Newtonsoft.Json;
using System.Diagnostics;
using Aurender.Core.Contents.Streaming;
using Aurender.Core.Player;
using Aurender.Core.UI;

namespace Aurender.Core.Setting
{

    public enum StorageType
    {
        App,
        Device,
        Streaming
    }

    [JsonObject]
    [DebuggerDisplay("Storage[{typeof(T)}] NumberOfSetting: {this.Count:n0}")]
    public abstract class SettingStorage<T> : IPersistentAurenderInformation where T : struct
    {
        [JsonProperty]
        public Dictionary<String, object> storage;

        public SettingStorage()
        {
            storage = new Dictionary<String, object>();
        }

        public override bool Equals(object obj)
        {
            if (obj is SettingStorage<T> b)
            {
                return b.GetHashCode() == this.GetHashCode();
            }
            return false;
        }

        public override int GetHashCode()
        {
            return storage.GetHashCode();
        }

        public String GetStringOrDefault(T key, String defaultString = "")
        {
            return Get(key, defaultString);
        }

        public int GetIntOrDefault(T key, int defaultInt = -1)
        {
            return Get(key, defaultInt);
        }

        public bool GetBoolOrDefault(T key, bool defaultBool = false)
        {
            return Get(key, defaultBool);
        }

        [Conditional("DEBUG")]
        public void DescribeSelf()
        {
            Debug.WriteLine($"Storage[{typeof(T).ToString()}] NumberOfSetting: {this.storage.Count:n0}");

            foreach (var kv in this.storage)
            {
                Debug.WriteLine($"\t{kv.Key,20}");
                //Debug.WriteLine($"\t{kv.Key,20} : [{kv.Value.ToString()}]");
            }
            Debug.WriteLine("");
        }

        public U Get<U>(T key, U defaultObject)
        {
            string strKey = key.ToString();
            var type = typeof(U);

            if (this.storage.ContainsKey(strKey))
            {
                if (storage[strKey] is U castedObject)
                {
                    return castedObject;
                }
                else if (typeof(U) == typeof(Themes))
                {
                    string result = storage[strKey] as string;
                    Enum.TryParse(result, out Themes theme);

                    var temp = theme as object;
                    return (U)temp;
                }
                else if (type == typeof(HashSet<String>))
                {
                    Newtonsoft.Json.Linq.JArray saved = this.storage[strKey] as Newtonsoft.Json.Linq.JArray;
                    HashSet<String> result = defaultObject as HashSet<String>;

                    if (saved != null)
                        foreach (var t in saved)
                            result.Add(t.ToString());

                    return defaultObject;
                }
                else if (type == typeof(Dictionary<string, ConnectedAurender>))
                {
                    Newtonsoft.Json.Linq.JObject saved = this.storage[strKey] as Newtonsoft.Json.Linq.JObject;
                    Dictionary<string, ConnectedAurender> result = defaultObject as Dictionary<string, ConnectedAurender>;

                    if (saved != null)
                    {
                        defaultObject = saved.ToObject<U>();
                    }

                    return defaultObject;
                }
            }

            // when failed to get, returns the defalut object.
            return defaultObject;
        }

        protected Object this[String key]
        {
            get => this.storage[key.ToString()];
            set => this.storage[key.ToString()] = value;
        }
    }

    [JsonObject]
    public class ConfigForApp : SettingStorage<FieldsForAppConfig>
    {
        internal ConfigForApp() : base()
        {
        }
        public Object this[FieldsForAppConfig key]
        {
            get => this[key.ToString()];
            set => this[key.ToString()] = value;
        }

    }

    public class ConfigForDevice : SettingStorage<FieldsForDeviceConfig>
    {
        [JsonConstructor]
        internal ConfigForDevice(String deviceMAC) : base()
        {
            this.DeviceMAC = deviceMAC;
        }


        public override int GetHashCode()
        {
            return $"DCFor{DeviceMAC}".GetHashCode();
        }

        [JsonProperty]
        public readonly string DeviceMAC;

        [JsonIgnore]
        public String Name => (String)this[FieldsForDeviceConfig.Name];

        public Object this[FieldsForDeviceConfig key]
        {
            get => this.storage[key.ToString()];
            set => this.storage[key.ToString()] = value;
        }
    }

    public class CacheForStreamingService : SettingStorage<FieldsForServiceCache>
    {
        [JsonConstructor]
        internal CacheForStreamingService(ContentType service) : base()
        {
            ServiceName = service.ToString();
        }

        public override int GetHashCode()
        {
            return $"CacheFor{ServiceName}".GetHashCode();
        }

        [JsonProperty]
        public readonly String ServiceName;

        public Object this[FieldsForServiceCache key]
        {
            get => this.storage[key.ToString()];
            set => this.storage[key.ToString()] = value;
        }

        public LinkedList<IStreamingTrack> GetCachedTracks<T>() where T : IStreamingTrack
        {
            String strKey = FieldsForServiceCache.CachedTracks.ToString();

            if (this.storage.ContainsKey(strKey))
            {
                LinkedList<IStreamingTrack> list = new LinkedList<IStreamingTrack>();

                string json = this.storage[strKey] as string;
                if (json != null)
                {
                    try
                    {
                        List<T> items = JsonConvert.DeserializeObject<List<T>>(json);
                        if (items != null)
                            foreach (T i in items)
                            {
                                list.AddFirst(i);
                            }
                    }
                    catch (Exception ex)
                    {
                        IARLogStatic.Error("Settings cached", $"Failed to load cached tracks for {ServiceName}", ex);
                    }
                }
                return list;
            }
            else
            {
                return new LinkedList<IStreamingTrack>();
            }
        }

        public void SetCashedTracks(LinkedList<IStreamingTrack> items)
        {
            string json = JsonConvert.SerializeObject(items);
            this[FieldsForServiceCache.CachedTracks] = json;
            //Debug.WriteLine($"{this[FieldsForServiceCache.CachedTracks]}");
        }
    }

}