using Aurender.Core.Contents;
using System;
using System.Linq;
using System.Text;

namespace Aurender.Core.Data.DB.Queries.SubQueries
{
    class SubQueryFactoryForSongsWithAlbumsByGenreOrderByReleaseYear : QueryFactoryForSong, ISubQueryFactory<ISongFromDB>
    {
        protected IGenreFromDB genre => subQueryFactory.genre;

        public IQueryFactory<IAlbumFromDB> albumsQueryFactory => subQueryFactory;

        protected String dataOrder;

        protected SubQueryFactoryForAlbumsByGenreOrderByAlbumTitle subQueryFactory;

        internal SubQueryFactoryForSongsWithAlbumsByGenreOrderByReleaseYear(IGenreFromDB genre) : base("album")
        {
            subQueryFactory = new SubQueryFactoryForAlbumsByGenreOrderByAlbumYear(genre);
            dataOrder = "albumYear, album_key, album, discIndex, indexOfSong";
        }


        protected String baseCondition()
        {

            String baseCondition = $" a.genre_id = {genre.dbID} ";
            return baseCondition;
        }

        protected override String WhereClause(DataFilter filter)
        {
            String where = base.WhereClause(filter);
    
            StringBuilder sb = new StringBuilder(where);

            if (where.Trim().Length > 0)
            {
                sb.Append($" and {baseCondition()} ");
            }
            else
            {
                sb.Append($" {baseCondition()} ");
            }
            return sb.ToString();
        }

        protected override String WhereClauseForMeta(DataFilter filter)
        {
            String where = base.WhereClauseForMeta(filter);
    
            StringBuilder sb = new StringBuilder(where);

            if (where.Trim().Length > 0)
            {
                sb.Append($" and {baseConditionForMeta()} ");
            }
            else
            {
                sb.Append($" {baseConditionForMeta()} ");
            }


            return sb.ToString();
        }

        private object baseConditionForMeta()
        {
            String baseCondition = $" genre_id = {genre.dbID} ";
            return baseCondition;
        }

        protected override string DataClauseForWhere(DataFilter filter)
        {
            String where = this.WhereClause(filter);
            if (where.Length > 0)
            {
                where = $" where '#' >= ? and {where}";
            }
            else
            {
                where = " where '#' >= ? ";
            }
            String order = $" order by {dataOrder} limit ? offset ? ";

            return $" {where} {order}";
        }


        protected override string MetaClauseForSelectFieldsWithTable()
        {
            String select = "select '#' as key, count(*) as cnt from songs";
            return select;
        }




        protected override String MetaClauseForKeyword(DataFilter filter)
        {
            StringBuilder result = new StringBuilder();

            String metaSearchField = "album";

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
                            result.AppendFormat(" ( {0} like '% {1} %' or {0} like '{1} %' or {0} like '% {1}' ) and ", metaSearchField, word);

                        }
                        else
                        {
                            result.AppendFormat(" ( {0} like '% {1} %' or {0} like '{1} %' or {0} like '% {1}' or {0} = '{1}' ) and ", metaSearchField, withoutQoute);
                        }

                    }
                    else
                    {
                        result.Append($" {metaSearchField} like '%{converted}%' and ");
                    }

                }

                if (keywords.Count() > 0)
                {
                    result.Remove(result.Length - 4, 4);
                    result.Insert(0, " album_id in (select x.album_id from albums x where ");
                    result.Append(" ) ");
                }
            }

            return result.ToString();
        }

    }

    class SubQueryFactoryForSongsWithAlbumsByGenreOrderByReleaseYearDesc : SubQueryFactoryForSongsWithAlbumsByGenreOrderByReleaseYear
    {
        public SubQueryFactoryForSongsWithAlbumsByGenreOrderByReleaseYearDesc(IGenreFromDB genre) : base(genre)
        {
            subQueryFactory = new SubQueryFactoryForAlbumsByGenreOrderByAlbumYearDesc(genre);
            dataOrder = "albumYear desc, album_key, album, discIndex, indexOfSong";
        }
    }
    class SubQueryFactoryForSongsWithAlbumsByGenreOrderByArtistAndAlbumYear : SubQueryFactoryForSongsWithAlbumsByGenreOrderByReleaseYear
    {
        public SubQueryFactoryForSongsWithAlbumsByGenreOrderByArtistAndAlbumYear(IGenreFromDB genre) : base(genre)
        {
            subQueryFactory = new SubQueryFactoryForAlbumsByGenreOrderByArtistAndAlbumYear(genre);
            dataOrder = "artist_key, artistNames, album_key, album, discIndex, indexOfSong";
        }
    }

    class SubQueryFactoryForSongsWithAlbumsByGenreOrderByAlbumTitle : SubQueryFactoryForSongsWithAlbumsByGenreOrderByReleaseYear
    {
        public SubQueryFactoryForSongsWithAlbumsByGenreOrderByAlbumTitle(IGenreFromDB genre) : base(genre)
        {
            subQueryFactory = new SubQueryFactoryForAlbumsByGenreOrderByAlbumTitle(genre);
            dataOrder = "album_key, albums, artist_key, artistNames, discIndex, indexOfSong";
        }
    }
}
