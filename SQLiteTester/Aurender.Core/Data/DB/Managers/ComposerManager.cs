using System;
using System.Collections.Generic;
using Aurender.Core.Contents;
using Aurender.Core.Data.DB.Queries;
using Aurender.Core.Data.DB.Managers.SubManagers;

namespace Aurender.Core.Data.DB.Managers
{
    public class ComposerManager : AbstractManager<IComposerFromDB>, IComposerManager
    {
        public ComposerManager(IDB db) : base(db)
        {
            QueryFactoryForAll<IComposerFromDB> qf = new QueryFactoryForComposer();

            this.cursor = new Windowing.WindowedData<IComposerFromDB>(this.db, qf, EViewType.Composers, db.popupDelegate, x => new Composer(x));
            LoadTotalItemCount();
            
            this.Y = typeof(Composer);
        }

        public IDataManagerForSearchResult<ISongFromDB, IComposerFromDB> GetAlbumsByArtist(IComposerFromDB artist)
        {
            if (!db.IsOpen())
            {
                return null;
            }

            var manager = new AlbumWithSongManagerByArtist<IComposerFromDB>(db, artist);

            return manager;
        }

        public IComposerFromDB GetComposerByID(int artistID)
        {
            //song_id, artistsNames, album, indexOfSong, song_title, duration, totalTracks, track_id, a.songRate, a.f2 "
            String query = "select composer, composer_id, albumsAsComposer as albumCount, songsAsComposer as songCount, composer_key, coverAsComposer from composers where composer_id = ?";

            return GetComposer(artistID, query);
        }

        public IComposerFromDB GetComposerBySongID(int songID)
        {
            String query = "select composer, composer_id, albumsAsComposer as albumCount, songsAsComposer as songCount, composer_key, coverAsComposer from composers where composer_id = (select composerID from songs where song_id = ?)";

            return GetComposer(songID, query);
        }

        private IComposerFromDB GetComposer(int param, string query)
        {
            IComposerFromDB composer = null;
            if (db.IsOpen())
            {

                using (var con = this.db.CreateConnection())
                {
                    var cmd = con.CreateCommand(query, param);


                    try
                    {
                        var result = cmd.ExecuteDeferredQuery(QueryFactoryForArtist.types);

                        foreach (var obj in result)
                        {
                            composer = new Composer(obj);

                            break;
                        }

                    }
                    catch (Exception ex)
                    {
                        IARLogStatic.Error("ComposerManager", $"Failed to excute : {ex.Message}", ex);
                    }
                }
            }

            return composer;
        }

        public IList<string> GetComposerSugestion(string userInput, int count = 10)
        {
            String query = "select composer from composers where composer like ? limit ?";
            String pattern = $"%{userInput}%";

            return GetSugestion(this.db, query, pattern, count);
        }
    }
}
