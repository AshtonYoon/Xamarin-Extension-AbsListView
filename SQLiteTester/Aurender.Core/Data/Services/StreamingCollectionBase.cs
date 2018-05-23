using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using Aurender.Core.Contents;
using Aurender.Core.Contents.Streaming;
using Aurender.Core.Utility;
using Newtonsoft.Json;

namespace Aurender.Core.Data.Services
{
    [DebuggerDisplay("{ServiceType} Collection{typeof(T)} [{Title}] [{dataOrder}] : [{CountForLoadedItems}/{Count}]")]
    internal abstract class StreamingCollectionBase<T> : IStreamingObjectCollection<T>, ISortable, IARLog where T : IStreamingServiceObject
    {
        #region IARLog
        private bool LogAll = false;
        bool IARLog.IsARLogEnabled { get { return LogAll; } set { LogAll = value; } }
        #endregion

        public override string ToString()
        {
            return $"{ServiceType} Collection<{typeof(T)}> [{Title}][{dataOrder}] : [{CountForLoadedItems}/{Count}]";
        }

        protected StreamingCollectionBase(int bucketSize, ContentType cType, String title)
        {
            this.ServiceType = cType;
            this.Title = title;
            this.BucketSize = bucketSize;
            this.items = new List<T>(this.BucketSize);
            this.Count = -1;
        }

        protected String urlForData;
        protected int BucketSize { get; private set; }
        protected List<T> items;
        protected Ordering dataOrder = Ordering.Default;


        public String Title { get; private set; }

        public Int32 CountForLoadedItems => items.Count;

        public ContentType ServiceType { get; private set; }


        public event EventHandler<IStreamingObjectCollection<T>> OnCollectionUpdated;

        public T this[int index]
        {
            get
            {
                if (index < items.Count)
                    return items[index];

                if (index < this.Count)
                {
                    var v = LoadNextAsync();
                    v.Wait();

                    return this[index];
                }

                throw new OverflowException($"Index:{index} out of bound:{Count}");
            }
        }

        public int Count { get; protected set; }

        public Ordering CurrentOrder { get; protected set; }
        public Dictionary<string, List<IStreamingAlbum>> AlbumsByType { get; set ; }

        public IEnumerator<T> GetEnumerator()
        {
            return items.GetEnumerator();
        }

        public int IndexOf(T item)
        {
            return items.IndexOf(item);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        public Task LoadNextAsync()
        {
            return Task.Run(async () =>
            {
                await LoadNextDataAsync();

                NotifyContentsUpdate();
            });
        }

        protected void NotifyContentsUpdate()
        {
            Task.Run(() => OnCollectionUpdated?.Invoke(this, this)).ContinueWith(task =>
            {
                task.Exception.Handle(ex =>
                {
                    IARLogStatic.Error("Exception in Event", $"For {this} StreamingCollection.OnCollectionUpdated.", ex);
                    return true;
                });

            }, TaskContinuationOptions.OnlyOnFaulted);
        }

        protected virtual async Task LoadNextDataAsync()
        {
            if (Count == this.items.Count)
                return;

            String url = URLForNextData();

            using (var response = await WebUtil.GetResponseAsync(url))
            {
                var str = await response.Content.ReadAsStringAsync();
                if (response.IsSuccessStatusCode)
                {
                    Dictionary<String, Object> sInfo = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(str);

                    var newTracks = new List<T>(this.items);

                    bool sucess = ProcessItems(sInfo, newTracks);

                    this.items = newTracks;
                }
                else
                {
                    items.Clear();
                    Count = 0;
                }
            }
        }

        protected abstract bool ProcessItems(Dictionary<string, object> sInfo, IList<T> newTracks);

        protected abstract String URLForNextData();

        protected List<Ordering> supportedOrdering = new List<Ordering>()
        {
            Ordering.Default
        };

        public virtual List<Ordering> SupportedOrdering()
        {
            return supportedOrdering;
        }

        public virtual Task OrderBy(Ordering ordering)
        {
            if (ordering == CurrentOrder)
            {
                return Task.CompletedTask;
            }
            else
            {
                Reset();

                this.CurrentOrder = ordering;

                return LoadNextAsync();
            }
        }

        public void Reset()
        {
            items.Clear();
            Count = -1;
        }

        public IEnumerable<T> GetRange(int index, int count)
        {
            return items.GetRange(index, count);
        }

        internal HashSet<String> AllIDs()
        {
            HashSet<String> ids = new HashSet<string>();

            foreach(var item in items)
                ids.Add(item.StreamingID);

            return ids;
        }
    }

    public static class CommaSeperatedTrackIDUtility
    {
        internal static string SeperatedItemIDsBySeparator(this IList<IStreamingTrack> self, char separator = ',')
        {
            StringBuilder ids = new StringBuilder();
            var enm = self.GetEnumerator();
            if (enm.MoveNext())
            {
                ids.Append(enm.Current.StreamingID);
                while (enm.MoveNext())
                {
                    ids.Append($"{separator}{enm.Current.StreamingID}");
                }
            }
            return ids.ToString();
        }

        internal static string SeperatedItemIDsBySeparator(this IList<IStreamingServiceObject> self, char separator = ',')
        {
            StringBuilder ids = new StringBuilder();
            var enm = self.GetEnumerator();
            if (enm.MoveNext())
            {
                ids.Append(enm.Current.StreamingID);
                while (enm.MoveNext())
                {
                    ids.Append($"{separator}{enm.Current.StreamingID}");
                }
            }
            return ids.ToString();
        }

    }
}
