using Aurender.Core.Contents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Aurender.Core.Data.DB.Queries.SubQueries
{
    abstract class SubQueryFactoryForAlbumsByArtist : QueryFactoryForAlbumOrderByAlbumTitle
    {
        internal protected IArtistFromDB artist;

        protected SubQueryFactoryForAlbumsByArtist(IArtistFromDB aArtist, Ordering order) : base(order)
        {
            artist = aArtist;
            ordering = " order by albumYear, album_key, album ";
        }

        public override string queryForMeta(DataFilter filter)
        {
            String select = this.MetaClauseForSelectFieldsWithTable();
            String where = this.MetaClauseForWhere(filter);
            String group = this.MetaGroupClauseForSummary();

            string query = $"{select} {where} {group}";
            return query;
        }

        public String queryForDataCount(DataFilter filter)
        {
            String select = this.TotalCountSelectClause();
            String where  = this.MetaClauseForWhere(filter);

            string query = $"{select} {where}";
            return query;
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

            String original = sb.ToString();
            String defaultCondition = $" album_id in (select c.album_id from songs c where {baseCondition(filter)} )";

            if (original.EndsWith(" and ") ||original.Trim().Length == 0)
            {
                sb.Append($" {defaultCondition}");
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

            String original = sb.ToString();

            String defaultCondition = $" album_id in (select c.album_id from songs c where {baseCondition(filter)} )";

            if (original.EndsWith(" and ") || original.Trim().Length == 0)
            {
                sb.Append($" {defaultCondition}");
            }
            else
            {
                    sb.Append($" and {defaultCondition} ");
            }

            return sb.ToString();
        }




        protected override string MetaClauseForSelectFieldsWithTable()
        {
            String select = "select '#' as key, count(*) as cnt from albums";
            return select;
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
            String order = $" {ordering} limit ? offset ? ";

            return $" {where} {order}";
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
            String type = getArtistType();

            String folderFilter = "";
            if (filter != null && filter.Keys.Contains(FilterTypes.FolderIndex))
            {
                var folder = filter[FilterTypes.FolderIndex];
                folderFilter = $" and c.genreFilter = {folder}";
            }

            String baseCondition = $" c.song_id in (select z.song_id from songArtists z where z.artist_id = {artist.dbID} and z.artistType = {type}) {folderFilter}";
            return baseCondition;
        }

        internal string getArtistType()
        {
            String type = "'Main'";

            if (artist is IComposerFromDB)
            {
                type = "'Composer'";
            }
            else if (artist is IConductorFromDB)
            {
                type = "'Conductor'";
            }

            return type;
        }
    }

    class SubQueryFactoryForAlbumsByArtistOrderByReleaseYear : SubQueryFactoryForAlbumsByArtist
    {
        internal SubQueryFactoryForAlbumsByArtistOrderByReleaseYear(IArtistFromDB artist) : base(artist, Ordering.ReleaseYear)
        {
            ordering = " order by albumYear, album_key, album ";
        }
    }

    class SubQueryFactoryForAlbumsByArtistOrderByReleaseYearDesc : SubQueryFactoryForAlbumsByArtist
    {
        internal SubQueryFactoryForAlbumsByArtistOrderByReleaseYearDesc(IArtistFromDB artist) : base(artist, Ordering.ReleaseYear)
        {
            ordering = " order by albumYear desc, album_key, album ";
        }
    }


    class SubQueryFactoryForAlbumsByArtistOrderByAlbumTitle : SubQueryFactoryForAlbumsByArtist
    {
        internal SubQueryFactoryForAlbumsByArtistOrderByAlbumTitle(IArtistFromDB artist) : base(artist, Ordering.ReleaseYear)
        {
            ordering = " order by album_key, album";
        }
    }

 
}
