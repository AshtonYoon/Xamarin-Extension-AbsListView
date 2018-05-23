using System;
using System.Collections.Generic;
using Aurender.Core.Contents.Streaming;
using System.Diagnostics;
using Newtonsoft.Json.Linq;

namespace Aurender.Core.Data.Services.TIDAL
{

    abstract class TIDALCollectionBase<T> : StreamingCollectionBase<T> where T : IStreamingServiceObject
    {

        protected readonly Func<JToken, T> constructor;
        public TIDALCollectionBase(int bucketSize, string title, Func<JToken, T> constructor) : base(bucketSize, ContentType.TIDAL, title)
        {
            this.constructor = constructor;
        }
        protected override string URLForNextData()
        {
            String orderClause = "";
            if (CurrentOrder != Contents.Ordering.Default)
            {
                orderClause = GetOrderClause();
                if (orderClause.Length > 0 && !orderClause.StartsWith("&")) {
                    orderClause = $"&{orderClause}";
                }
            }
            return $"{urlForData}&limit={BucketSize}&offset={CountForLoadedItems}{orderClause}";
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
            bool sucess = int.TryParse(sInfo["totalNumberOfItems"].ToString(), out totalCount);

            if (!sucess)
            {
                this.EP("TIDAL Collection", $"Failed to get totalNumberOfItems \n{sInfo}");
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
                var item = constructor(i);
                newItems.Add(item);
                //this.LP("TIDAL Collection parsing", $"\t {track}");
            }

            return sucess;
        }

        //protected virtual bool ProcessItems(IList<object> sInfo, IList<T> newItems)
        //{
        //    int totalCount = 0;

        //    foreach (var i in sInfo)
        //    {
        //        var item = (T)Activator.CreateInstance(Y, i);
        //        newItems.Add(item);
        //        //this.LP("TIDAL Collection parsing", $"\t {track}");
        //        totalCount++;
        //    }
        //    this.Count = totalCount;

        //    return true;
        //}
    }

}