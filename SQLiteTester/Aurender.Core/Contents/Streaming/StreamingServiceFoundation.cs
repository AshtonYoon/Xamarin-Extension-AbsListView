using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Aurender.Core.Player;
using Aurender.Core.Utility;

namespace Aurender.Core.Contents.Streaming
{
    public static class StreamingSerivceTypeMethods
    {
        static readonly List<String> Prefixes = new List<String>()
            {
                "file",
                "tidal",
                "qobuz",
                "bugs",
                "melon",
                "shout"
            };
        static readonly List<String> HTTPs = new List<String>() { "http", "https", "mms" };

        public static String GetName(this ContentType sType)
        {
            String name;

            switch (sType)
            {
                case ContentType.TIDAL:
                    name = "TIDAL";
                    break;
                case ContentType.Qobuz:
                    name = "Quboz";
                    break;
                case ContentType.Melon:
                    name = "멜론";
                    break;
                case ContentType.Bugs:
                    name = "벅스";
                    break;
                case ContentType.InternetRadio:
                    name = "Shoutcast";
                    break;

                case ContentType.Local:
                    name = "Aurender";
                    break;

                default:
                    name = "N/A";
                    break;
            }

            return name;
        }

        public static String StreamingIDFromPath(String path)
        {
            var matches = PlayableFile.RegexForStreamingID.Matches(path);

            if (matches.Count > 0)
            {
                String streamingID = matches[0].Groups[1].Value;
                return streamingID;
            }

            return String.Empty;
        }

        public static ContentType ServiceForPrefix(String path)
        {
            var matches = PlayableFile.RegexForStreamingPrefix.Matches(path);
            if (matches.Count > 0)
            {
                String lower = matches[0].Groups[1].Value;

                int index = Prefixes.IndexOf(lower);
                if (index < 0)
                {
                    if (HTTPs.Contains(lower))
                        return ContentType.InternetRadio;
                    else
                    {
                        IARLogStatic.Info($"ContentType", $"Looks like it is a new content type, please prepare for it. ---> [{lower}]");
                        return ContentType.Local;
                    }
                }
                else
                    return (ContentType)index;
            }
            else
                return ContentType.Local;
        }

        public static String StreamingPrefix(this ContentType sType)
        {
            return Prefixes[(int)sType];
        }

    }

    [Flags]
    public enum StreamingServiceFeatures
    {
        Dummy,

        Moods,

        FavoritePlaylist,
        FavoriteTrack,
        FavoriteAlbum,
        FavoriteArtist,

        RelatedArtistsForArtist,
        SimilarArtist,
        ArtistTopTracks,

        AlbumDetail,

        PlaylistSearch,
        PlaylistCreateion
    }
    public static class StreamItemTypeUtility
    {
        public static StreamingServiceFeatures FavoriteFeature(this StreamingItemType item)
        {
            switch (item)
            {
                case StreamingItemType.Album:
                    return StreamingServiceFeatures.FavoriteAlbum;
                case StreamingItemType.Aritst:
                    return StreamingServiceFeatures.FavoriteArtist;
                case StreamingItemType.Playlist:
                    return StreamingServiceFeatures.FavoritePlaylist;
                case StreamingItemType.Track:
                    return StreamingServiceFeatures.FavoriteTrack;
            }

            return StreamingServiceFeatures.Dummy;
        }
    }


    [Flags]
    public enum StreamingServiceItemCharacteristic
    {
        MQA,
        ForAdult,
        Premium,
        HighRes,

    }

    public enum StreamingServiceLoginInformation
    {
        UserID,
        UserPassword,
        LoginToken,
        StreamingQuality,
        SerivceType,
        MaximumQuality,
        SubscriptionInfo
    }
    public enum StreamingItemType
    {
        Track,
        Album,
        Aritst,
        Playlist,
        Genre
    }

    public interface IStreamingServiceObject
    {
        ContentType ServiceType { get; }
        String StreamingID { get; }
    }

    public interface IStreamingFavoritable : IStreamingServiceObject
    {
        StreamingItemType StreamingItemType { get; }
        bool IsFavorite { get; }

        void SetFavorite(bool favorite);
    }

    public class StreamingObjectDescription : Dictionary<DescriptionField, String>
    {
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder("Description for object\n");

            foreach (var kv in this)
            {

                sb.AppendFormat("\t [{0}] : {1}\n\n", kv.Key, kv.Value);
            }

            sb.AppendLine("---------------------------------------------------");
            return sb.ToString();
        }
    }

    public interface IStreamingServiceObjectWithImage
    {
        String ImageUrl { get; }
        String ImageUrlForMediumSize { get; }
        String ImageUrlForLargeSize { get; }
    }

    public enum ImageSize
    {
        Small, Medium, Large
    }

    public static class IStreamingServiceObjectWithImageExtension
    {
        public static object GetImage(this IStreamingServiceObjectWithImage contents, ImageSize imageSize = ImageSize.Medium)
        {
            var url = imageSize == ImageSize.Small
                ? contents.ImageUrl
                : (imageSize == ImageSize.Medium
                    ? contents.ImageUrlForMediumSize
                    : contents.ImageUrlForLargeSize);

            return url != null ? ImageUtility.GetImageSourceFromUri(new Uri(url)) : ImageUtility.GetDefaultAlbumCover();
        }
    }

    public delegate void LoadAsyncCompletion(bool success, String errorMessage);

    public interface IStreamingObjectCollection<out T>  : IReadOnlyList<T> where T : IStreamingServiceObject
    {
        String Title { get; }
        Int32 CountForLoadedItems { get; }
        Task LoadNextAsync();
        void Reset();

        IEnumerable<T> GetRange(int index, int count);

        Dictionary<string, List<IStreamingAlbum>> AlbumsByType { get; set; }
        //event EventHandler<IStreamingObjectCollection<T>> OnCollectionUpdated;
    }
    public interface IStreamingAlbumCollectionsForArtist <out T> : IStreamingObjectCollection<IStreamingAlbum>
    {
        IList<String> GetTypesOtherThanAlbum();
        IList<IStreamingAlbum> GetAlbumsByType(String type);
    }

    public interface IStreamgingSearchResult<T> : IStreamingObjectCollection<T> where T : IStreamingServiceObject

    {
        String Keyword { get; }

        Task SearchAsync(String keyword);
    }

    public interface IStreamingServiceManager
    {
        IEnumerable<IStreamingService> AvailableServices();

        IEnumerable<IStreamingService> LoggedInServices();

        IStreamingService this[ContentType type] { get; }

        void PrepareSearch(String keyword, EViewType viewType);

        IStreamingService CurrentService { get; }

        void SelectService(ContentType serviceToSelect);
        void CheckServicesFor(IAurenderEndPoint aurender);
        void SaveSettings();

        Task<IPlayableItem> GetItemForPathAsync(String path, ContentType type);
        IStreamingTrack GetItemForPath(String path, ContentType service);


        event StreamingServiceEventHandler OnServiceLoginStatusChanged;
        event StreamingServiceEventHandler<IStreamingTrack> OnStreamingTrackLoaded;
        event StreamingServiceEventHandler<String> GetMessageFromService;
        event StreamingServiceEventHandler<IStreamingFavoritable, bool> OnFavoriteItemStatusChanged;
        event StreamingServiceEventHandler<IStreamingFavoritable> OnFavoriteItemStatusChangeFailed;

    }
}
