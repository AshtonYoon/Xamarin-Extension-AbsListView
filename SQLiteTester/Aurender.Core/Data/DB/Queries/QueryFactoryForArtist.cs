using Aurender.Core.Contents;

namespace Aurender.Core.Data.DB.Queries
{
    class QueryFactoryForArtist : QueryFactoryForArtistCommon<IArtistFromDB>
     {
        internal QueryFactoryForArtist() : base("artist")
        {
            this.dataTypes = types;
            this.TableName = "artists";
            this.FieldNameForKey = "artist_key";
            this.FieldNameForName = "artist";
            this.FieldNameForID = "artist_id";
            this.FieldNameForSongCount = "songsAsArtist";
            this.FieldNameForAlbumCount = "albumsAsArtist";
            this.FieldNameForCover = "coverAsArtist";

            this.ValueForArtistType = "1";

        }

        protected override string IndexSearchClauseForSelectFieldsWithTable(DataFilter filter)
        {
            string condition = "";
            if (filter.Count > 0)
            {
                condition = $"where {WhereClause(filter)}";
            }

            return $"select artist_id from artists {condition} order by artist_key, artist";
        }


    }
}
