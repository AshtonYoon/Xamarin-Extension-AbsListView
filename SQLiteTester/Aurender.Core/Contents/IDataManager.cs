using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Aurender.Core.Contents
{
    [Newtonsoft.Json.JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
    public enum Ordering
    {
        Default,
        AlbumName,
        ArtistName,
        AritstNameAndReleaseYear,
        ArtistNameAndAlbumName,
        SongName,
        PlaylistName,
        ReleaseYear,
        ReleaseYearDesc,
        ReleaseDate,
        ReleaseDateDesc,
        AddedDate,
        AddedDateDesc,
    }

    public struct SongMeta
    {
        public String FileFormat;
        public String CODEC;
        public Int32 SamplingRate;
        public Int32 BitWidth;
        public Int32 Bitrate;
        public string FileSize;
    }


    public interface ISortable
    {
        List<Ordering> SupportedOrdering();
        Task OrderBy(Ordering ordering);

        Ordering CurrentOrder { get; }
    }

    public interface IDataManager<out T> : IReadOnlyList<T>, ISortable where T : IDatabaseItem
    {
        int TotalItemCount { get; }
        DataFilter Filter { get; }
        List<String> Sections { get; }
        List<Int32> ItemCountsPerSection { get; }

        String Summary();
        void ReplaceDB(IDB db);

        void CheckDataAvailability();
        String KeyForSection(Int32 section);

        void ReloadData();

        void FilterWith(DataFilter filter);

        T ItemAt(Int32 section, Int32 index);

        Task<int> IndexOfItem(object item);

        event EventHandler OnDataRefreshed;
        
        IReadOnlyList<T> GetRange(int index, int count);

        IEnumerable<IGrouping<String, T>> DataForUI();
    }
    
    public interface IDataManagerForSearchResult<TElement, out TQueryItem> : IDataManager<TElement> where TElement : IDatabaseItem
    {
        TQueryItem QueryItem { get; }
        Int32 AlbumCount { get; }
        IAlbumFromDB AlbumAtIndex(int index);
    }
    
    public interface ISongManager : IDataManager<ISongFromDB>
    {
        IPlayableItem PlayableItemByPath(String path);
        String FilePathBySongID(int songID);
        String GetLyrics(int songID);

        IInformationAvailablity GetAvailability(ISongFromDB song);
        IInformationAvailablity GetAvailabilityByPath(String filepath);
        IInformationAvailablity GetAvailabilityByTrackID(int trackID);
        string GetLyricsByFilePath(string filePath);

        SongMeta GetSongMeta(int songID);
    }

    public interface IArtistManager : IDataManager<IArtistFromDB>
    {
        IArtistFromDB GetArtistByID(int artistID);
        IArtistFromDB GetAlbumArtistByAlbumID(int albumID);
        IArtistFromDB GetArtistBySongID(int songID);
        IDataManagerForSearchResult<ISongFromDB, IArtistFromDB> GetAlbumsByArtist(IArtistFromDB artist);

        IList<String> GetArtistSugestion(String userInput, int count = 10);
    }

    public interface IComposerManager : IDataManager<IComposerFromDB>
    {
        IComposerFromDB GetComposerByID(int artistID);
        IComposerFromDB GetComposerBySongID(int songID);
        IDataManagerForSearchResult<ISongFromDB, IComposerFromDB> GetAlbumsByArtist(IComposerFromDB artist);
        IList<String> GetComposerSugestion(String userInput, int count = 10);
    }
    public interface IConductorManager : IDataManager<IConductorFromDB>
    {
        IConductorFromDB GetConductorByID(int artistID);
        IConductorFromDB GetConductorBySongID(int songID);
        IDataManagerForSearchResult<ISongFromDB, IConductorFromDB> GetAlbumsByArtist(IConductorFromDB artist);
        IList<String> GetConductorSugestion(String userInput, int count = 10);
    }

    public interface IGenreManager : IDataManager<IGenreFromDB>
    {
        IGenreFromDB GetGenreByID(int genreID);
        IGenreFromDB GetGenreBySongID(int songID);
        IGenreFromDB GetGenreByTrackID(int trackID);

        int GetGenreIDBySongID(int songID);
        int GetGenreUDByTrackID(int trackID);

        IDataManagerForSearchResult<ISongFromDB, IGenreFromDB> GetAlbumsByGenre(IGenreFromDB genre);

        IList<String> GetGenreSugestion(String userInput, int count = 10);
    }

    public interface IAlbumManager : IDataManager<IAlbumFromDB>
    {
        IAlbumFromDB GetAlbumByID(int albumID);
        IAlbumFromDB GetAlbumByFilePath(String path);

        object GetImageBySongID(int songID);
        object GetImageByFilePath(String filePath);
        String GetLargeSizeFrontCoverURLBySongID(int songID);
        String GetLargeSizeFrontCoverURLByFilePath(String filePath);
        String GetLargeSizeFrontCoverURLByAlbumID(int albumID);
        String GetLargeSizeBackCoverURLBySongID(int songID);
        String GetLargeSizeBackCoverURLByFilePath(String filePath);
        String GetLargeSizeBackCoverURLByAlbumID(int albumID);

        /// <summary>
        /// imageIndex 1: front, 2: back, 3: etc
        /// </summary>
        /// <param name="albumID"></param>
        /// <param name="imageIndex"> 0: front, 1: back, 2: etc</param>
        /// <returns></returns>
        bool HasImageForAlbumID(int albumID, int imageIndex);
        /// <summary>
        ///  imageIndex 0: front, 1: back, 2: etc
        /// </summary>
        /// <param name="songID"></param>
        /// <param name="imageIndex"> 0: front, 1: back, 2: etc</param>
        /// <returns></returns>
        bool HasImageForSongID(int songID, int imageIndex);
        /// <summary>
        ///  imageIndex 0: front, 1: back, 2: etc
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="imageIndex"> 0: front, 1: back, 2: etc</param>
        /// <returns></returns>
        bool HasImageForFilePath(String filePath, int imageIndex);
    }
}
