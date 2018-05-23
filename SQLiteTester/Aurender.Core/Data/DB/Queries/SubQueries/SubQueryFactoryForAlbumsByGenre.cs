using Aurender.Core.Contents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Aurender.Core.Data.DB.Queries.SubQueries
{
    class SubQueryFactoryForAlbumsByGenreOrderByAlbumTitle : QueryFactoryForAlbumOrderByAlbumTitle
    {
        internal protected IGenreFromDB genre;

        public IQueryFactory<IAlbumFromDB> albumsQueryFactory
        {
            get
            {
                IARLogStatic.Error($"{this}", $"Doesn't support this");
                return null;
            }
        }

        internal protected SubQueryFactoryForAlbumsByGenreOrderByAlbumTitle(IGenreFromDB aGenre, Ordering order) : base(order)
        {
            genre = aGenre;
            this.ordering = " order by album_key, album, albumYear ";
        }
        internal SubQueryFactoryForAlbumsByGenreOrderByAlbumTitle(IGenreFromDB aGenre) : this(aGenre, Ordering.AlbumName)
        {

        }


        protected override String WhereClause(DataFilter filter)
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
                        case FilterTypes.AddedDate:
                            clause = this.ClauseForRecentlyAdded(filter);
                            break;
                        case FilterTypes.Rating:
                            clause = this.ClauseForRate(filter);
                            break;
                        case FilterTypes.Keyword:
                            clause = this.ClauseForKeyword(filter);
                            break;
                        case FilterTypes.AudioProperty:
                            clause = this.ClauseForAudioFilter(filter);

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


            String defaultCondition = $" album_id in (select c.album_id from songs c where {baseCondition(filter)} )";

            String original = sb.ToString();
            if (original.EndsWith(" and ") || original.Trim().Length == 0)
            {
                sb.Append($" {defaultCondition} ");
            }
            else
            {
                sb.Append($" and {defaultCondition} ");
            }

            return sb.ToString();
        }

        protected override String WhereClauseForMeta(DataFilter filter)
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
                        case FilterTypes.AddedDate:
                            clause = this.MetaClauseForRecentlyAdded(filter);
                            break;
                        case FilterTypes.Rating:
                            clause = this.MetaClauseForRate(filter);
                            break;
                        case FilterTypes.Keyword:
                            clause = this.MetaClauseForKeyword(filter);
                            break;
                        case FilterTypes.AudioProperty:
                            clause = this.MetaClauseForAudioFilter(filter);

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

            String defaultCondition = $" album_id in (select c.album_id from songs c where {baseCondition(filter)} )";

            String original = sb.ToString();
            if (original.EndsWith(" and ") || original.Trim().Length == 0)
            {
                sb.Append($" {defaultCondition} ");
            }
            else
            {
                sb.Append($" and {defaultCondition} ");
            }


            return sb.ToString();
        }

        protected override string AllDataClauseForWhere(DataFilter filter)
        {
            String songWhere = this.WhereClause(filter);

            if (songWhere.Length == 0)
            {
                string where = $" where album_id in (select c.album_id from songs c where {baseCondition(filter)}) ";

                return where;
            }
            else
            {
                String where = $" where album_id in (select c.album_id from songs c {songWhere} and {baseCondition(filter)}) ";
                return where;
            }
        }

        protected override string IndexSearchClauseForSelectFieldsWithTable(DataFilter filter)
        {
            String ordering = GetOrdering();
            String query = $"select album_id from albums where album_id in (select c.album_id from songs c where {baseCondition(filter)}) {ordering}";
            return query;
        }


        protected override String ClauseForFolderFilter(DataFilter filter)
        {
            /// this will be taken care by in baseCondition;
            return "";
        }



        protected String baseCondition(DataFilter filter = null)
        {

            String folderFilter = "";
            if (filter != null && filter.Keys.Contains(FilterTypes.FolderIndex))
            {
                var folder = filter[FilterTypes.FolderIndex];
                folderFilter = $" and c.genreFilter = {folder}";
            }

            String baseCondition = $" c.genre_id = {genre.dbID} {folderFilter}";
            return baseCondition;
        }


    }
    class SubQueryFactoryForAlbumsByGenreOrderByArtistAndAlbumYear : SubQueryFactoryForAlbumsByGenreOrderByAlbumTitle
    {

        internal SubQueryFactoryForAlbumsByGenreOrderByArtistAndAlbumYear(IGenreFromDB aGenre) : base(aGenre, Ordering.AritstNameAndReleaseYear)
        {
            ordering = " order by artist_key, artistNames, albumYear";
        }

        protected override string MetaClauseForSelectFieldsWithTable()
        {
            String select = "select artist_key as key, count(*) as cnt from albums";
            return select;
        }

        protected override string DataClauseForWhere(DataFilter filter)
        {
            String where = this.WhereClause(filter);
            if (where.Length > 0)
            {
                where = $" where artist_key >= ? and {where}";
            }
            else
            {
                where = " where artist_key >= ? ";
            }
            String order = $" {ordering} limit ? offset ? ";

            return $" {where} {order}";
        }
    }

    class SubQueryFactoryForAlbumsByGenreOrderByAlbumYear : SubQueryFactoryForAlbumsByGenreOrderByAlbumTitle
    {

        internal SubQueryFactoryForAlbumsByGenreOrderByAlbumYear(IGenreFromDB aGenre) : base(aGenre, Ordering.ReleaseYear)
        {
            ordering = "  order by albumYear, album_key, album";
        }

        protected override string MetaClauseForSelectFieldsWithTable()
        {
            String select = "select albumYear as key, count(*) as cnt from albums";
            return select;
        }

        protected override string DataClauseForWhere(DataFilter filter)
        {
            String where = this.WhereClause(filter);
            if (where.Length > 0)
            {
                where = $" where albumYear >= ? and {where}";
            }
            else
            {
                where = " where albumYear >= ? ";
            }
            String order = $" {Ordering} limit ? offset ? ";

            return $" {where} {order}";
        }
    }

    class SubQueryFactoryForAlbumsByGenreOrderByAlbumYearDesc : SubQueryFactoryForAlbumsByGenreOrderByAlbumTitle
    {

        internal SubQueryFactoryForAlbumsByGenreOrderByAlbumYearDesc(IGenreFromDB aGenre) : base(aGenre, Ordering.ReleaseYearDesc)
        {
            ordering = "  order by albumYear desc, album_key, album";
        }

        protected override string MetaClauseForSelectFieldsWithTable()
        {
            String select = "select albumYear as key, count(*) as cnt from albums";
            return select;
        }

        protected override string DataClauseForWhere(DataFilter filter)
        {
            String where = this.WhereClause(filter);
            if (where.Length > 0)
            {
                where = $" where albumYear <= ? and {where}";
            }
            else
            {
                where = " where albumYear <= ? ";
            }
            String order = $" {ordering} limit ? offset ? ";

            return $" {where} {order}";
        }
    }


}
