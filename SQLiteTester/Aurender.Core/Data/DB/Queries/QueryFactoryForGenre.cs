using System;
using System.Linq;
using System.Text;
using Aurender.Core.Contents;

namespace Aurender.Core.Data.DB.Queries
{
    class QueryFactoryForGenre : QueryFactoryForAll<IGenreFromDB>
    {
        // genre_id, genre, albumCount, songCount, cover_m
        internal static readonly Type[] types = new Type[]
        {
            typeof(int), //genre_ID
            typeof(String), // genre
            typeof(int), // albumCount
            typeof(int), //songCount
            typeof(byte[]) // cover_m
        };


        internal QueryFactoryForGenre() : this(Contents.Ordering.Default)
        {
        }
        protected QueryFactoryForGenre(Ordering ordering) : base("genre")
        {
            this.dataTypes = types;
            this.Ordering = ordering;
        }

        internal readonly Ordering Ordering;


        protected override String TotalCountSelectClause()
        {
            return "select count(*) from audio_genres";
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
                String where = $" where genre_id in (select a.genre_id from songs a {songWhere} )";
                return where;
            }
        }


        protected override string DataClauseForSelectFieldsWithTable()
        {
            String query = "select  genre_id, genre, albumCount, songCount, cover_m "
                          + " from audio_genres";

            return query;
        }

        protected override string DataClauseForWhere(DataFilter filter)
        {
            String where = this.WhereClause(filter);
            if (where.Length > 0)
            {
                where = $" where '#' = ? and {where}";
            }
            else
            {
                where = " where '#' >= ? ";
            }
            String order = " order by genre limit ? offset ? ";

            return $" {where} {order}";
        }

        protected override string IndexSearchClauseForSelectFieldsWithTable(DataFilter filter)
        {
            string condition = "";
            if (filter.Count > 0)
            {
                condition = $"where {WhereClause(filter)}";
            }

            return $"select  genre_id from audio_genres {condition} order by genre";
        }


        protected override string MetaClauseForSelectFieldsWithTable()
        {
            String select = "select '#' as key, count(*) as cnt from audio_genres";
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

                foreach (var keyword in keywords)
                {
                    String converted = keyword.Replace("'", "''");

                    if (converted.StartsWith("\"") && converted.EndsWith("\"") && converted.Length > 1)
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

                if (keywords.Count() > 0)
                {
                    result.Remove(result.Length - 4, 4);
                }
            }

            return result.ToString();
        }

        protected override String ClauseForFolderFilter(DataFilter filter)
        {
            String result = "";

            Int32 index = filter.FolderFilter;

            if (index != -1)
            {
                result = $" genre_id in (select songs.genre_id from songs where genreFilter = {index}) ";
            }

            return result;
        }

        protected override String ClauseForAudioFilter(DataFilter filter)
        {
            string result;

            AudioPropertyFilter bitFilter = filter.AudioPropertyFilter & AudioPropertyFilter.F_BIT_WIDTH;

            if (bitFilter == AudioPropertyFilter.F_BIT_WIDTH)
                bitFilter = 0;


            AudioPropertyFilter samplingFilter = filter.AudioPropertyFilter & AudioPropertyFilter.F_NORMAL_SAMPLING_RATE;
            if (samplingFilter == AudioPropertyFilter.F_NORMAL_SAMPLING_RATE)
                samplingFilter = 0;


            var prefix = " genre_id in (select songs.genre_id from songs where ";
            var postfix = ") ";

            if (bitFilter != 0 && samplingFilter != 0)

                result = $"{prefix} (f2 & {(Int32)bitFilter}) and (f2 & {(Int32)samplingFilter}) {postfix} ";

            else if (bitFilter != 0)
                result = $"{prefix} (f2 & {(Int32)bitFilter}) {postfix}";


            else if (samplingFilter != 0)

                result = $"{prefix} (f2 & {(Int32)samplingFilter}) {postfix}";

            else

                result = @" ";



            //            this.LP("AudioFilter for ClauseForAudioFilter", $"Filter : {filter.AudioPropertyFilter}");
            //            this.LP("AudioFilter for ClauseForAudioFilter", $"Bit    : {bitFilter}");
            //            this.LP("AudioFilter for ClauseForAudioFilter", $"SRate  : {samplingFilter}");


            return result;
        }

        protected override String ClauseForRate(DataFilter filter)
        {
            String result = "";

            RatingFilterRange rateFilter = filter.RatingFilter;

            if (rateFilter != RatingFilterRange.Empty)
            {
                var prefix = " genre_id in (select songs.genre_id from songs where ";
                var postfix = ") ";

                if (rateFilter.IsRange())
                    result = $"{prefix}  songRate BETWEEN {rateFilter.Min} AND {rateFilter.Max} {postfix}";
                else
                    result = $"{prefix}  songRate = {rateFilter.Min} {postfix}";

            }

            return result;
        }

        protected override String ClauseForRecentlyAdded(DataFilter filter)
        {
            String result = "";

            Int32 recentlyAdded = filter.AddedDateFilter;
            if (recentlyAdded != -1)
            {
                Int32 sec = DBUtility.InSecToMinusDaysFromTodayToLinuxSecFrom1970(recentlyAdded) - 1;

                result = $" genre_id in (select songs.genre_id from songs where trackModified > {sec}) ";
            }
            return result;
        }
        protected override String MetaClauseForKeyword(DataFilter filter)
        {
            StringBuilder result = new StringBuilder();


            if (filter.ContainsKey(FilterTypes.Keyword))
            {
                var keywords = filter.Keywords;

                foreach (var keyword in keywords)
                {
                    String converted = keyword.Replace("'", "''");

                    if (converted.StartsWith("\"") && converted.EndsWith("\"") && converted.Length > 1)
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

                if (keywords.Count() > 0)
                {
                    result.Remove(result.Length - 4, 4);
                }
            }

            return result.ToString();
        }

        protected override String MetaClauseForFolderFilter(DataFilter filter)
        {
            return this.ClauseForFolderFilter(filter);
        }

        protected override String MetaClauseForAudioFilter(DataFilter filter)
        {
            return this.ClauseForAudioFilter(filter);
        }

        protected override String MetaClauseForRate(DataFilter filter)
        {
            return this.ClauseForRate(filter);
        }

        protected override String MetaClauseForRecentlyAdded(DataFilter filter)
        {
            return this.ClauseForRecentlyAdded(filter);
        }
    }

}
