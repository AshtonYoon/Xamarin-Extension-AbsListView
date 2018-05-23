using System;
using System.Collections.Generic;
using Aurender.Core.Contents;
using Aurender.Core.Data.DB.Queries;
using Aurender.Core.Player.mpd;

namespace Aurender.Core.Data.DB.Managers
{

    internal class AlbumUtil 
    {

        internal static IList<ISong> LoadSongsForAlbum(int albumID)
        {
            IList<ISong> songs = new List<ISong>();
            var aurender = AurenderBrowser.GetCurrentAurender();
            if (aurender == null)
            {
                return songs;
            }

            AurenderDB db = ((Aurender) aurender).db;
            var currentFilter = aurender.AlbumManager.Filter;

            using (var conn = db.CreateConnection())
            {
                var query = QueryForSongsOfAlbum(currentFilter);
                var cmd = conn.CreateCommand(query, albumID);


                var result = cmd.ExecuteDeferredQuery(types: QueryFactoryForSong.TypesForSongsWithMediaIndex);

                int counter = 0;
                foreach (var objects in result)
                {
                    var item = new Song(objects);

                    songs.Add(item);

                    counter++;
                }
            }
            return songs;
        }

        /// <summary>
        /// 0 year, 1, publisher
        /// </summary>
        /// <param name="albumID"></param>
        /// <returns></returns>
        internal static IList<Object>  LoadAlbumYearAndPublisher(int albumID)
        {
            IList<ISong> songs = new List<ISong>();
            var aurender = AurenderBrowser.GetCurrentAurender();
            if (aurender == null)
            {
                return new List<Object>() { 0, String.Empty };
            }

            AurenderDB db = ((Aurender) aurender).db;
            var currentFilter = aurender.AlbumManager.Filter;

            using (var conn = db.CreateConnection())
            {
                var query = QueryFactoryForAlbumOrderByAlbumTitle.QueryForAlbumDetail;
                var cmd = conn.CreateCommand(query, albumID);


                var result = cmd.ExecuteDeferredQuery(types: QueryFactoryForAlbumOrderByAlbumTitle.QueryForAlbumDetailTypes);

                List<Object> resultInfo = new List<Object>();

                resultInfo.AddRange(result);
                return resultInfo;
            }
        }

      
        private static String QueryForSongsOfAlbum(DataFilter filter)
        {
            String condition = QueryUtiltty.WhereClauseFactory(filter,
                QueryFactoryForSong.GenericClauseForFolderFilter,
                QueryFactoryForSong.GenericClauseForRecentlyAdded,
                QueryFactoryForSong.GenericClauseForRate, 
                null, 
                QueryFactoryForSong.GenericClauseForAudioFilter);


            String query = $"{QueryFactoryForSong.SongQueryWithMediaIndex} where b.album_id = ? { condition} order by a.album_id, discIndex, indexOfSong ";

            return query;
        }
    }

}