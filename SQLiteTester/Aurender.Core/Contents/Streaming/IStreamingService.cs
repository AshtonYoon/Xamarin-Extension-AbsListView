using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aurender.Core.Player;

namespace Aurender.Core.Contents.Streaming
{
    public delegate void StreamingServiceEventHandler(IStreamingService sender);
    public delegate void StreamingServiceEventHandler<T>(IStreamingService sender, T t);
    public delegate void StreamingServiceEventHandler<T, U>(IStreamingService sender, T t, U u);

    public interface IStreamingService
    {
        ContentType ServiceType {get;}
        String Name {get;}
        String ServiceSiteURL {get;}
        String ServiceJoinURL {get;}

        HashSet<StreamingServiceFeatures> Capability {get;}

        bool IsLoggedIn {get;}

        String LogInMessage { get; }
        String SubscriptionInfo { get; }        

        IDictionary<StreamingServiceLoginInformation, String> ServiceInformation { get; }

        Task CheckServiceAndLoginIfAvailableAsync(IAurenderEndPoint aurender);
        void Logout();

        Task<Tuple<bool, String>> TryLoginAsync(IDictionary<StreamingServiceLoginInformation, String> information);
        
        String ArtistWithAlbumForTrack(IStreamingTrack track);


        IList<String> SupportedStreamingQuality { get; }
        IList<String> AvailableStreamingQuality { get; }
        String  SelectedStreamingQuality { get; set; }



        object IconFor(bool selected);
        object IconForSetting { get; }
        object IconForAddToMyLibraryProcessing { get; }
        object IconForQueue(bool isPlaying);

        object ImageForFavorite(bool isFavorite);

        IList<(string title, EViewType type)> TitlesForViewType();
 
        IList<String> TitlesForViewType(EViewType viewType);


        IStreamingObjectCollection<IStreamingTrack>    TracksForTitle(String title);
        IStreamingObjectCollection<IStreamingAlbum>    AlbumsForTitle(String title);
        IStreamingObjectCollection<IStreamingArtist>   ArtistsForTitle(String title);
        IStreamingObjectCollection<IStreamingGenre>    GenresForTitle(String title);
        IStreamingObjectCollection<IStreamingPlaylist> PlaylistsForTitle(String title);

        IList<String> TitlesForCollectionForGenre();

        IStreamingObjectCollection<U>    CollectionForGenreTitle<U>(String title) where U : IStreamingServiceObject ;

        Task SearchForAsync(EViewType viewType, String keyword, LoadAsyncCompletion callback = null);
        IStreamgingSearchResult<T> SearchResultForViewType<T>(EViewType viewType) where T : IStreamingServiceObject;




    
        /// <summary>
        /// Will return Track from cached contents.
        /// If there is no track in the cache, it will be scheduled for loading
        /// and will be notified through Notification with a loaded track.
        /// If you need to get track directly, you should use GetTrackWithPathAsync(filePath) instead.
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        IStreamingTrack GetTrackWithPath(String filePath);
        /// <summary>
        /// This will lookup cache then load from service.
        /// </summary>
        /// <param name="trackID"></param>
        /// <returns></returns>
        Task<IStreamingTrack> GetTrackWithPathAsync(String filePath);
        Task<IStreamingTrack> GetTrackWithIDAsync(String trackID);
        Task<IStreamingAlbum> GetAlbumWithIDAsync(String albumID);
        Task<IStreamingArtist> GetArtistWithIDAsync(String artistID);
        Task<IStreamingPlaylist> GetPlaylistWithIDAsync(String playlistID);



        Task<IStreamingPlaylist> CreatePlaylistAsync(String title, IList<IStreamingTrack> tracks, String description = "");
        Task<bool> DeletePlaylistAsync(IStreamingPlaylist playlist);
        Task<bool> UpdatePlaylistAsync(IStreamingPlaylist playlist, String newTitle, IList<IStreamingTrack> tracks, String description = "");

        void SaveStatus();

        void AddToCache(IStreamingTrack track);


        event StreamingServiceEventHandler OnServiceLoginStatusChanged;
        event StreamingServiceEventHandler<IStreamingTrack> OnStreamingTrackLoaded;
        event StreamingServiceEventHandler<String> GetMessageFromService;
        event StreamingServiceEventHandler<IStreamingFavoritable, bool> OnFavoriteItemStatusChanged;
        event StreamingServiceEventHandler<IStreamingFavoritable> OnFavoriteItemStatusChangeFailed;
    }

}