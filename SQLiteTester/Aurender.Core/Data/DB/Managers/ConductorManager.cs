using System;
using System.Collections.Generic;
using Aurender.Core.Contents;
using Aurender.Core.Data.DB.Queries;
using Aurender.Core.Data.DB.Managers.SubManagers;

namespace Aurender.Core.Data.DB.Managers
{
    public class ConductorManager : AbstractManager<IConductorFromDB>, IConductorManager
    {
        public ConductorManager(IDB db) : base(db)
        {
            QueryFactoryForAll<IConductorFromDB> qf = new QueryFactoryForConductor();

            this.cursor = new Windowing.WindowedData<IConductorFromDB>(this.db, qf, EViewType.Conductors, db.popupDelegate, x => new Conductor(x));
            LoadTotalItemCount();
            
            this.Y = typeof(Conductor);
        }

        public IDataManagerForSearchResult<ISongFromDB, IConductorFromDB> GetAlbumsByArtist(IConductorFromDB artist)
        {
            if (!db.IsOpen())
            {
                return null;
            }

            var manager = new AlbumWithSongManagerByArtist<IConductorFromDB>(db, artist);

            return manager;
        }

        public IConductorFromDB GetConductorByID(int artistID)
        {
            //song_id, artistsNames, album, indexOfSong, song_title, duration, totalTracks, track_id, a.songRate, a.f2 "
            String query = "select conductor, conductor_id, albumsAsConductor as albumCount, songsAsConductor as songCount, conductor_key, coverAsConductor from conductors where conductor_id = ?";
            return GetConductor(artistID, query);
        }

        private IConductorFromDB GetConductor(int param, string query)
        {
            IConductorFromDB conductor = null;
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
                            conductor = new Conductor(obj);

                            break;
                        }
                    }
                    catch (Exception ex)
                    {
                        IARLogStatic.Error("ConductorManager", $"Failed to excute : {ex.Message}", ex);
                    }
                }
            }

            return conductor;
        }

        public IConductorFromDB GetConductorBySongID(int songID)
        {
            String query = "select conductor, conductor_id, albumsAsConductor as albumCount, songsAsConductor as songCount, conductor_key, coverAsConductor from conductors where conductor_id = (select conductor_id from songs where song_id = ? )";
            return GetConductor(songID, query);
        }

        public IList<string> GetConductorSugestion(string userInput, int count = 10)
        {
                       String query = "select conductor from conductors where conductor like ? limit ?";
            String pattern = $"%{userInput}%";

            return GetSugestion(this.db, query, pattern, count);

        }
    }
}
