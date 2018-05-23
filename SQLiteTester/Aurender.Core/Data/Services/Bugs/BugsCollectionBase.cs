using Aurender.Core.Contents.Streaming;
using Aurender.Core.Utility;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Aurender.Core.Data.Services.Bugs
{
    abstract class BugsCollectionBase<T> : StreamingCollectionBase<T> where T : IStreamingServiceObject
    {
        protected readonly Type Y;
        protected string nameForItems = String.Empty;

        public BugsCollectionBase(int bucketSize, Type y, string title) : base(bucketSize, ContentType.Bugs, title)
        {
            this.Y = y;
        }

        protected override string URLForNextData()
        {
            String orderClause = "";
            if (CurrentOrder != Contents.Ordering.Default)
            {
                orderClause = $"&{GetOrderClause()}";
            }
            int page = (CountForLoadedItems / BucketSize) + 1;
            if (urlForData.Contains("?"))
            {
                return $"{urlForData}&size={BucketSize}&page={page}{orderClause}";
            }
            else
            {
                return $"{urlForData}?size={BucketSize}&page={page}{orderClause}";
            }
        }

        protected virtual string GetOrderClause()
        {
            String orderAndDirection = "";
            switch (CurrentOrder)
            {
                case Contents.Ordering.AlbumName:
                case Contents.Ordering.ArtistName:
                case Contents.Ordering.SongName:
                case Contents.Ordering.PlaylistName:
                    orderAndDirection = "&order=NAME&orderDirection=ASC";
                    break;

                case Contents.Ordering.ReleaseDate:
                    orderAndDirection = "&order=RELEASE_DATE&orderDirection=ASC";
                    break;
                case Contents.Ordering.ReleaseDateDesc:
                    orderAndDirection = "&order=RELEASE_DATE&orderDirection=DESC";
                    break;

                case Contents.Ordering.AddedDate:
                    orderAndDirection = "&order=DATE&orderDirection=ASC";
                    break;
                case Contents.Ordering.AddedDateDesc:
                    orderAndDirection = "&order=DATE&orderDirection=DESC";
                    break;

                case Contents.Ordering.Default:
                    break;


                default:
                    this.EP($"{GetType()}", $"Doesn't support sort option for {CurrentOrder}");
                    break;
            }

            return orderAndDirection;
        }

        protected override bool ProcessItems(Dictionary<string, object> sInfo, IList<T> newItems)
        {
            bool sucess = false;
            int totalCount;

            Object obj = sInfo["pager"];
            if (obj != null)
            {
                JToken pager = obj as JToken;

                sucess = pager != null;

                if (!sucess)
                {
                    this.EP("TIDAL Collection", $"Failed to get totalNumberOfItems \n{sInfo}");
                    this.items.Clear();
                    Count = 0;
                    return sucess;
                }


                totalCount = pager["total_count"].ToObject<int>();

                if (Count == -1)
                    Count = totalCount;

                Debug.Assert(totalCount == Count);

                var items = sInfo["list"] as JArray;

                foreach (var i in items)
                {
                    var item = (T)Activator.CreateInstance(Y, i);
                    newItems.Add(item);
                    //this.LP("TIDAL Collection parsing", $"\t {track}");
                }
            }
            return sucess;
        }

        protected override async Task LoadNextDataAsync()
        {
            if (Count == this.items.Count)
                return;

            String url = URLForNextData();

            using (var response = await ServiceManager.Bugs().GetResponseByGetDataAsync(url, null))
            {
                string str = null;
                if (response != null)
                    str = await response.Content.ReadAsStringAsync();

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
    }

    abstract class BugsSearchCollectionBase<T> : 
        BugsCollectionBase<T>, IStreamingObjectCollection<T>, IStreamgingSearchResult<T>
        where T: IStreamingServiceObject
    {
        public string Keyword { get; protected set; }

        protected BugsSearchCollectionBase(int count, Type y, string title, String searchTypes) : base(count, y, title)
        {
            this.urlForData = $"search/{searchTypes}";
        }
        protected BugsSearchCollectionBase(Type y, string title, String searchTypes) : this(50, y, title, searchTypes)
        {
        }

        protected override string URLForNextData()
        {

            String url = ServiceManager.Bugs().URLFor(urlForData, $"keyword={Keyword.URLEncodedString()}");

            return url;
        }

        public Task SearchAsync(string keyword)
        {
            if (keyword == null)
            {
                keyword = String.Empty;
            }

            if (this.Keyword == keyword)
            {
                return Task.CompletedTask;
            }

            this.Keyword = keyword;
            Reset();

            return LoadNextAsync();
        }
    }
}
