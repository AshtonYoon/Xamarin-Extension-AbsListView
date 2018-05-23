using System;
using System.Linq;
using System.Text;
using Aurender.Core.Contents;

namespace Aurender.Core.Data.DB.Queries
{
    class QueryFactoryForAlbumOrderByAlbumTitle : QueryFactoryForAll<IAlbumFromDB>
    {
        internal const String QueryForAlbumDetail = "select year, publisher from album_meta where album_id = ?";

        internal static readonly Type[] QueryForAlbumDetailTypes = new Type[]
        {
            typeof(int), // album_id
            typeof(String), // album
        };

        // album_id, album, artistNames, songCount, cover_m
        internal static readonly Type[] types = new Type[]
        {
            typeof(int), // album_id
            typeof(String), // album
            typeof(String), // artistNames 
            typeof(int), // songCount
            typeof(String), // order key
            typeof(byte[]), // cover_m
        };

        protected String orderKey;

        internal QueryFactoryForAlbumOrderByAlbumTitle() : this(Ordering.AlbumName)
        {
        }
        
        protected QueryFactoryForAlbumOrderByAlbumTitle(Ordering ordering) : base("album")
        {
            this.dataTypes = types;
            this.Ordering = ordering;
            this.orderKey = "album_key";
            this.ordering = " order by album_key, album, artistNames ";
        }

        internal readonly Ordering Ordering;
        internal string ordering;

        protected override String TotalCountSelectClause()
        {
            return "select count(*) from albums";
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


        protected override string DataClauseForSelectFieldsWithTable()
        {
            String query = $"select album_id, album, artistNames, songCount, {orderKey}, cover_m from albums";

            return query;
        }

        protected override string DataClauseForWhere(DataFilter filter)
        {
            String where = this.WhereClause(filter);
            if (where.Length > 0)
            {
                where = $" where {orderKey} >= ? and {where}";
            }
            else
            {
                where = $" where {orderKey} >= ? ";
            }

            String ordering = GetOrdering();
            String order = $" {ordering} limit ? offset ? ";

            return $" {where} {order}";
        }

        protected override string IndexSearchClauseForSelectFieldsWithTable(DataFilter filter)
        {
            string condition = "";
            if (filter.Count > 0)
            {
                condition = $"where {WhereClause(filter)}";
            }
            String ordering = GetOrdering();

            String query = $"select album_id from albums {condition} {ordering}";
            return query;
        }


        protected override string MetaClauseForSelectFieldsWithTable()
        {
            String select = "select album_key as key, count(*) as cnt from albums";
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
                result = $" album_id in (select songs.album_id from songs where genreFilter = {index}) ";
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


            var prefix = " album_id in (select songs.album_id from songs where ";
            var postfix = ") ";

            if (bitFilter != 0 && samplingFilter != 0)

                result = $"{prefix} (f2 & {(Int32)bitFilter}) and (f2 & {(Int32)samplingFilter}) {postfix} ";

            else if (bitFilter != 0)
                result = $"{prefix} (f2 & {(Int32)bitFilter}) {postfix}";


            else if (samplingFilter != 0)

                result = $"{prefix} (f2 & {(Int32)samplingFilter}) {postfix}";

            else

                result = @" ";



//            this.LP("audiofilter", $"Filter : {filter.AudioPropertyFilter}");
//            this.LP("audiofilter", $"Bit    : {bitFilter}");
//            this.LP("audiofilter", $"SRate  : {samplingFilter}");

            return result;
        }

        protected override String ClauseForRate(DataFilter filter)
        {
            String result = "";

            RatingFilterRange rateFilter = filter.RatingFilter;

            if (rateFilter != RatingFilterRange.Empty)
            {
                var prefix = " album_id in (select songs.album_id from songs where ";
                var postfix = ") ";

                if (rateFilter.IsRange())
                    result = $"{prefix} songRate BETWEEN {rateFilter.Min} AND {rateFilter.Max} {postfix}";
                else
                    result = $"{prefix} songRate = {rateFilter.Min} {postfix}";

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

                result = $" album_id in (select songs.album_id from songs where trackModified > {sec}) ";
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

        protected String GetOrdering()
        {
            return ordering;
        }
    }


    class QueryFactoryForAlbumOrderByArtistNameAndReleaseYear : QueryFactoryForAlbumOrderByAlbumTitle
    {
        protected QueryFactoryForAlbumOrderByArtistNameAndReleaseYear(Ordering order) : base(order) { }

        internal QueryFactoryForAlbumOrderByArtistNameAndReleaseYear() : this(Ordering.AritstNameAndReleaseYear)
        {
            this.orderKey = "artist_key";
            ordering = " order by artist_key, artistNames, albumYear ";
        }
   protected override string MetaClauseForSelectFieldsWithTable()
        {
            String select = "select artist_key as key, count(*) as cnt from albums";
            return select;
        }
    }


    class QueryFactoryForAlbumOrderByArtistNameAndAlbumName : QueryFactoryForAlbumOrderByArtistNameAndReleaseYear
    {
        internal QueryFactoryForAlbumOrderByArtistNameAndAlbumName() : base(Ordering.ArtistNameAndAlbumName)
        {
            this.orderKey = "artist_key";
            ordering = " order by artist_key, artistNames, album ";
        }
    }

 /*   class QueryFactoryForAlbumOrderByAddedDate : QueryFactoryForAlbumOrderByAlbumTitle
    {
        internal QueryFactoryForAlbumOrderByAddedDate() : base(Ordering.AddedDate)
        {
            ordering = " order by trackModified, album ";
        }

        protected override string MetaClauseForSelectFieldsWithTable()
        {
            /// to group trackModified we need to make a user defined function to use
            String select = "select artist_key as key, count(*) as cnt from albums";
            return select;
        }
    } */

    class QueryFactoryForAlbumOrderByReleaseYear : QueryFactoryForAlbumOrderByAlbumTitle
    {
        internal QueryFactoryForAlbumOrderByReleaseYear() : this(Ordering.ReleaseYear)
        {
            this.orderKey = "albumYear";
            ordering = " order by albumYear, artistNames,  album ";
        }
        protected QueryFactoryForAlbumOrderByReleaseYear(Ordering order) : base(order) { }

        protected override string MetaClauseForSelectFieldsWithTable()
        {
            String select = "select albumYear as key, count(*) as cnt from albums";
            return select;
        }
    }

    class QueryFactoryForAlbumOrderByReleaseYearDesc : QueryFactoryForAlbumOrderByReleaseYear
    {
        internal QueryFactoryForAlbumOrderByReleaseYearDesc() : base(Ordering.ReleaseYearDesc)
        {
            this.orderKey = "albumYear";
            ordering = " order by albumYear desc, artistNames, album ";
        }
    }
}
