using System;
using Aurender.Core.Contents;
using System.Text.RegularExpressions;
using Aurender.Core.Player.mpd;

namespace Aurender.Core.Data.DB.Managers
{
    public class SongManager : AbstractManager<ISongFromDB>, ISongManager
    {
        public SongManager(IDB db) : base(db)
        {
            var qf = new Queries.QueryFactoryForSong();
            this.cursor = new Windowing.WindowedDataForSong(this.db, qf, EViewType.Songs, db.popupDelegate, x => new Song(x));
            LoadTotalItemCount();
            
            this.Y = typeof(Song);
        }

        public IPlayableItem PlayableItemByPath(String path)
        {
            IPlayableItem song = null;

            if (!db.IsOpen())
            {
                return null;
            }
            //song_id, artistsNames, album, indexOfSong, song_title, duration, totalTracks, track_id, a.songRate, a.f2 "
            String query = "select s.album_id, s.song_id, a.track_id, album,  mediaIndex, songIndex, indexOfSong, "
                                  + "song_title, track_title, s.artistsNames, a.fileName, a.duration, b.artistNames as albumArtist, discIndex, s.songRate, s.f2 "
                                  + ", bitrate, containerformat, bitwidth, size"
                                  + "  from tracks a join songs s on a.song_id = s.song_id left join albums b on s.album_id = b.album_id "
                                  + " inner join track_meta tm on a.track_id = tm.track_id "
                                  + " where a.fileName = ? ";

            using (var con = this.db.CreateConnection())
            {
                var cmd = con.CreateCommand(query, path);

                try
                {
                    var result = cmd.ExecuteDeferredQuery(PlayingTrackFromDB.Types);

                    foreach (var obj in result)
                    {
                        song = new PlayingTrackFromDB(obj);

                        break;
                    }
                }
                catch (Exception ex)
                {
                    IARLogStatic.Error("SongManager", $"DB doesn't have {path}", ex);
                }
            }

            return song;
        }

        public String FilePathBySongID(int songID)
        {
            String query = "select fileName from tracks where song_id = ?";

            String path = ExecuteQueryForString(songID, query);
            return path;
        }

        public String GetLyrics(int songID)
        {
            String query = "select lyrics from track_meta where track_id = (select track_id from tracks where song_id = ?)";

            String lyrics = ExecuteQueryForString(songID, query);

            return lyrics;
        }

        public String GetLyricsByFilePath(String filePath)
        {
            String query = "select lyrics from track_meta where track_id = (select track_id from tracks where song_id = (select song_id from tracks where fileName = ?))";

            String lyrics = ExecuteQueryForString(filePath, query);

            return lyrics;
        }


        public IInformationAvailablity GetAvailability(ISongFromDB song) {
            String query = "select t.track_id, s.song_id, a.album_id, at.artist_id, cp.composer_id, cd.conductor_id, g.genre_id, t.fileName "
                + "    from tracks t left outer join songs s on t.song_id = s.song_id "
                + "                  left outer join albums  a on s.album_id = a.album_id "
                +        "                  left outer join artists at on s.artist_id = at.artist_id "
                +        "                  left outer join composers cp on s.composerID = cp.composer_id "
                +        "                  left outer join conductors cd on s.conductor_id = cd.conductor_id"
                +        "                  left outer join audio_genres g on a.genre_id = g.genre_id "
                +        "    where t.song_id = ?";


            IInformationAvailablity result = GetAvailability(query, song.dbID);
            return result;

        }


        public IInformationAvailablity GetAvailabilityByTrackID(int trackID) {
            String query = "select t.track_id, s.song_id, a.album_id, at.artist_id, cp.composer_id, cd.conductor_id, g.genre_id, t.fileName "
                + "    from tracks t left outer join songs s on t.song_id = s.song_id "
                + "                  left outer join albums  a on s.album_id = a.album_id "
                +        "                  left outer join artists at on s.artist_id = at.artist_id "
                +        "                  left outer join composers cp on s.composerID = cp.composer_id "
                +        "                  left outer join conductors cd on s.conductor_id = cd.conductor_id"
                +        "                  left outer join audio_genres g on a.genre_id = g.genre_id "
                +        "    where t.track_id = ?";


            IInformationAvailablity result = GetAvailability(query, trackID);
            return result;

        }

        public IInformationAvailablity GetAvailabilityByPath(String path) {
            String query = "select t.track_id, s.song_id, a.album_id, at.artist_id, cp.composer_id, cd.conductor_id, g.genre_id, t.fileName "
                       + "    from tracks t left outer join songs s on t.song_id = s.song_id "
                + "                  left outer join albums  a on s.album_id = a.album_id "
                +        "                  left outer join artists at on s.artist_id = at.artist_id "
                +        "                  left outer join composers cp on s.composerID = cp.composer_id "
                +        "                  left outer join conductors cd on s.conductor_id = cd.conductor_id"
                +        "                  left outer join audio_genres g on a.genre_id = g.genre_id "
                +        "    where t.fileName = ?";


            IInformationAvailablity result = GetAvailability(query, path);
            return result;

        }

        public SongMeta GetSongMeta(int songID)
        {
            String query = "select containerFormat, codec, bitWidth, sampleRate, bitrate, size from track_meta where track_id = (select track_id from songs where song_id = ?)";
            SongMeta meta = new SongMeta();

            if (db.IsOpen())
            {
                using (var con = this.db.CreateConnection())
                {
                    var cmd = con.CreateCommand(query, songID);
                    try
                    {
                        var result = cmd.ExecuteDeferredQuery(SongManager.InfoTypesForTrackMeta);

                        foreach (var data in result)
                        {
                            if (data.Count != SongManager.InfoTypesForTrackMeta.Length)
                            {
                                throw new Exception("Wrong query");
                            }
                            meta.FileFormat = data[0] as String;
                            meta.CODEC = data[1] as String;
                            meta.BitWidth = (Int32)data[2];
                            meta.SamplingRate = (Int32)data[3];
                            meta.Bitrate = (Int32)data[4];
                            meta.FileSize = (string)data[5];
                            break;
                        }
                    }
                    catch (Exception ex)
                    {
                        IARLogStatic.Error("SongManager", $"Failed to excute : {ex.Message}", ex);
                    }
                }
            }
            return meta;
        }
        static internal readonly Type[] InfoTypesForTrackMeta = new Type[]
{
                typeof(String), //container
            typeof(String), //codec
            typeof(int), //bitwidth
            typeof(int), //sampleRate
            typeof(int), //bitrate
            typeof(string), //size


};
        static internal readonly Type[] InfoTypes = new Type[]
     {
            typeof(int), //track_id
            typeof(int), //song_id
            typeof(int), //albumID
            typeof(int), //artist_id
            typeof(int), //composer_id
            typeof(int), //conductor_id
            typeof(int), //genre_id
            typeof(String), //song_title
     };
        private IInformationAvailablity GetAvailability(String query, object param) 
        {
            IInformationAvailablity info = IInformationAvailablity.NONE;

            if (db.IsOpen())
            {
                
                using (var con = this.db.CreateConnection())
                {
                    var cmd = con.CreateCommand(query, param);

                    try
                    {
                        var result = cmd.ExecuteDeferredQuery(SongManager.InfoTypes);


                        foreach (var data in result)
                        {
                            
                            if (data.Count != 8 ) {
                                throw new Exception("Wrong query");
                            }

                            String fileName = data[7].ToString();
                            Regex regex = new Regex(@"(^[a-z]*://)");

                            if (!regex.IsMatch(fileName)) {
                                info |= IInformationAvailablity.TRACK;
                            }

                            if (data[1] != null ) {
                                info |= IInformationAvailablity.SONG;
                            }
                            if (data[2] != null ) {
                                info |= IInformationAvailablity.ALBUM;
                            }
                            if (data[3] != null ) {
                                info |= IInformationAvailablity.ARTIST;
                            }
                            if (data[4] != null ) {
                                info |= IInformationAvailablity.COMPOSER;
                            }
                            if (data[5] != null ) {
                                info |= IInformationAvailablity.CONDUCTOR;
                            }
                            if (data[6] != null ) {
                                info |= IInformationAvailablity.GENRE;
                            }
                            
                            break;
                        }

                    }
                    catch (Exception ex)
                    {
                        IARLogStatic.Error("SongManager", $"Failed to excute : {ex.Message}", ex);
                    }
                }
            }

            return info;
        }

        internal void UpdateRating(IRatableDBItem song, int newRating)
        {
            if (song.Rating == newRating) return;

            var songPath = this.FilePathBySongID(song.dbID);

            /// update local rate db.
            var pKeyForRating = UpdateLocalDB(songPath);
            /// update local DB
            UpdateLocalDBRating(song.dbID, newRating);
            /// update cached data
            UpdateCursorForRating(song, newRating);
            /// ask aurender to update.

            if (AurenderBrowser.GetCurrentAurender() is AurenderPlayer aurender)
            {
                aurender.UpdateRating(songPath, pKeyForRating, newRating);
            }
            else
            {
                this.EP("[SongRate]", "Aurender is not connected");
            }
            song.Rating = (byte) newRating;

            NotifyRatingChange(song);
        }

        private void NotifyRatingChange(IRatableDBItem song)
        {
            ///TODO : Needs to notify UI
            if (AurenderBrowser.GetCurrentAurender() is AurenderPlayer aurender)
            {
                aurender.CallRatingUpdated(new Tuple<byte, int>(song.Rating, song.dbID));
            }
        }

        private void UpdateCursorForRating(IRatableDBItem song, int newRating)
        {
            Windowing.WindowedDataForSong songCursor = this.cursor as Windowing.WindowedDataForSong;

            songCursor.UpdateRating(song, newRating);            
        }

        private void UpdateLocalDBRating(int songID, int newRating)
        {
            String query = "update songs set songrate = ? where song_id = ?";

            int updatedCount = ExecuteQuery(query, newRating, songID);

            if (updatedCount == 0)
            {
                this.E("[SongRate]Failed to update songRate for local DB");
            }
        }

        private (String pKey, bool isUpdate) UpdateLocalDB(String songPath)
        {
            (String pKey, bool isUpdate) result;

            String query = "select pKey from track_rate where path = ?";
            

            String str = String.Empty;
            if (db.IsOpen())
            {
                using (var con = this.db.CreateRatingConnection())
                {
                    var cmd = con.CreateCommand(query, songPath);
                    try
                    {
                        str = cmd.ExecuteScalar<String>();
                    }
                    catch (Exception ex)
                    {
                        IARLogStatic.Error("SongManager", $"Failed to excute : {ex.Message}", ex);
                    }
                }
            }
            result.pKey = str; 
            result.isUpdate = result.pKey != null;

            return result;
        }

    }
}
