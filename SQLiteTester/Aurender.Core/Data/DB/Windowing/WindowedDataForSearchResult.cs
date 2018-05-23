using System;
using System.Collections.Generic;
using Aurender.Core.Contents;

namespace Aurender.Core.Data.DB.Windowing
{

    internal  class WindowedDataForSearchResult<T> : WindowedData<T> where T: IDatabaseItem
    {
        internal override IDB Db
        {
            get => _db;

            set
            {
                lock(this)
                {
                    base.Db = value;
                    if (Albums != null)
                    {
                        Albums.Db = value;
                    }
                }
            }
        }
        public WindowedData<IAlbumFromDB> Albums { get; internal protected set; }

        public int AlbumsCount { get => Albums.totalItemCount; }


       

        private ISubQueryFactory<T> subQueryFactory => (ISubQueryFactory<T>)queryFactory;
       
        private WindowedDataForSearchResult(IDB db, IQueryFactory<T> factory, EViewType vType, IWindowedDataWatingDelegate callback, Func<IList<object>, T> constructor) : base(db, factory, vType, callback, constructor)
        {
            this.Albums = new WindowedData<IAlbumFromDB>(db, subQueryFactory.albumsQueryFactory, vType, null, x => new Album(x));
            this.Albums.Y = typeof(Album);
        }

        internal WindowedDataForSearchResult(IDB db, ISubQueryFactory<T> factory, EViewType vType, IWindowedDataWatingDelegate callBack, Func<IList<object>, T> constructor) 
            : this(db, (IQueryFactory<T>) factory, vType, callBack, constructor)
        {

        }

        internal override void reset()
        {
            base.reset();
            Albums.reset();
        }

        internal override void loadTotalItemCount()
        {
            base.loadTotalItemCount();
            Albums.loadTotalItemCount();
        }

        internal override void loadMetadataWith(DataFilter newFilter)
        {
            Albums.SetFilter(newFilter);
            base.loadMetadataWith(newFilter);
            Albums.loadMetadataWith(newFilter);
        }
    }

}