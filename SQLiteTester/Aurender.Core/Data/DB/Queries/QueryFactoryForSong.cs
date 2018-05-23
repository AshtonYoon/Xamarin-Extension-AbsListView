using System;
using System.Linq;
using System.Text;
using Aurender.Core.Contents;
using System.Diagnostics;

namespace Aurender.Core.Data.DB.Queries
{
    internal class QueryFactoryForSong : QueryFactoryForAll<ISongFromDB>
    {
        internal const String BasicSongQuery = "select song_id, artistsNames, album, indexOfSong, song_title, duration," +
                                                               " totalTracks, track_id, a.songRate, a.f2, a.song_key " +
                                                         " from songs a left join albums b on a.album_id = b.album_id ";

        internal const String SongQueryWithMediaIndex = "select song_id, artistsNames, album, indexOfSong, song_title, duration," +
                                                                          " totalTracks, track_id, a.songRate, a.f2, a.song_key, discIndex, genre" +
                                                         " from songs a left join albums b on a.album_id = b.album_id left join audio_genres c on a.genre_id = c.genre_id ";



         internal static readonly Type[] types = new Type[]
         {
            typeof(int), //song_ID
            typeof(String), // artistNames
            typeof(String), // album 
            typeof(int), //indexOfSong
            typeof(String), // song_Title 4
            typeof(int), //duration 5
            typeof(int), //totalTracks 6
            typeof(int), //track_id 7
            typeof(int), //songRate 8
            typeof(int), //f2 9
            typeof(String), // song_Key 10
        };

        internal static  Type[] TypesForSongsWithMediaIndex => new Type[]
        {
            typeof(int), //song_ID
            typeof(String), // artistNames
            typeof(String), // album 
            typeof(int), //indexOfSong
            typeof(String), // song_Title 4
            typeof(int), //duration 5
            typeof(int), //totalTracks 6
            typeof(int), //track_id 7
            typeof(int), //songRate 8
            typeof(int), //f2 9
            typeof(String), // song_Key 10
            typeof(byte), //mediaIndex 11
            typeof(String), //mediaIndex 12
        };


        internal protected QueryFactoryForSong(String fieldName) : base(fieldName)
        {
            this.dataTypes = types;
        }

        internal QueryFactoryForSong() : this("song_title")
        {
        }
        
        protected override String TotalCountSelectClause()
        {
            return "select count(*) from songs";
        }
        

        protected override string AllDataClauseForSelectFieldsWithTable()
        {
           return "select fileName from tracks";
        }

        protected override string AllDataClauseForWhere(DataFilter filter)
        {
            String songWhere = this.WhereClause(filter);

            if (songWhere.Length == 0)
            {
                return "";
            }
            else
            {
                String where = $" where song_id in (select a.song_id from songs a {songWhere} )";
                return where;
            }
        }


        protected override string DataClauseForSelectFieldsWithTable() => BasicSongQuery;

        protected override string DataClauseForWhere(DataFilter filter)
        {
            String where = this.WhereClause(filter);
            if (where.Length > 0)
            {
                where = $" where a.song_key >= ? and {where}"; 
            }
            else
            {
                where = " where a.song_key >= ? ";
            }
            String order = " order by a.song_key, song_title limit ? offset ? ";

            return $" {where} {order}";
        }

        protected override string IndexSearchClauseForSelectFieldsWithTable(DataFilter filter)
        {
            Debug.Assert(false, "This shouldn't be called at all");
            return "";
        }


        protected override string MetaClauseForSelectFieldsWithTable()
        {
            String select = "select song_key as key, count(*) as cnt from songs";
            return select;
        }

        protected override string MetaGroupClauseForSummary()
        {
            return " group by key order by key ";
        }


        protected override String ClauseForKeyword(DataFilter filter)
        {
            StringBuilder result = new StringBuilder();


            if (filter.ContainsKey(FilterTypes.Keyword))
            {
                var keywords = filter.Keywords;

                foreach(var keyword in keywords)
                {
                    String converted = keyword.Replace("'", "''");

                    if (converted.StartsWith("\"") && converted.EndsWith("\"") && converted.Length > 1 )
                    {
                        String withoutQoute = converted.Substring(1, converted.Length - 2);


                        if (withoutQoute.Length == 0)
                        {
                            String word = "\"";
                            result.AppendFormat(" ( {0} like '% {1} %' or {0} like '{1} %' or {0} like '% {1}' ) and ", SearchFieldName, word);
                             
                        }
                        else
                        {
                            result.AppendFormat(" ( {0} like '% {1} %' or {0} like '{1} %' or {0} like '% {1}' or {0} = '{1}' ) and ", SearchFieldName, withoutQoute); 
                        }

                    }
                    else
                    {
                        result.Append($" {SearchFieldName} like '%{converted}%' and ");
                    }

                }
            
                if (keywords.Count() > 0 )
                {
                    result.Remove(result.Length - 4, 4);
                }
            }

            return result.ToString();
        }

        public static String GenericClauseForFolderFilter(DataFilter filter)
        {
            String result = "";
            Int32 index = filter.FolderFilter;

            if (index != -1)
                result = $" a.genreFilter = {index} ";

            return result;
        }
        protected override String ClauseForFolderFilter(DataFilter filter) => GenericClauseForFolderFilter(filter);

        public static String GenericClauseForAudioFilter(DataFilter filter)
        {
            string result;
            AudioPropertyFilter bitFilter = filter.AudioPropertyFilter & AudioPropertyFilter.F_BIT_WIDTH;

            if (bitFilter == AudioPropertyFilter.F_BIT_WIDTH)
                bitFilter = 0;

            AudioPropertyFilter samplingFilter = filter.AudioPropertyFilter & AudioPropertyFilter.F_NORMAL_SAMPLING_RATE;
            if (samplingFilter == AudioPropertyFilter.F_NORMAL_SAMPLING_RATE)
                samplingFilter = 0;

            if (bitFilter != 0 && samplingFilter != 0)
                result = $" (a.f2 & {(Int32)bitFilter}) and (a.f2 & {(Int32)samplingFilter}) ";
            else if (bitFilter != 0)
                result = $" (a.f2 & {(Int32)bitFilter}) ";
            else if (samplingFilter != 0)
                result = $" (a.f2 & {(Int32)samplingFilter}) ";
            else
                result = @" ";

            IARLogStatic.Log("GenericClauseForAudioFilter", $"Filter : {filter.AudioPropertyFilter}");
            IARLogStatic.Log("GenericClauseForAudioFilter", $"Bit    : {bitFilter}");
            IARLogStatic.Log("GenericClauseForAudioFilter", $"SRate  : {samplingFilter}");

            return result;
        }
        protected override String ClauseForAudioFilter(DataFilter filter) => GenericClauseForAudioFilter(filter);


        public static String GenericClauseForRate(DataFilter filter)
        {
            String result = "";

            RatingFilterRange rateFilter = filter.RatingFilter;

            if (rateFilter != RatingFilterRange.Empty)
            {
                if (rateFilter.IsRange())
                    result = $" a.songRate BETWEEN {rateFilter.Min} AND {rateFilter.Max} ";
                else
                    result = $" a.songRate = {rateFilter.Min} ";
            }

            return result;
        }

        protected override String ClauseForRate(DataFilter filter) => GenericClauseForRate(filter); 
       
        public static String GenericClauseForRecentlyAdded(DataFilter filter)
        {
            String result = "";

            Int32 recentlyAdded = filter.AddedDateFilter;
            if (recentlyAdded != -1)
            {
                Int32 sec = DBUtility.InSecToMinusDaysFromTodayToLinuxSecFrom1970(recentlyAdded) - 1;

                result = $" a.trackModified > {sec} ";
            }
            return result;
        }

        protected override String ClauseForRecentlyAdded(DataFilter filter) => GenericClauseForRecentlyAdded(filter);


        protected override String MetaClauseForKeyword(DataFilter filter)
        {
            StringBuilder result = new StringBuilder();


            if (filter.ContainsKey(FilterTypes.Keyword))
            {
                var keywords = filter.Keywords;

                foreach(var keyword in keywords)
                {
                    String converted = keyword.Replace("'", "''");

                    if (converted.StartsWith("\"") && converted.EndsWith("\"") && converted.Length > 1 )
                    {
                        String withoutQoute = converted.Substring(1, converted.Length - 2);


                        if (withoutQoute.Length == 0)
                        {
                            String word = "\"";
                            result.AppendFormat(" ( {0} like '% {1} %' or {0} like '{1} %' or {0} like '% {1}' ) and ", SearchFieldName, word);
                             
                        }
                        else
                        {
                            result.AppendFormat(" ( {0} like '% {1} %' or {0} like '{1} %' or {0} like '% {1}' or {0} = '{1}' ) and ", SearchFieldName, withoutQoute); 
                        }

                    }
                    else
                    {
                        result.Append($" {SearchFieldName} like '%{converted}%' and ");
                    }

                }
            
                if (keywords.Count() > 0 )
                {
                    result.Remove(result.Length - 4, 4);
                }
            }

            return result.ToString();
        }

        protected override String MetaClauseForFolderFilter(DataFilter filter)
        {
            String result = "";

            Int32 index = filter.FolderFilter;

            if (index != -1)
            {
                result = $" genreFilter = {index} ";
            }

            return result;
        }

        protected override String MetaClauseForAudioFilter(DataFilter filter)
        {
            string result;

            AudioPropertyFilter bitFilter  = filter.AudioPropertyFilter & AudioPropertyFilter.F_BIT_WIDTH;

            if (bitFilter == AudioPropertyFilter.F_BIT_WIDTH)
                bitFilter = 0;


            AudioPropertyFilter  samplingFilter = filter.AudioPropertyFilter & AudioPropertyFilter.F_NORMAL_SAMPLING_RATE;
            if (samplingFilter == AudioPropertyFilter.F_NORMAL_SAMPLING_RATE)
                samplingFilter = 0;



            if (bitFilter != 0 && samplingFilter != 0)

                result = $" (f2 & {(Int32)bitFilter}) and (f2 & {(Int32)samplingFilter}) ";

            else if (bitFilter != 0)
                result = $" (f2 & {(Int32)bitFilter}) ";


             else if (samplingFilter != 0)  

                result = $" (f2 & {(Int32)samplingFilter}) ";
            
            else

                result = @" ";



            //            this.LP("AudioFilter for MetaClauseForAudioFilter", $"Filter : {filter.AudioPropertyFilter}");
            //            this.LP("AudioFilter for MetaClauseForAudioFilter", $"Bit    : {bitFilter}");
            //            this.LP("AudioFilter for MetaClauseForAudioFilter", $"SRate  : {samplingFilter}");

            return result;
        }

        protected override String MetaClauseForRate(DataFilter filter)
        {
            String result = "";

                RatingFilterRange rateFilter = filter.RatingFilter;

                if (rateFilter != RatingFilterRange.Empty)
                {
                    if (rateFilter.IsRange())
                        result = $" songRate BETWEEN {rateFilter.Min} AND {rateFilter.Max} ";
                    else
                        result = $" songRate = {rateFilter.Min} ";

                }

            return result;
        }
         
        protected override  String MetaClauseForRecentlyAdded(DataFilter filter)
        {
            String result = "";

            Int32 recentlyAdded = filter.AddedDateFilter;
            if (recentlyAdded != -1)
            {
                Int32 sec = DBUtility.InSecToMinusDaysFromTodayToLinuxSecFrom1970(recentlyAdded) - 1;

                result = $" trackModified > {sec} ";
            }
            return result;
        }
    }
}
