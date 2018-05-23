using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Aurender.Core.Contents.Streaming;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Aurender.Core.Data.Services.Qobuz
{
    abstract class QobuzCollectionBase<T> : StreamingCollectionBase<T> where T : IStreamingServiceObject
    {
        protected readonly Type Y;

        public QobuzCollectionBase(int bucketSize, Type y, string title) : base(bucketSize, ContentType.Qobuz, title)
        {
            this.Y = y;
        }

        protected override string URLForNextData()
        {
            String orderClause = "";
            if (CurrentOrder != Contents.Ordering.Default)
            {
                orderClause = GetOrderClause();
            }
            if (urlForData.IndexOf('?') > 0)
                return $"{urlForData}&limit={BucketSize}&offset={CountForLoadedItems}&{orderClause}";
            else
                return $"{urlForData}?limit={BucketSize}&offset={CountForLoadedItems}&{orderClause}";
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
            int totalCount;
            bool sucess = int.TryParse(sInfo["total"].ToString(), out totalCount);

            if (!sucess)
            {
                this.EP("Qobuz Collection", $"Failed to get totalNumberOfItems \n{sInfo}");
                this.items.Clear();
                Count = 0;
                return sucess;
            }

            if (Count == -1)
                Count = totalCount;

            Debug.Assert(totalCount == Count);

            var items = sInfo["items"] as JArray;

            foreach (var i in items)
            {
                var item = (T)Activator.CreateInstance(Y, i);
                newItems.Add(item);
                //this.LP("Qobuz Collection parsing", $"\t {track}");
            }

            return sucess;
        }

        protected bool ProcessItems(JToken sInfo, IList<T> newItems)
        {
            JToken total = sInfo?["total"];
            if (total != null)
            {
                int totalCount = total.ToObject<Int32>();

                if (Count == -1)
                    Count = totalCount;

                Debug.Assert(totalCount == Count);

                var items = sInfo["items"] as JArray;

                foreach (var i in items)
                {
                    var item = (T)Activator.CreateInstance(Y, i);
                    newItems.Add(item);
                    //this.LP("Qobuz Collection parsing", $"\t {track}");
                }

                return true;
            }

            this.EP("Qobuz Collection", $"Failed to get totalNumberOfItems \n{sInfo}");
            this.items.Clear();
            Count = 0;
            return false;

        }

        protected bool ProcessItems(JToken token)
        {
            var newItems = new List<T>(this.items);

            bool sucess = this.ProcessItems(token, newItems);

            if (sucess)
                this.items = newItems;

            return sucess;
        }

        protected override async Task LoadNextDataAsync()
        {
            if (Count == this.items.Count)
                return;

            String url = URLForNextData();
            url = ServiceManager.Qobuz().URLFor(url);
            using (var response = await ServiceManager.Qobuz().GetResponseByGetDataAsync(url, null))
            {
                if (response != null && response.IsSuccessStatusCode)
                {
                    var str = await response.Content.ReadAsStringAsync();

                    Dictionary<String, Object> sInfo = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(str);

                    var newTracks = new List<T>(this.items);
                    bool sucess = ProcessItems(sInfo, newTracks);

                    this.items = newTracks;
                }
                else
                {
                    items.Clear();
                    Count = 0;
                    if (response != null)
                    {
                        var str = await response.Content.ReadAsStringAsync();
                        this.EP("Qobuz", $"Failed to parse for {this} : {str} ");
                    }
                    else
                    {
                        this.EP("Qobuz", $"Failed to parse (result is null) for {url}");
                    }
                }
            }
        }
    }
}
