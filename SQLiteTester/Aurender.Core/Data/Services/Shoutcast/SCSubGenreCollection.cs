using System;
using System.Collections.Generic;
using Aurender.Core.Contents.Streaming;
using Newtonsoft.Json.Linq;

namespace Aurender.Core.Data.Services.Shoutcast
{

    class SCSubGenreCollection : StreamingCollectionBase<IStreamingGenre>
    {
        internal SCSubGenreCollection(String name, String genreID) : base(50, ContentType.InternetRadio, name)
        {
            this.urlForData = $"genre/secondary?parentid={genreID}";
        }
        protected override bool ProcessItems(Dictionary<string, object> info, IList<IStreamingGenre> newTracks)
        {
              bool result = false;
            if (info.ContainsKey("response"))
            {
                JToken sInfo = info["response"] as JToken;

                JToken data = sInfo["data"]["genrelist"]["genre"];

                if (data != null && data.Type == JTokenType.Array)
                {
                    foreach (JToken item in data)
                    {
                        String subGenreName = item["name"].ToObject<String>();
                        String id = item["id"].ToObject<String>();
                        int count = item["count"].ToObject<int>();
                        var genre = new SCSubGenre(subGenreName, id, count);

                        newTracks.Add(genre);
                    }
                    this.Count = newTracks.Count;
                    result = true;
                }
                else
                    IARLogStatic.Log("ShoutcastGenre", "Faeild to get genrelist");
            }
            else
                IARLogStatic.Log("ShoutcastGenre", "Faeild to get response");

            return result;
        }

        protected override string URLForNextData()
        {
            String pararm = $"limit={this.BucketSize}&offset={this.items.Count}";
            return SSShoutcast.URLForShoutcast(urlForData, pararm);
        }
    }

}