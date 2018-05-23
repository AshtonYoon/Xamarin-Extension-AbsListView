using System;
using System.Collections.Generic;
using Aurender.Core.Contents;
using Aurender.Core.Data.DB.Queries;
using Aurender.Core.Data.DB.Managers.SubManagers;

namespace Aurender.Core.Data.DB.Managers
{
    public class ArtistManager : AbstractManager<IArtistFromDB>, IArtistManager
    {
        public ArtistManager(IDB db) : base(db)
        {
            var qf = new Queries.QueryFactoryForArtist();
            this.cursor = new Windowing.WindowedData<IArtistFromDB>(this.db, qf, EViewType.Artists, db.popupDelegate, x => new Artist(x));
            LoadTotalItemCount();

            this.Y = typeof(Artist);
        }

        public IArtistFromDB GetArtistByID(int artistID)
        {
            //song_id, artistsNames, album, indexOfSong, song_title, duration, totalTracks, track_id, a.songRate, a.f2 "
            String query = "select artist, artist_id, albumsAsArtist as albumCount, songsAsArtist as songCount, artist_key, coverAsArtist from artists where artist_id = ?";

            return GetArtist(artistID, query);
        }

        public IArtistFromDB GetAlbumArtistByAlbumID(int albumID)
        {
            String query = "select artist, artist_id, albumsAsArtist as albumCount, songsAsArtist as songCount, artist_key, coverAsArtist from artists where artist_id = (select artist_id from albums where album_id = ?)";

            return GetArtist(albumID, query);
        }

        public IArtistFromDB GetArtistBySongID(int songID)
        {
            String query = "select artist, artist_id, albumsAsArtist as albumCount, songsAsArtist as songCount, artist_key, coverAsArtist from artists where artist_id = (select artist_id from songs where song_id = ? )";

            return GetArtist(songID, query);
        }


        private IArtistFromDB GetArtist(int param1, string query)
        {
            IArtistFromDB artist = null;
            if (db.IsOpen())
            {
                using (var con = this.db.CreateConnection())
                {

                    var cmd = con.CreateCommand(query, param1);


                    try
                    {
                        var result = cmd.ExecuteDeferredQuery(QueryFactoryForArtist.types);

                        foreach (var obj in result)
                        {
                            artist = new Artist(obj);

                            break;
                        }

                    }
                    catch (Exception ex)
                    {
                        IARLogStatic.Error("ArtistManager", $"Failed to excute : {ex.Message}", ex);
                    }
                }
            }

            return artist;
        }

        public IDataManagerForSearchResult<ISongFromDB, IArtistFromDB> GetAlbumsByArtist(IArtistFromDB artist)
        {
           if (!db.IsOpen())
            {
                return null;
            }
          
            var manager = new AlbumWithSongManagerByArtist<IArtistFromDB>(db, artist);

            return manager;
        }

        public IList<string> GetArtistSugestion(string userInput, int count = 10)
        {
            String query = "select artist from artists where artist like ? limit ?";
            String pattern = $"%{userInput}%";

            return GetSugestion(this.db, query, pattern, count);
        }
    }
}
