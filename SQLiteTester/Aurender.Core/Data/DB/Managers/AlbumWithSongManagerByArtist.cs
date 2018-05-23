using System;
using System.Collections.Generic;
using Aurender.Core.Contents;
using Aurender.Core.Data.DB.Queries.SubQueries;
using Aurender.Core.Data.DB.Windowing;

namespace Aurender.Core.Data.DB.Managers.SubManagers
{

    public class AlbumWithSongManagerByArtist<U> : AbstractManager<ISongFromDB>, ISongManager, IDataManagerForSearchResult<ISongFromDB, U> where U : IArtistFromDB
    {
        public U QueryItem { get; protected set; }

        internal WindowedDataForSearchResult<ISongFromDB> searchResult => (WindowedDataForSearchResult<ISongFromDB>)this.cursor;

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
        
        internal AlbumWithSongManagerByArtist(IDB db, U artist) : base(db)
        {
            this.QueryItem = artist;

            var qf = GetSubQueryFactory(Ordering.AritstNameAndReleaseYear);

            EViewType viewType = GetViewType();

            this.cursor = new Windowing.WindowedDataForSearchResult<ISongFromDB>(this.db, qf, viewType, db.popupDelegate, x => new Song(x));

            this.Y = typeof(Song);
        }

        private EViewType GetViewType()
        {
            EViewType viewType;

            if (QueryItem is IComposerFromDB)
            {
                viewType = EViewType.Composers;
            }
            else if (QueryItem is IConductorFromDB)
            {
                viewType = EViewType.Conductors;
            }
            else if (QueryItem is IArtistFromDB)
            {
                viewType = EViewType.Artists;
            }
            else
            {
                throw new InvalidOperationException("Doesn't support other than Composer, Conductor and Artist ");
            }

            return viewType;
        }

        public override List<Ordering> SupportedOrdering()
        {
            return new List<Ordering>()
            {
                Ordering.AlbumName,
                Ordering.ReleaseYear,
                Ordering.ReleaseYearDesc,
            };
        }


        internal override IQueryFactory<ISongFromDB> GetQueryFactoryForOrdering(Ordering ordering)
        {
            IQueryFactory<ISongFromDB> qf = GetSubQueryFactory(ordering) ;

            return qf;
        }

        internal ISubQueryFactory<ISongFromDB> GetSubQueryFactory(Ordering ordering)
        {
            ISubQueryFactory<ISongFromDB> result = null;
            if (ordering == Ordering.AlbumName)
            {
                result = new SubQueryFactoryForhSongsWithAlbumsByArtistOrderByAlbumTitle(QueryItem);
            }
            else if (ordering == Ordering.ReleaseYearDesc)
            {
                result = new SubQueryFactoryForhSongsWithAlbumsByArtistOrderByReleaseYearDesc(QueryItem);
            }
            else
            {
                result = new SubQueryFactoryForSongsWithAlbumsByArtistOrderByReleaseYear(QueryItem);
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
