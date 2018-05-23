using System;
using System.Collections.Generic;
using System.Text;
using Aurender.Core.Contents;

namespace Aurender.Core.Data.DB.Queries
{
    internal abstract class QueryFactoryForAll<T> : IQueryFactory<T>, IARLog where T : IDatabaseItem
    {
       #region IARLog
        private bool LogAll = false;
        bool IARLog.IsARLogEnabled { get { return LogAll; } set { LogAll = value; } }
        #endregion

        public string queryForAll(DataFilter filter)
        {
            String select = this.AllDataClauseForSelectFieldsWithTable();
            String where  = this.AllDataClauseForWhere(filter);


            string query = $"{select} {where}";
            this.LP("Query", $"Query for all {filter} ----->        {query}");
            return query;
        }

        public String queryForDataCount(DataFilter filter)
        {
            String select = this.TotalCountSelectClause();
            String where  = this.MetaClauseForWhere(filter);

            string query = $"{select} {where}";
            this.LP("Query", $"Query for count {filter} ----->        {query}");
            return query;
        }

        public string queryForData(DataFilter filter)
        {
            String select = this.DataClauseForSelectFieldsWithTable();
            String where  = this.DataClauseForWhere(filter);

            string query = $"{select} {where}";
            this.LP("Query", $"Query for data {filter} ----->        {query}");
            return query;
        }

        public string queryForIndexSearch(int objectID, DataFilter filter)
        {
            String select = this.IndexSearchClauseForSelectFieldsWithTable(filter);

            string query = $"{select} ";
            this.LP("Query", $"Query for indexSearch   ----->        {query}");
            return query;
        }
        public virtual string queryForMeta(DataFilter filter)
        {
            String select = this.MetaClauseForSelectFieldsWithTable();
            String where  = this.MetaClauseForWhere(filter);
            String group = this.MetaGroupClauseForSummary();

            string query = $"{select} {where} {group}";
            this.LP("Query", $"Query for meta {filter} ----->        {query}");
            return query;
        }
        Type[] IQueryFactory<T>.dataTypes => this.dataTypes;


        protected Type[] dataTypes;

        protected  String SearchFieldName { get; private set; }


        protected QueryFactoryForAll(String searchFieldName)
        {
            this.SearchFieldName = searchFieldName;
        }


        protected abstract String MetaClauseForSelectFieldsWithTable();
        protected String MetaClauseForWhere(DataFilter filter)
        {
            String where = this.WhereClauseForMeta(filter);

            if (where.Length == 0)
                return "";

            return $" where {where} ";
        }

        protected abstract String MetaGroupClauseForSummary();

        protected abstract String TotalCountSelectClause();



        protected abstract String DataClauseForSelectFieldsWithTable();
        protected abstract String DataClauseForWhere(DataFilter filter);


        protected abstract String AllDataClauseForSelectFieldsWithTable();
        protected abstract String AllDataClauseForWhere(DataFilter filter);

        protected abstract String IndexSearchClauseForSelectFieldsWithTable(DataFilter filter);

     

        protected virtual String WhereClause(DataFilter filter)
        {
            return QueryUtiltty.WhereClauseFactory(filter,
                ClauseForFolderFilter,
                ClauseForRecentlyAdded,
                ClauseForRate, 
                ClauseForKeyword,
                ClauseForAudioFilter);
        }

        protected virtual String WhereClauseForMeta(DataFilter filter)
        {
            return QueryUtiltty.WhereClauseFactory(filter,
                MetaClauseForFolderFilter, 
                MetaClauseForRecentlyAdded,
                MetaClauseForRate,
                MetaClauseForKeyword, 
                MetaClauseForAudioFilter);
        }


        //        abstract string IQueryFactory<T>.queryForMeta(DataFilter filter);
        //abstract string IQueryFactory<T>.queryForData(DataFilter filter);
        //abstract string IQueryFactory<T>.queryForAll(DataFilter filter);


        protected abstract String ClauseForKeyword(DataFilter filter);

        protected abstract String ClauseForFolderFilter(DataFilter filter);

        protected abstract String ClauseForAudioFilter(DataFilter filter);

        protected abstract String ClauseForRate(DataFilter filter);

        protected abstract String ClauseForRecentlyAdded(DataFilter filter);

        protected abstract String MetaClauseForKeyword(DataFilter filter);

        protected abstract String MetaClauseForFolderFilter(DataFilter filter);

        protected abstract String MetaClauseForAudioFilter(DataFilter filter);

        protected abstract String MetaClauseForRate(DataFilter filter);

        protected abstract String MetaClauseForRecentlyAdded(DataFilter filter);


    }

    public static class QueryUtiltty
    {
   public static String WhereClauseFactory(DataFilter filter, 
            Func<DataFilter, String> fFolderFilter, 
            Func<DataFilter, String> fRecentlyAddedFilter,
            Func<DataFilter, String> fRateFilter, 
            Func<DataFilter, String> fKeywordFilter, 
            Func<DataFilter, String> fAudioFilter)
        { 
            StringBuilder sb = new StringBuilder();

            if (filter != null)
            {
                int count = 0;
                foreach (KeyValuePair<FilterTypes, Object> kvp in filter)
                {
                    String clause = "";

                    switch (kvp.Key)
                    {
                        case FilterTypes.FolderIndex:
                            if (fFolderFilter != null)
                                clause = fFolderFilter(filter);
                            break;
                        case FilterTypes.AddedDate:
                            if (fRecentlyAddedFilter != null)
                                clause = fRecentlyAddedFilter(filter);
                            break;
                        case FilterTypes.Rating:
                            if (fRateFilter != null)
                                clause = fRateFilter(filter);
                            break;
                        case FilterTypes.Keyword:
                            if (fKeywordFilter != null)
                                clause = fKeywordFilter(filter);
                            break;

                        case FilterTypes.AudioProperty:
                            if (fAudioFilter != null)
                                clause = fAudioFilter(filter);
                            break;

                        default:
                            break;
                    }
                    count++;

                    if (clause.Length > 0)
                    {
                        if (count == filter.Count)
                        {
                            sb.Append($" {clause}");
                        }
                        else
                        {
                            sb.Append($" {clause} and ");
                        }
                    }
                }
            }

            return sb.ToString();
        }
    }
}
