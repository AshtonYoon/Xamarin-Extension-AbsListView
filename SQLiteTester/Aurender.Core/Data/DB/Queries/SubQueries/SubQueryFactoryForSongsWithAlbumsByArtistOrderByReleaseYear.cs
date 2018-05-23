using System;
using System.Linq;
using System.Text;
using Aurender.Core.Contents;

namespace Aurender.Core.Data.DB.Queries.SubQueries
{
    class SubQueryFactoryForSongsWithAlbumsByArtistOrderByReleaseYear : QueryFactoryForSong, ISubQueryFactory<ISongFromDB>
    {
        protected IArtistFromDB artist => subQueryFactory.artist;

        public IQueryFactory<IAlbumFromDB> albumsQueryFactory => subQueryFactory;

        protected String dataOrder;

        protected SubQueryFactoryForAlbumsByArtist subQueryFactory;
                  
        internal SubQueryFactoryForSongsWithAlbumsByArtistOrderByReleaseYear(IArtistFromDB artist) : base("album")
        {
            subQueryFactory = new SubQueryFactoryForAlbumsByArtistOrderByReleaseYear(artist);
            dataOrder = "albumYear, album_key, album, discIndex, indexOfSong";

        }


        protected String baseCondition()
        {
            string type = getArtistType();

            String baseCondition = $" song_id in (select z.song_id from songArtists z where z.artist_id = {artist.dbID} and z.artistType = {type})";
            return baseCondition;
        }

        private string getArtistType()
        {
            return subQueryFactory.getArtistType();
        }

        protected override String WhereClause(DataFilter filter)
        {
            String original = base.WhereClause(filter);
            StringBuilder sb = new StringBuilder(original);

            if (original.EndsWith(" and ") || original.Trim().Length == 0)
            {
                sb.Append($" {baseCondition()}");
            }
            else
            {
                    sb.Append($" and {baseCondition()} ");
            }

            return sb.ToString();
        }

        protected override String WhereClauseForMeta(DataFilter filter)
        {
            String original = base.WhereClauseForMeta(filter);
            StringBuilder sb = new StringBuilder(original);

            if (original.EndsWith(" and ") || original.Trim().Length == 0)
            {
                sb.Append($" {baseCondition()}");
            }
            else
            {
                    sb.Append($" and {baseCondition()} ");
            }

            return sb.ToString();
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

    class SubQueryFactoryForhSongsWithAlbumsByArtistOrderByReleaseYearDesc : SubQueryFactoryForSongsWithAlbumsByArtistOrderByReleaseYear
    {
        public SubQueryFactoryForhSongsWithAlbumsByArtistOrderByReleaseYearDesc(IArtistFromDB artist) : base(artist)
        {
            subQueryFactory = new SubQueryFactoryForAlbumsByArtistOrderByReleaseYearDesc(artist);
            dataOrder = "albumYear desc, album_key, album, discIndex, indexOfSong";
        }
    }

    class SubQueryFactoryForhSongsWithAlbumsByArtistOrderByAlbumTitle : SubQueryFactoryForSongsWithAlbumsByArtistOrderByReleaseYear
    {
        public SubQueryFactoryForhSongsWithAlbumsByArtistOrderByAlbumTitle(IArtistFromDB artist) : base(artist)
        {
            subQueryFactory = new SubQueryFactoryForAlbumsByArtistOrderByAlbumTitle(artist);
            dataOrder = "album_key, album, discIndex, indexOfSong";
        }
    }

}
