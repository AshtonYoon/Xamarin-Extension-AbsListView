using System;
using System.Collections.Generic;
using Aurender.Core.Contents;
using Aurender.Core.Data.DB.Queries.SubQueries;
using Aurender.Core.Data.DB.Windowing;

namespace Aurender.Core.Data.DB.Managers.SubManagers
{

    internal class AlbumWithSongManagerByGenres : AbstractManager<ISongFromDB>, ISongManager, IDataManagerForSearchResult<ISongFromDB, IGenreFromDB>
    {
        public AlbumWithSongManagerByGenres(IDB db, IGenreFromDB genre) : base(db)
        {
            this.QueryItem = genre;

            var qf = GetSubQueryFactory(Ordering.AritstNameAndReleaseYear);

            EViewType viewType = EViewType.Genres;

            this.cursor = new Windowing.WindowedDataForSearchResult<ISongFromDB>(this.db, qf, viewType, db.popupDelegate, x => new Song(x));

            this.Y = typeof(Song);
        }

        public IGenreFromDB QueryItem { get; protected set; }

        public Int32 AlbumCount => this.searchResult.AlbumsCount;

        public IAlbumFromDB AlbumAtIndex(int index)
        {
            IAlbumFromDB album = new Album();

            if (index < this.searchResult.AlbumsCount)
            {
                album = this.searchResult.Albums.ItemAt(index);
            }

            return album;
        }

        public override List<Ordering> SupportedOrdering()
        {
            return new List<Ordering>()
            {
                Ordering.AlbumName,
                Ordering.ArtistNameAndAlbumName,
                Ordering.AritstNameAndReleaseYear,
                Ordering.ReleaseYear,
                Ordering.ReleaseYearDesc,
            };
        }

        internal WindowedDataForSearchResult<ISongFromDB> searchResult => (WindowedDataForSearchResult<ISongFromDB>)this.cursor;


        internal ISubQueryFactory<ISongFromDB> GetSubQueryFactory(Ordering ordering)
        {
            ISubQueryFactory<ISongFromDB> result = null;

            switch (ordering)
            {

                case Ordering.AlbumName:
                    result = new SubQueryFactoryForSongsWithAlbumsByGenreOrderByAlbumTitle(this.QueryItem);
                    break;

                case Ordering.ReleaseYear:
                    result = new SubQueryFactoryForSongsWithAlbumsByGenreOrderByReleaseYear(this.QueryItem);
                    break;

                case Ordering.ReleaseYearDesc:
                    result = new SubQueryFactoryForSongsWithAlbumsByGenreOrderByReleaseYearDesc(this.QueryItem);
                    break;

                case Ordering.AritstNameAndReleaseYear:
                case Ordering.AddedDate:
                case Ordering.ArtistNameAndAlbumName:
                case Ordering.Default:
                    result = new SubQueryFactoryForSongsWithAlbumsByGenreOrderByArtistAndAlbumYear(this.QueryItem);
                    break;
                default:
                    break;
            }

            return result;

        }

        public IPlayableItem PlayableItemByPath(string path)
        {
            IARLogStatic.Error($"{this}", $"Doesn't support this");
            return null;
        }

        public string FilePathBySongID(int songID)
        {
            return string.Empty;
        }

        public string GetLyrics(int songID)
        {
            return string.Empty;
        }

        public IInformationAvailablity GetAvailability(ISongFromDB song)
        {
            throw new NotImplementedException();
        }

        public IInformationAvailablity GetAvailability(string filePath)
        {
            throw new NotImplementedException();
        }

        public IInformationAvailablity GetAvailabilityByTrackID(int trackID)
        {
            throw new NotImplementedException();
        }

        public IInformationAvailablity GetAvailabilityByPath(string filepath)
        {
            throw new NotImplementedException();
        }

        public string GetLyricsByFilePath(string filePath)
        {
            throw new NotImplementedException();
        }

        public SongMeta GetSongMeta(int songID)
        {
            throw new NotSupportedException(ToString());
        }
    }
}
