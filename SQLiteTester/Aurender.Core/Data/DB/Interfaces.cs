using System;
using Aurender.Core.Contents;

namespace Aurender.Core.Data.DB
{
    interface IQueryFactory<T>
    {
        String queryForMeta(DataFilter filter);
        String queryForDataCount(DataFilter filter);
        String queryForData(DataFilter filter);
        String queryForAll(DataFilter filter);

        String queryForIndexSearch(int objectID, DataFilter filter);

        Type[] dataTypes { get; }
    }

    interface ISubQueryFactory<T> : IQueryFactory<T>
    {
        IQueryFactory<IAlbumFromDB> albumsQueryFactory { get; }
    }

    public class SectionIndexQueryRow
    {
        public String key { get; set; }

        public Int32 cnt { get; set; }
    }

    enum DataLoadingTaskType
    {
        Previous,
        Current,
        Next
    }

  
}
