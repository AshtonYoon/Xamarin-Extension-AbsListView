using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Diagnostics;
using Aurender.Core.Contents;

namespace Aurender.Core.Data.DB
{
    class DataItemGrouping<T> : IGrouping<String, T> where T : IDatabaseItem
    {
        public String Key { get; private set; }
        private readonly IEnumerator<T> enumerator;

        internal DataItemGrouping(String aKey, IEnumerator<T> enumr)
        {
            this.Key = aKey;
            this.enumerator = enumr;
        }


        public IEnumerator<T> GetEnumerator() => this.enumerator;

        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
    }

    public class KeyDataEnumerable<T> : IEnumerable<IGrouping<String, T>> where T : IDatabaseItem
    {
        private readonly AbstractManager<T> manager;

        public KeyDataEnumerable()
        {
        }

        public KeyDataEnumerable(AbstractManager<T> dataManager)
        {
            this.manager = dataManager;
        }

        IEnumerator<IGrouping<String, T>> IEnumerable<IGrouping<String, T>>.GetEnumerator()
        {
            if (manager == null || manager.Sections == null)
            {
                yield break;
            }

            for (int i = 0; i < manager.Sections.Count; i++)
            {

                String key = manager.Sections[i];


                yield return new DataItemGrouping<T>(key, GetEnumeratorForKey(key, i).GetEnumerator());
            }

            yield break;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            var enumerator = this as IEnumerable<IGrouping<String, T>>;
            return enumerator.GetEnumerator();
        }

        IEnumerable<T> GetEnumeratorForKey(String key, int i)
        {
            if (manager == null || manager.Sections == null)
            {
                yield break;
            }

            for (int j = 0; j < manager.ItemCountsPerSection[i]; j++)
            {
                T data = manager.ItemAt(i, j);
                yield return data;
            }

            yield break;
        }
    }

    public abstract class AbstractManager<T> : IDataManager<T>, IARLog where T : IDatabaseItem
    {
        public int TotalItemCount { get; protected set; }

        protected AbstractManager(IDB db)
        {
            this.db = db;
            this.TotalItemCount = -1;

            LoadTotalItemCount();
        }

        event EventHandler IDataManager<T>.OnDataRefreshed
        {
            add
            {
                IARLogStatic.Error("AbstractManager", "Doesn't support adding OnDataRefreshed  yet");
            }

            remove
            {
                IARLogStatic.Error("AbstractManager", "Doesn't support removingr OnDataRefreshed  yet");
            }
        }

        #region IARLog
        private bool LogAll = false;
        bool IARLog.IsARLogEnabled { get { return LogAll; } set { LogAll = value; } }
        #endregion

        protected DataFilter filter;
        internal Windowing.WindowedData<T> cursor;
        internal protected IDB db;

        protected void ShowWating()
        {

        }

        protected Type Y { set => this.cursor.Y = value; }


        #region IDataManager

        DataFilter IDataManager<T>.Filter
        {
            get => filter;
        }

        internal List<String> Sections => cursor.sections;

        internal List<int> ItemCountsPerSection => cursor.itemCountForSection;
        List<String> IDataManager<T>.Sections => this.Sections;
        List<int> IDataManager<T>.ItemCountsPerSection => this.ItemCountsPerSection;

        int IReadOnlyCollection<T>.Count { get => this.Count; }
        public int Count { get => cursor.totalItemCount; }

        T IReadOnlyList<T>.this[int index]
        {
            get => this[index];
        }

        public T this[int index]
        {
            get => cursor.ItemAt(index);
        }


        string IDataManager<T>.Summary()
        {
            IARLogStatic.Error("AbstractManager", "Doesn't support  yet");
            return "Not support yet";
        }

        void IDataManager<T>.ReplaceDB(IDB db)
        {
            this.cursor.Db = db;
            this.Reset();
            ReloadData();
        }


        void IDataManager<T>.CheckDataAvailability()
        {
            if (this.cursor.sections == null)
            {
                ReloadData();
            }
        }

        String IDataManager<T>.KeyForSection(int section)
        {
            if (section < this.cursor.sections.Count)
            {
                return this.cursor.sections[section];
            }
            Debug.Assert(false, $"Index:{section} out of bound for section:{this.cursor.sections.Count}");
            return "?";
        }

        void IDataManager<T>.ReloadData()
        {
            lock (this)
            {
                if (db.IsOpen())
                {
                    this.cursor.loadMetadataWith(filter);
                }
                else
                {
                    this.L("DB is closed, so we can't load the data");
                }
            }
        }

        public void FilterWith(DataFilter filter)
        {
            this.cursor.loadMetadataWith(filter);

        }

        void IDataManager<T>.FilterWith(DataFilter filter)
        {
            this.FilterWith(filter);

            this.LP("Manager", $"Manager[{Count:n0} is filtered with {filter}. [{this}] ");
        }
        internal T ItemAt(int section, int index)
        {
            return this.cursor.ItemAt(section, index);
        }
        T IDataManager<T>.ItemAt(int section, int index) => this.ItemAt(section, index);

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            for (int i = 0; i < this.Count; i++)
            {
                yield return this.cursor.ItemAt(i);
            }

            yield break;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<T>)this).GetEnumerator();
        }

        public IEnumerable<IGrouping<String, T>> DataForUI()
        {
            if (this.Sections == null)
            {
                return Enumerable.Empty<IGrouping<String, T>>();
            }
            return new KeyDataEnumerable<T>(this);
        }
        #endregion

        protected void LoadTotalItemCount()
        {
            this.cursor?.loadTotalItemCount();
            if (this.TotalItemCount == -1 && this.cursor != null)
            {
                this.TotalItemCount = this.cursor.totalItemCount;
            }
        }

        protected virtual void ClearData()
        {
            Debug.Assert(false, "subclass must implement this");
        }

        protected void Reset()
        {
            this.filter?.Clear();

            this.cursor.reset();
        }

        protected void ReloadData()
        {
            this.filter = null;
        }

        public virtual List<Ordering> SupportedOrdering()
        {
            return new List<Ordering>();
        }

        public Task OrderBy(Ordering ordering)
        {
            if (this.CurrentOrder != ordering)
            {
                this.cursor.queryFactory = GetQueryFactoryForOrdering(ordering);
            }

            return Task.Run(() =>
            {
                this.ReloadData();
                this.Reset();

            });
        }

        public async Task<Int32> IndexOfItem(object t)
        {
            Int32 objectID = ((T)t).dbID;

            Int32 index = await this.cursor.indexForObjectIDAsync(objectID);
            
            return index;
        }

        public virtual Ordering CurrentOrder
        {
            get => Ordering.Default;
        }
        internal virtual IQueryFactory<T> GetQueryFactoryForOrdering(Ordering ordering)
        {
            IARLogStatic.Error("AbstractManager", "Doesn't support  yet");
            return null;
        }

        public IReadOnlyList<T> GetRange(int index, int count)
        {
            if (index < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }
            if (Count < index)
            {
                return null;
            }

            var list = new List<T>();

            if (index + count > Count)
            {
                count = Count - index;
            }

            for (int i = index; i < index + count; i++)
            {
                list.Add(this[i]);
            }

            return list;
        }
        
        public static IList<string> GetSugestion(IDB db, string query, string userInput, int count = 10)
        {
            List<String> resultList = new List<string>(count);

            using (var con = db.CreateConnection())
            {
                Type[] types = new Type[]
                {
            typeof(String)
                };

                var cmd = con.CreateCommand(query, userInput, count);
                try
                {
                    var result = cmd.ExecuteDeferredQuery(types);

                    foreach (var obj in result)
                    {

                        resultList.Add((String) obj[0]); 
                        break;
                    }

                }
                catch (Exception ex)
                {
                    IARLogStatic.Error("ArtistManager", $"Failed to excute : {ex.Message}", ex);
                }
            }
            return resultList; 
        }

        protected int ExecuteQuery(string query, params Object[] p)
        {
            int count = 0;
            if (db.IsOpen())
            {
                using (var con = this.db.CreateConnection())
                {
                    var cmd = con.CreateCommand(query, p);
                    try
                    {
                        count = cmd.ExecuteNonQuery();
                    }
                    catch (Exception ex)
                    {
                        IARLogStatic.Error("SongManager", $"Failed to excute : {ex.Message}", ex);
                    }
                }
            }
            return count;
        }
        protected string ExecuteQueryForString(object param1, string query)
        {
            String str = String.Empty;
            if (db.IsOpen())
            {
                using (var con = this.db.CreateConnection())
                {
                    var cmd = con.CreateCommand(query, param1);
                    try
                    {
                        str = cmd.ExecuteScalar<String>();
                    }
                    catch (Exception ex)
                    {
                        IARLogStatic.Error("SongManager", $"Failed to excute : {ex.Message}", ex);
                    }
                }
            }
            return str;
        }

        protected int ExecuteQueryForInt(object param1, string query)
        {
            int intValue = 0;
            if (db.IsOpen())
            {
                using (var con = this.db.CreateConnection())
                {

                    var cmd = con.CreateCommand(query, param1);
                    try
                    {
                        var value = cmd.ExecuteScalar<int>();
                        intValue = value;
                    }
                    catch (Exception ex)
                    {
                        IARLogStatic.Error("SongManager", $"Failed to excute : {ex.Message}", ex);
                    }
                }
            }
            return intValue;
        }
    }
}
