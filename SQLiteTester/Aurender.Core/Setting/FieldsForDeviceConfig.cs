using Newtonsoft.Json;

namespace Aurender.Core.Setting
{
    [JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
    public enum FieldsForDeviceConfig
    {
        /// <summary>
        /// string
        /// </summary>
        Name,
        /// <summary>
        /// string
        /// </summary>
        LastIP,
        /// <summary>
        /// string
        /// </summary>
        DBVersion,
        /// <summary>
        /// string
        /// </summary>
        RateDBVersion,
        /// <summary>
        /// string
        /// </summary>
        MPDPassword,

        /// <summary>
        /// List<String>
        /// </summary>
        LastUsedPlaylistsToAdd,
      
        /// <summary>
        /// bool
        /// </summary>
        DoNotAskForTimeZoneChange,

        /// <summary>
        /// bool
        /// </summary>
        DoNotAskForUpgrade,
        /// <summary>
        /// bool
        /// </summary>
        DoNotAskForEncoding,
        /// <summary>
        /// string
        /// </summary>
        MAC,
    }

}