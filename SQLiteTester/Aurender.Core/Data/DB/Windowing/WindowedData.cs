using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Diagnostics;
using Aurender.Core.Contents;

namespace Aurender.Core.Data.DB.Windowing
{
    internal class WindowedData<T> : IARLog where T : IDatabaseItem
    {
        #region IARLog
        private bool LogAll = false;
        bool IARLog.IsARLogEnabled { get { return LogAll; } set { LogAll = value; } }
        #endregion

        const int S_Window_Size = 1000;

        internal List<String> sections { get; private set; }
        internal List<Int32> itemCountForSection { get; private set; }
        internal Int32 totalItemCount { get; private set; }

        internal Type Y;

        protected IDB _db;
        internal virtual IDB Db
        {
            get => _db;

            set
            {
                lock(this)
                {
                    if (value == _db)
                    {
                        this.LP("DB", "Database is same to old one.");
                        return;
                    }
                    if (value == null)
                    {
                        reset();
                        _db = null;
                    }
                    else
                    {
                        _db = value;
                    }
                }
            }
        }

        //fields
        internal IQueryFactory<T> queryFactory;
        
        protected Int32 currentWindowStartIndex;

        protected List<T> prevBucket;
        protected List<T> currBucket;
        protected List<T> nextBucket;

        protected readonly Object mLock = new Object();
        protected Object pLock;
        protected Object cLock;
        protected Object nLock;

        protected Task prevTask;
        protected Task nextTask;

        protected bool stopNextTask;
        protected bool stopPrevTask;

        protected EViewType viewType;

        protected DataFilter filter;

        protected IWindowedDataWatingDelegate waitUICallback;
        protected Func<IList<object>, T> constructor;

        internal WindowedData(IDB db, IQueryFactory<T> factory, EViewType vType, IWindowedDataWatingDelegate callback, Func<IList<object>, T> constructor)
        {
            this.Db = db;
            this.viewType = vType;
            this.queryFactory = factory;
            this.waitUICallback = callback;
            this.constructor = constructor;

            pLock = new object();
            nLock = new object();
            cLock = new object();
        }

        /// <summary>
        /// Get all items count
        /// </summary>
        internal virtual void loadTotalItemCount()
        {
            if (queryFactory != null && this.Db != null && this.Db.IsOpen())
            {
                String query = this.queryFactory.queryForDataCount(this.filter);

                using (var conn = this.Db.CreateConnection())
                {
                    var cmd = conn.CreateCommand(query);
                    this.totalItemCount = cmd.ExecuteScalar<int>();
                }
            }
            else
            {
                this.totalItemCount = 0;
            }

        }

        private void stopLoading()
        {
            lock (nLock)
            {
                lock (pLock)
                {
                    this.stopPrevTask = true;
                    this.stopNextTask = true;
                }
            }
        }

        internal virtual void reset()
        {
            stopLoading();
            //await cancelQueueJob();

            this.sections = null;
            this.filter = null;
            this.totalItemCount = 0;

            this.prevBucket?.Clear();
            this.nextBucket?.Clear();
            this.currBucket?.Clear();

            loadMetadataWith(new DataFilter());
        }

        virtual internal void loadMetadataWith(DataFilter newFilter)
        {
            stopLoading();
            waitTillJobIsDone();
            
            lock(this.mLock)
            {
                if (this.Db.IsOpen())
                {
                    if (!IsFilterSameTo(newFilter))
                    {
                        this.filter = newFilter;
                        this.loadTotalItemCount();
                        initMetaInfo();
                    }
                }
                else
                {
                    initMetaInfo();
                }
            }
        }

         
        private void waitTillJobIsDone()
        {
            List<Task> tasks = new List<Task>(2);

            // task가 null이 아닐 경우 추가
            // prev가 null → current index : 0
            // next가 null → current index : total count

            if (prevTask != null)
            {
                tasks.Add(prevTask);
            }
            if (nextBucket != null)
            { 
                tasks.Add(nextTask);
            }

            Task.WaitAll(tasks.ToArray());
        }

        private void cancelQueueJob()
        {
            
        }

        internal async Task<Int32> indexForObjectIDAsync(Int32 objectID)
        {
            String query = this.queryFactory.queryForIndexSearch(objectID, filter);

            // get result set and find for matching

            // if has result, find
            // else index = -1
            Int32 temp = await Task.Run(() =>
                {
                   Int32 counter = -1;
                    if (this.Db.IsOpen())
                    {
                        using (var conn = this.Db.CreateConnection())
                        {
                            var cmd = conn.CreateCommand(query);

                            var eumerator = cmd.ExecuteDeferredQuery<int>(false);

                            foreach (int objId in eumerator)
                            {
                                counter++;
                                if (objectID == objId)
                                    break;
                            }
                        }
                    }
                    return counter;
                }
            );


            return temp;
        }


        async Task<Tuple<Int32, Int32>> sectionAndIndexByObjectIDAsync(Int32 objectID)
        {
            Int32 index = await indexForObjectIDAsync(objectID);
            Int32 section = 0;
            Int32 indexInSection = 0;

            if (index > -1)
            {
                Int32 calcedIndex = 0;

                foreach (Int32 count in this.itemCountForSection)
                {
                    if (count + calcedIndex > index)
                    {
                        indexInSection = index - calcedIndex;
                        break;
                    }
                    calcedIndex += count;
                }
            }


            return new Tuple<int, int>(section, indexInSection);
        }

        internal T ItemAt(Int32 section, Int32 index)
        {
            T result = default;
            Int32 calcedIndex = index;

            if (section < this.sections.Count)
            {
                for(Int32 i = 0; i < section; i++)
                {
                    calcedIndex += this.itemCountForSection[i];
                }

                //Debug.WriteLine($"ItemAt({section}, {index}) +++> {calcedIndex}");
                result = this.ItemAt(calcedIndex);
            }
            else
            {
                //Debug.WriteLine($"+++++++++++++++++++++ +++> wrong section");
            }

            if (result == null)
                result = (T) Activator.CreateInstance(Y);

            return result;
        }

        internal T ItemAt(Int32 index)
        {
            Debug.Assert(index < this.totalItemCount, "Something wrong for index");
            T result = default;

            if (this.currentWindowStartIndex != -1 && index < totalItemCount)
                lock (cLock)
                {
                    LoadCurrentWithIndex(index);
                    Int32 calcedIndex = index - this.currentWindowStartIndex;

                    Debug.Assert(currBucket != null && calcedIndex < this.currBucket.Count,
                                    $"There is a problem to get item with index:{index}");
                    if (currBucket != null && calcedIndex < this.currBucket.Count)
                        result = this.currBucket[calcedIndex];
                    else
                        this.L("windowed data", "out of index", this.ToString());
                }
            else
            {
                this.EP("WindowedData", "Fail to load data");
                Debug.Assert(false);
            }

            if (result == null)
                result = (T) Activator.CreateInstance(Y);

            return result;
        }

        private void LoadCurrentWithIndex(Int32 index)
        {
            Int32 requestSlot = index / S_Window_Size;
            Int32 currentSlot = currentWindowStartIndex / S_Window_Size;

            if (requestSlot == currentSlot)
            {
                //this.L($"We are at the same slot:{requestSlot}, so do nothing.");
            }
            else
            {
                bool needRefreshWindow = IsRefreshNeededForCurrentSlot(index);

                if (needRefreshWindow || currentWindowStartIndex < 0)
                {
                    bool needToHideWait = checkWatiingWindow();

                    cancelQueueJob();
                    waitTillJobIsDone();

                    int gap = (currentSlot - requestSlot);
                    
                    
                    switch(gap)
                    {
                        case -1:
                            switchToNext();//.Wait();
                            break;

                        case 0:
                            this.L($"We are at the same page, so do nothing. Index:{index}, requestPage:{requestSlot}, currentSlot:{currentSlot}");
                            break;

                        case 1:
                            switchToPrev();// Wait();
                            break;

                        default:
                            {

                                currentWindowStartIndex = (index / S_Window_Size) * S_Window_Size;
                                this.L($"CurrentWindowStartIndex changed to {currentWindowStartIndex}");

                                var indexPath = this.indexPathForPosition(currentWindowStartIndex);
                                this.currBucket = dataAt(indexPath, DataLoadingTaskType.Current);
                               
                                this.nextBucket = null;
                                this.nextTask = enqueueJob(LoadNextData, currentWindowStartIndex);

                                this.prevBucket = null;
                                this.prevTask = enqueueJob(LoadPrevData, currentWindowStartIndex);
                            }
                            break;
                    }
                    

                    hideWaitingWindows(needToHideWait);
                }
                
            }

        }


        private void LoadPrevData(Int32 expectedCurPos)
        {
            if (stopPrevTask)
                return;

            if (expectedCurPos != currentWindowStartIndex || expectedCurPos < 0 || sections == null)
            {
                this.E($"loadPrevData skip since requested index:{expectedCurPos} is different from current:{currentWindowStartIndex}");
                return;
            }

            lock (pLock)
            {
                if (currentWindowStartIndex >= S_Window_Size)
                {
                    Int32 index = currentWindowStartIndex - S_Window_Size;
                    var indexPath = indexPathForPosition(index);

                    stopPrevTask = false;
                    this.prevBucket = dataAt(indexPath, DataLoadingTaskType.Previous);

                    validatePrevBucket();
                }
                else
                {
                    this.L("We are at the front");
                    prevBucket?.Clear();
                }
            }
        }
        private void LoadNextData(Int32 expectedCurPos)
        {
            if (stopNextTask)
                return;

            if (currentWindowStartIndex != expectedCurPos || expectedCurPos < 0 || sections == null)
            {
                this.E($"loadNextData skip since requested index:{expectedCurPos} is different from current:{currentWindowStartIndex}");
                return;
            }


            lock(nLock)
            {
                Int32 checker = totalItemCount - currentWindowStartIndex - S_Window_Size;

                if (checker > 0)
                {
                    var indexPath = indexPathForPosition(currentWindowStartIndex + S_Window_Size);

                    stopNextTask = false;
                    this.nextBucket = dataAt(indexPath, DataLoadingTaskType.Next);

                    validateNextBucket(checker);
                }
                else
                {
                    this.L("We are at the end");
                    nextBucket?.Clear();
                }
            }

        }
        [Conditional("DEBUG")]
        private void validateNextBucket(Int32 checker)
        {
            if (checker >= S_Window_Size)
            {
                Debug.Assert(nextBucket.Count == S_Window_Size, 
                    $"Next bucket item count:{nextBucket.Count} is not same we expected:{S_Window_Size}.");
            }
            else
            {
                Debug.Assert(nextBucket.Count == totalItemCount % S_Window_Size,
                    $"Next bucket Item count:{nextBucket.Count} is not same as we expected:{totalItemCount % S_Window_Size}");
            }
        }


        internal Tuple<Int32, Int32> indexPathForPosition(Int32 position)
        {
            Int32 section = 0;
            Int32 index = 0;
            Int32 calcedIndex = 0;

            if (totalItemCount > 0)
            {
                if (position >= totalItemCount)
                {
                    this.L($"Index:{position} must be less than totalItem:{this.totalItemCount}");
                }
                else
                {
                    
                    foreach (var count in itemCountForSection)
                    {
                        if (position < calcedIndex + count)
                        {
                            index = position - calcedIndex;

                            this.L($"loadIndexForPosition {position} translated to {section}:{index}");
                            break;
                        }
                        section++;
                        calcedIndex += count;
                    }

                }
            }

            return new Tuple<int, int>(section, index);
        }

        private void switchToPrev()
        {
            if (this.currentWindowStartIndex >= S_Window_Size || this.prevBucket.Count == 0)
            {
                waitTillJobIsDone();

                if (this.prevBucket== null || this.prevBucket.Count == 0)
                {
                    stopPrevTask = false;
                    LoadPrevData(this.currentWindowStartIndex);
                }

                lock(pLock)
                {
                    lock(nLock)
                    {
                        this.nextBucket = this.currBucket;
                        this.currBucket = this.prevBucket;
                        this.prevBucket = null;

                        this.currentWindowStartIndex -= S_Window_Size;
                    }
                }

                this.prevTask = enqueueJob(LoadPrevData, this.currentWindowStartIndex);
            }
            else
            {
                this.L("Cannot switch to prev since we are at the front");
            }

        }

        private void switchToNext()
        {
            Int32 checker = totalItemCount - S_Window_Size;

            if (this.currentWindowStartIndex <= checker)
            {
                waitTillJobIsDone();

                if (this.nextBucket == null || this.nextBucket.Count == 0)
                {
                    stopNextTask = false;
                    LoadNextData(this.currentWindowStartIndex);
                }

                lock(pLock)
                {
                    lock(nLock)
                    {
                        this.prevBucket = this.currBucket;
                        this.currBucket = this.nextBucket;
                        this.nextBucket = null;

                        this.currentWindowStartIndex += S_Window_Size;
                    }
                }

                this.nextTask = enqueueJob(LoadNextData, this.currentWindowStartIndex);
            }
            else
            {
                this.L("Cannot switch to next since we are at the front");
            }

        }

        List<T> dataAt(Tuple<Int32, Int32> indexPath, DataLoadingTaskType tType)
        {
            List<T> t = new List<T>(S_Window_Size);


            if (totalItemCount > 0 && this.Db.IsOpen())
            {
                Int32 section = indexPath.Item1;
                Int32 index = indexPath.Item2;

              

                Func<bool> shouldStop;//= () => false;


                object l;
                
                switch (tType)
                {
                    case DataLoadingTaskType.Previous:
                        l = pLock;
                        shouldStop = () => this.stopPrevTask;
                        break;
                    case DataLoadingTaskType.Next:
                        l = nLock;
                        shouldStop = () => this.stopNextTask;
                        break;

                    default:
                        l = cLock;
                        shouldStop = () => false;
                        break;
                }



                lock (l)
                {
                    var sectionMark = this.sections[section];
                    using (var conn = this.Db.CreateConnection())
                    {
                        var query = this.queryFactory.queryForData(this.filter);
                        var cmd = conn.CreateCommand(query, sectionMark.ToString(), S_Window_Size, index);


                        var result = cmd.ExecuteDeferredQuery(this.queryFactory.dataTypes);

                        int counter = 0;
                        foreach (var objects in result)
                        {
                            var item = constructor(objects);
                            t.Add(item);

                            counter++;
                            if (counter == S_Window_Size || shouldStop())
                                break;


                        }
                    }
                }

            }
            else
            {
                this.L("There is no data load for dataAtPosition.");
            }
            
            return t;
        }

        private void initMetaInfo()
        {
            //this.totalItemCount = 0;

            var sect = new List<String>(30);
            var countPerSect = new List<Int32>(sect.Capacity);
            prepareSection(sect, countPerSect);
            
            this.sections = sect;
            this.itemCountForSection = countPerSect;

            this.L("Manager", $"Manager [{this.totalItemCount}] {this}");

            this.currentWindowStartIndex = S_Window_Size * (-2);

            this.prevBucket = null;
            this.currBucket = null;
            this.nextBucket = null;

            this.stopPrevTask = false;
            this.stopNextTask = false;

            this.LoadCurrentWithIndex(0);
        }

        private void prepareSection(List<String> sect, List<int> countPerSect)
        {
            if (!Db.IsOpen())
            {
                return;
            }

            using (var conn = Db.CreateConnection())
            {
                var query = this.queryFactory.queryForMeta(filter);


                var cmd = conn.CreateCommand(query);

                var enumabler = cmd.ExecuteDeferredQuery<SectionIndexQueryRow>();

                int total = 0;
                foreach (SectionIndexQueryRow row in enumabler)
                {
                    sect.Add(row.key);
                    total += row.cnt;
                    this.LP("Index", $"[{row.key}] [{row.cnt}]");
                    countPerSect.Add(row.cnt);
                }
                this.LP("DB Summary", $"Total[{total}] totalitemCount[{totalItemCount}]");

                // Debug.Assert(total == this.totalItemCount, $"Unexpected total count : {totalItemCount:n0}, actual : {total:n0}");
                //Debug.Assert(sect.Count > 0, "Section count must be bigger than 0");
            }

        }

        private Task enqueueJob(Action<int> job, int currentWindowStartIndex)
        {
            return Task.Run(() => {
                //job(currentWindowStartIndex);
                //if (job == loadPrevData)
                //    this.LP("Cursor", $"Enqueue prevData for {currentWindowStartIndex}");
                //else 
                //    this.LP("Cursor", $"Enqueue nextData for {currentWindowStartIndex}");
            });
        }

        private bool checkWatiingWindow()
        {
            bool result = false;

            if (this.waitUICallback != null && this.viewType == this.waitUICallback.ActiveViewType)
            {
                this.waitUICallback.ShowWaitingPopupWith();
                result = true;
            }

            return result;
        }

        private void hideWaitingWindows(bool shouldHide)
        {
            if (shouldHide)
               this.waitUICallback.HideWaitingPopup();
        }

        private bool IsRefreshNeededForCurrentSlot(int index)
        {
            bool result = false;
            if (true)
            {
                Int32 checker = Math.Abs(currentWindowStartIndex - index);
                if (checker > S_Window_Size - 1)
                    result = true;

                if (index < currentWindowStartIndex)
                {
                    result = true;
                }
                return result;
            }
            //if (index < currentWindowStartIndex)
            //{
            //    Int32 checker = currentWindowStartIndex - index;
            //    if (checker > 0)
            //        result = true;

            //}
            //else
            //{
            //    Int32 checker = index - currentWindowStartIndex;
            //    if (checker > S_Window_Size - 1)
            //        result = true;
            //}

            //return result; 
        }

        private bool IsFilterSameTo(DataFilter newFilter)
        {
            bool result = false;
            if (!this.Db.IsOpen())
            {
            }
            else if (this.filter != null && newFilter.Count == this.filter.Count)
            {
                foreach (var item in newFilter)
                {
                    if (filter.ContainsKey(item.Key))
                    {
                        var obj = filter[item.Key];

                        bool eqauls = filter[item.Key].Equals(item.Value);
                        if (!eqauls) return false;
                    }

                    return false;
                }

                result = true;
            }

            return result;
        }

        [Conditional("DEBUG")]
        private void validatePrevBucket()
        {
            Debug.Assert(prevBucket.Count == S_Window_Size, 
                $"Prev bucket Item count:{prevBucket.Count} is not same we expected:{S_Window_Size}.");
        }

        internal void SetFilter(DataFilter newFilter)
        {
           this.filter = newFilter; 
        }
    }


    internal class WindowedDataForSong :WindowedData<ISongFromDB>
    {
        internal WindowedDataForSong(IDB db, IQueryFactory<ISongFromDB> factory, EViewType vType, IWindowedDataWatingDelegate callback, Func<IList<object>, ISongFromDB> constructor) : base(db, factory, vType, callback, constructor)
        {
        }

        internal void UpdateRating(IRatableDBItem song, int rating)
        {
            Func<ISongFromDB, bool> checker = s => (s.dbID == song.dbID);

            var result = this.currBucket.FirstOrDefault(checker);
            if (result == null)
            {
                result = this.nextBucket?.FirstOrDefault(checker);
                
                if (result == null)
                    result = this.nextBucket?.First(checker);
            }

            if (result != null)
            {
                Song songInCache = result as Song;

                songInCache.Rating = (byte) rating;
            }
            else
            {
                this.L($"[SongRate] Not in cache {song}");
            }

        }
    }

}
