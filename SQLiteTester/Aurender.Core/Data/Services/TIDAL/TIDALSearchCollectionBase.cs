using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aurender.Core.Contents.Streaming;
using Aurender.Core.Utility;
using System.Diagnostics;
using Newtonsoft.Json.Linq;

namespace Aurender.Core.Data.Services.TIDAL
{

    abstract class TIDALSearchCollectionBase<T> : 
        TIDALCollectionBase<T>, IStreamingObjectCollection<T>, IStreamgingSearchResult<T>
         where T : IStreamingServiceObject
    {
     
        protected readonly String searchTypes;

        public string Keyword { get; protected set; }
        
        protected TIDALSearchCollectionBase(int count, string title, String searchTypes, Func<JToken, T> constructor) : base(count, title, constructor)
        {
            this.searchTypes = searchTypes;
            this.urlForData = "search/";
        }
        protected TIDALSearchCollectionBase(string title, String searchTypes, Func<JToken, T> constructor) : this(50, title, searchTypes, constructor)
        {
        }

        protected override string URLForNextData() {
            
            String url = ServiceManager.Service(ContentType.TIDAL).URLFor("search/", $"query={Keyword.URLEncodedString()}&limit={BucketSize}&offset={CountForLoadedItems}&types={searchTypes}");

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


        protected override bool ProcessItems(Dictionary<string, object> totalInfo, IList<T> newItems)
        {
            bool sucess = false;
            JToken sInfo = totalInfo[searchTypes.ToLower()] as JToken;

            if (sInfo != null)
            {

                int totalCount;
                sucess = int.TryParse(sInfo["totalNumberOfItems"].ToString(), out totalCount);

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
            }
            return sucess;
        }
    }

}