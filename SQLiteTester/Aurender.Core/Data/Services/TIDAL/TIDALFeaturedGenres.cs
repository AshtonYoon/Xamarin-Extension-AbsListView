using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aurender.Core.Contents.Streaming;
using Aurender.Core.Utility;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Aurender.Core.Data.Services.TIDAL
{

    class TIDALFeaturedGenres : TIDALCollectionBase<IStreamingGenre>
    {
        protected String checkerField;
        internal protected TIDALFeaturedGenres(string title, string url, Func<JToken, IStreamingGenre> constructor) : base(50, title, constructor)
        {
            this.urlForData = ServiceManager.Service(ContentType.TIDAL).URLFor(url);
            checkerField = "hasAlbums";
        }
        internal TIDALFeaturedGenres(string title, string url) : this(title, url, token => new TIDALGenre(token))
        {
        }

        protected override async Task LoadNextDataAsync()
        {
            if (Count == this.items.Count)
                return;

            String url = URLForNextData();

            using (var response = await WebUtil.GetResponseAsync(url))
            {
                var str = await response.Content.ReadAsStringAsync();
                if (response.IsSuccessStatusCode)
                {

                    List<Object> sInfo = JsonConvert.DeserializeObject<List<dynamic>>(str);

                    var newTracks = new List<IStreamingGenre>(this.items);

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

        protected bool ProcessItems(IList<object> sInfo, IList<IStreamingGenre> newItems)
        {
            if (Count != -1)
            {
                this.EP("TIDAL Collection", $"Failed to get totalNumberOfItems \n{sInfo}");
                //    this.items.Clear();
                //   Count = 0;
                return true;
            }

            int totalCount = 0;
            foreach (var i in sInfo)
            {
                JToken token = i as JToken;
                if (token != null && token[checkerField] != null)
                {
                    if (token[checkerField].ToObject<bool>())
                    {
                        var item = constructor(token);
                        newItems.Add(item);
                        totalCount++;
                    }
                }
                //this.LP("TIDAL Collection parsing", $"\t {track}");
            }

            Count = totalCount;

            return Count != -1;
        }
    }

    class TIDALFeaturedMoods : TIDALFeaturedGenres
    {
        internal TIDALFeaturedMoods(string title, string url) : base(title, url, token => new TIDALMoods(token))
        {
            checkerField = "hasPlaylists";
        }

    }
}