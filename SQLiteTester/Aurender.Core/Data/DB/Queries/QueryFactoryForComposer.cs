using Aurender.Core.Contents;
namespace Aurender.Core.Data.DB.Queries
{
    class QueryFactoryForComposer : QueryFactoryForArtistCommon<IComposerFromDB>
    {
        internal QueryFactoryForComposer() : base("composer")
        {
            this.dataTypes = types;
            this.TableName = "composers";
            this.FieldNameForKey = "composer_key";
            this.FieldNameForName = "composer";
            this.FieldNameForID = "composer_id";
            this.FieldNameForSongCount = "songsAsComposer";
            this.FieldNameForAlbumCount = "albumsAsComposer";
            this.FieldNameForCover = "coverAsComposer";

            this.ValueForArtistType = "2";
        }

        protected override string IndexSearchClauseForSelectFieldsWithTable(DataFilter filter)
        {
            string condition = "";
            if (filter.Count > 0)
            {
                condition = $"where {WhereClause(filter)}";
            }

            return $"select composer_id from composers {condition} order by composer_key, composer ";
        }
    }


    class QueryFactoryForConductor : QueryFactoryForArtistCommon<IConductorFromDB>
    {
        internal QueryFactoryForConductor() : base("conductor")
        {
            this.dataTypes = types;
            this.TableName = "conductors";
            this.FieldNameForKey = "conductor_key";
            this.FieldNameForName = "conductor";
            this.FieldNameForID = "conductor_id";
            this.FieldNameForSongCount = "songsAsConductor";
            this.FieldNameForAlbumCount = "albumsAsConductor";
            this.FieldNameForCover = "coverAsConductor";

            this.ValueForArtistType = "3";
        }

        protected override string IndexSearchClauseForSelectFieldsWithTable(DataFilter filter)
        {
            string condition = "";
            if (filter.Count > 0)
            {
                condition = $"where {WhereClause(filter)}";
            }

            return $"select conductor_id  from conductors {condition} order by conductor_key, conductor";
        }
    }
}
