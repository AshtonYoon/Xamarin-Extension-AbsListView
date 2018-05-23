using System;
using System.Threading.Tasks;
using Aurender.Core.Contents.Streaming;
using Aurender.Core.Utility;

namespace Aurender.Core.Data.Services.Shoutcast
{

    class SCStationSearchCollection : SCStationCollection, IStreamgingSearchResult<IStreamingTrack>
    {

        public string Keyword { get; protected set; }
        internal SCStationSearchCollection(String name) : base(name, $"legacy/stationsearch?f=json&search=")
        {
        }
        protected override string URLForNextData()
        {
            String url = $"{this.urlForData}{Keyword.URLEncodedString()}";
            String pararm = $"limit={this.CountForLoadedItems},{this.BucketSize}";
            return SSShoutcast.URLForShoutcast(url, pararm);
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