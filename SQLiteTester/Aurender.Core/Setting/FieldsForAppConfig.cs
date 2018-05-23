using Newtonsoft.Json;

namespace Aurender.Core.Setting
{
    [JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
    public enum FieldsForAppConfig
    {
        /// <summary>
        /// Dictionary<string, ConnectedAurender>
        /// </summary>
        LastConnectedAurender,

        /// <summary>
        /// List<KeyValuePair<Name, IP>>
        /// </summary>
        StaticallyAddedAurenders, 

        /// <summary>
        /// <see cref="Core.Player.OptionForAddPosition"/> : Song selection default behaviour
        /// </summary>
        DefaultActionForSelect,
        /// <summary>
        /// bool : whether play a sound when select a song to add
        /// </summary>
        PlaySoundForSelect,

        /// <summary>
        /// String : path to the UI, so we can restore UI
        /// </summary>
        LastUIPath,

        /// <summary>
        /// Dictionary<>
        /// </summary>
        LastFilterInformation, 

        /// <summary>
        /// bool
        /// </summary>
        RestoreLastFilter,

        /// <summary>
        /// bool
        /// </summary>
        DisplayAlbumsWithCover,


        /// <summary>
        /// int : 0 : cover, 1 : songs with cover
        /// </summary>
        ViewTypeForAlbumsOfArtists,
        /// <summary>
        /// int : 0 : cover, 1 : songs with cover
        /// </summary>
        ViewTypeForAlbums,

        /// <summary>
        /// Ordering enum
        /// </summary>
        LocalSortForAlbums,
      
        /// <summary>
        /// Ordering enum
        /// </summary>
        LocalSortForAlbumsByArtist,
        /// <summary>
        /// Ordering enum
        /// </summary>
        LocalSortForAlbumsByGenre,

        /// <summary>
        /// String, last connected SSID
        /// </summary>
        WiFi_LastSSID,       
        /// <summary>
        /// String last connected Router MAC
        /// </summary>
        WiFi_LastSSIDMAC, 

        /// <summary>
        /// bool : when add to my lib, set favorite also
        /// </summary>
        AddToMyLib_ShouldAddToFavorite,
        /// <summary>
        /// <see cref="bool"/> : To check display message for Should add to favorite too.
        /// </summary>
        AddToMyLib_HaveUsedBefore,

        /// <summary>
        /// <see cref="UI.Themes"/>
        /// </summary>
        CurrentTheme
    }

}
