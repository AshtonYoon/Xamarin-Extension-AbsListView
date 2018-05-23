using Newtonsoft.Json;

namespace Aurender.Core.Setting
{
    [JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
    public enum FieldsForServiceCache
    {
        /// <summary>
        /// List<>
        /// </summary>
        CachedTracks,

        /// <summary>
        /// List<String>
        /// </summary>
        FavoriteTracks,
        /// <summary>
        /// List<String>
        /// </summary>
        FavoriteAlbums,
        /// <summary>
        /// List<String>
        /// </summary>
        FavoriteArtists,
        /// <summary>
        /// List<String>
        /// </summary>
        FavoritePlaylists,

        /// <summary>
        /// Integer64
        /// </summary>
        ETagsForFavoriteIDs,

        ServiceToken,

        OrderForFavoriteTracks,
        OrderForFavoritePlaylists,
        OrderForMyPlaylists,
        OrderForFavoriteArtists,
        OrderForFavoriteAlbums,
    }

}