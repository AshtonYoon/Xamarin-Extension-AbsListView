using System;
using System.Linq;
using System.Text;
using Aurender.Core.Contents;

namespace Aurender.Core.Data.DB.Queries
{

    abstract class QueryFactoryForArtistCommon<T> : QueryFactoryForAll<T> where T : IDatabaseItem
    {
        /*
        NSString *name      = [rs stringForColumnIndex:0];
        NSInteger aID = [rs intForColumnIndex: 1];
        NSInteger aCount = [rs intForColumnIndex: 2];
        NSInteger sCount = [rs intForColumnIndex: 3];
        NSData* imageData = [rs dataForColumnIndex: 4];
         */
        static internal readonly Type[] types = new Type[]
        {
            typeof(String), // artistNames
            typeof(int), //artistID
            typeof(int), //albumCount
            typeof(int), //songCcunt
            typeof(String), // artistKey
            typeof(byte[]), // image
        };


        internal protected String TableName { get; set; }
        internal protected String FieldNameForKey { get; set; }
        internal protected String FieldNameForName { get; set; }
        internal protected String FieldNameForID { get; set; }
        internal protected String FieldNameForAlbumCount { get; set; }
        internal protected String FieldNameForSongCount { get; set; }
        internal protected String ValueForArtistType { get; set; }
        internal protected String FieldNameForCover { get; set; }

        public QueryFactoryForArtistCommon(string searchFieldName) : base(searchFieldName)
        {
        }

        protected override String TotalCountSelectClause()
        {
            return $"select count(*) as artistCount from {this.TableName}";
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
            String query = $"select {FieldNameForName}, {FieldNameForID}, {FieldNameForAlbumCount} as albumCount, {FieldNameForSongCount}  as songCount, {FieldNameForKey}, {FieldNameForCover} from {TableName} ";

            return query;
        }

        protected override string DataClauseForWhere(DataFilter filter)
        {
            String where = this.WhereClause(filter);
            if (where.Length > 0)
            {
                where = $" where {FieldNameForKey} >= ? and {where}";
            }
            else
            {
                where = $" where  {FieldNameForKey} >= ? ";
            }
            String order = $" order by  {FieldNameForKey},  {FieldNameForName} limit ? offset ? ";

            return $" {where} {order}";
        }

  /*      protected override string IndexSearchClauseForSelectFieldsWithTable()
        {
            this.E("Doesn't support this for QueryFactoryForSong");
            return "";
        } */


        protected override string MetaClauseForSelectFieldsWithTable()
        {
            return $"select {FieldNameForKey} as key, count(*) as cnt from  {TableName}";
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
                result = $"  {FieldNameForID} in (select sa.artist_id from songs s, songArtists sa where s.genreFilter =  {index} and s.song_id = sa.song_id and sa.isArtist = {ValueForArtistType}) ";
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



            if (bitFilter != 0 && samplingFilter != 0)

                result = $" (f2 & {(Int32)bitFilter}) and (f2 & {(Int32)samplingFilter}) ";

            else if (bitFilter != 0)
                result = $" (f2 & {(Int32)bitFilter}) ";


            else if (samplingFilter != 0)

                result = $" (f2 & {(Int32)samplingFilter}) ";

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
                if (rateFilter.IsRange())
                    result = $"  {FieldNameForKey} in (select artist_id from songArtists where isArtist = {ValueForArtistType}1 and song_id in (select song_id from songs where songRate between {rateFilter.Min} AND {rateFilter.Max})) ";

                else
                    result = $"  {FieldNameForKey} in (select artist_id from songArtists where isArtist = {ValueForArtistType} and song_id in (select song_id from songs where songRate = {rateFilter.Min} )) ";

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

                result = $" trackModified > {sec} ";
            }
            return result;
        }

        protected override String MetaClauseForKeyword(DataFilter filter)
        {
            var result = this.ClauseForKeyword(filter);
            return result;
        }

        protected override String MetaClauseForFolderFilter(DataFilter filter)
        {
            var result = this.ClauseForFolderFilter(filter);

            return result;
        }

        protected override String MetaClauseForAudioFilter(DataFilter filter)
        {
            var result = this.ClauseForAudioFilter(filter);
            return result;
        }

        protected override String MetaClauseForRate(DataFilter filter)
        {
            var result = this.ClauseForRate(filter);
            return result;
        }

        protected override String MetaClauseForRecentlyAdded(DataFilter filter)
        {
            var result = this.ClauseForRecentlyAdded(filter);
            return result;
        }
    }

}