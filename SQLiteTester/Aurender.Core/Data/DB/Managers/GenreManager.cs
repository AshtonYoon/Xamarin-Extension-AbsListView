using System;
using System.Collections.Generic;
using Aurender.Core.Contents;
using Aurender.Core.Data.DB.Queries;
using Aurender.Core.Data.DB.Managers.SubManagers;

namespace Aurender.Core.Data.DB.Managers
{

    public class GenreManager : AbstractManager<IGenreFromDB>, IGenreManager
    {
        public GenreManager(IDB db) : base(db)
        {
            var qf = new Queries.QueryFactoryForGenre();
            this.cursor = new Windowing.WindowedData<IGenreFromDB>(this.db, qf, EViewType.Genres, db.popupDelegate, x => new Genre(x));
            LoadTotalItemCount();

            this.Y = typeof(Genre);
        }

        public IDataManagerForSearchResult<ISongFromDB, IGenreFromDB> GetAlbumsByGenre(IGenreFromDB genre)
        {
            
            
            if (!db.IsOpen())
            {
                return null;
            }
           
            var manager = new AlbumWithSongManagerByGenres(db, genre);

            return manager;

        }

        public IGenreFromDB GetGenreByID(int genreID)
        {
            //song_id, artistsNames, album, indexOfSong, song_title, duration, totalTracks, track_id, a.songRate, a.f2 "
            String query = "select genre_id, genre, albumCount, songCount, cover_m from audio_genres where genre_id = ?";
            IGenreFromDB genre = GetGenreByQuery(query, genreID);

            return genre;
        }

        public IGenreFromDB GetGenreBySongID(int songID)
        {
            //song_id, artistsNames, album, indexOfSong, song_title, duration, totalTracks, track_id, a.songRate, a.f2 "
            String query = "select genre_id, genre, albumCount, songCount, cover_m from audio_genres where genre_id = (select genre_id from songs where song_id = ?)";
            IGenreFromDB genre = GetGenreByQuery(query, songID);

            return genre;
        }

        public IGenreFromDB GetGenreByTrackID(int trackID)
        {
            //song_id, artistsNames, album, indexOfSong, song_title, duration, totalTracks, track_id, a.songRate, a.f2 "
            String query = "select genre_id, genre, albumCount, songCount, cover_m from audio_genres where genre_id = (select genre_id from songs where song_id = (select song_id from tracks where track_id = ?))";
            IGenreFromDB genre = GetGenreByQuery(query, trackID);

            return genre;
        }

        public int GetGenreIDBySongID(int songID)
        {
            String query = "select genre_id where genre_id = (select genre_id from songs where song_id = ?)";
            int genreID = this.ExecuteQueryForInt(songID, query);

            return genreID;
        }

        public int GetGenreUDByTrackID(int trackID)
        {
            String query = "select genre_id where genre_id = (select genre_id from songs where song_id = (select song_id from tracks where track_id = ?))";
            int genreID = this.ExecuteQueryForInt(trackID, query);

            return genreID;
        }

        private IGenreFromDB GetGenreByQuery(String query, Object parameter)
        {
            Genre genre = null;
            if (!db.IsOpen())
            {
                return null;
            }
          

            using (var con = this.db.CreateConnection())
            {

                var cmd = con.CreateCommand(query, parameter);


                try
                {
                    var result = cmd.ExecuteDeferredQuery(QueryFactoryForGenre.types);

                    foreach (var obj in result)
                    {
                        genre = new Genre(obj);

                        break;
                    }

                }
                catch (Exception ex)
                {
                    IARLogStatic.Error("AlbumManager", $"Failed to excute : {ex.Message}", ex);
                }
            }
            return genre;
        }

        public IList<string> GetGenreSugestion(string userInput, int count = 10)
        {
                     String query = "select genre from audio_genres where genre like ? limit ?";
            String pattern = $"%{userInput}%";

            return GetSugestion(this.db, query, pattern, count);
        }


        /*
                public override List<Ordering> SupportedOrdering()
                {
                    return new List<Ordering>()
                    {
                        Ordering.AlbumName,
                        Ordering.ReleaseYear,
                        Ordering.ReleaseYearDesc,
                        Ordering.AritstNameAndReleaseYear,
                        Ordering.AddedDate,
                    };
                }

                private QueryFactoryForGenre queryFactoryForAlbum
                {
                    get => (QueryFactoryForGenre)this.cursor.queryFactory;
                }

                internal override QueryFactoryForAll<Genre> GetQueryFactoryForOrdering(Ordering ordering)
                {
                    QueryFactoryForAll<Genre> result = null;
                    switch (ordering)
                    {
                        case Ordering.AlbumName:
                            result = new QueryFactoryForAlbumOrderByArtistNameAndReleaseYear();
                            break;

                        case Ordering.ReleaseYear:
                            result = new QueryFactoryForAlbumOrderByReleaseYear();
                            break;

                        case Ordering.ReleaseYearDesc:
                            result = new QueryFactoryForAlbumOrderByReleaseYearDesc();
                            break;

                        case Ordering.AritstNameAndReleaseYear:
                            break;

                        case Ordering.AddedDate:
                            result = new QueryFactoryForAlbumOrderByAddedDate();
                            break;


                        default:
                            throw new NotImplementedException();
                            break;
                    }

                    return result;
                }
                */

    }

}