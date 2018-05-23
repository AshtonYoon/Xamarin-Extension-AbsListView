using System;
using System.Collections.Generic;
using Aurender.Core.Contents.Streaming;
using Aurender.Core.Utility;
using Newtonsoft.Json.Linq;

namespace Aurender.Core.Data.Services.Shoutcast
{

    class SCGenreCollection : StreamingCollectionBase<IStreamingGenre>
    {
        internal SCGenreCollection(String api) : base(50, ContentType.InternetRadio, LocaleUtility.Translate("BrowseByGenre"))
        {
            this.urlForData = api;
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
                        if (item["haschildren"].ToObject<bool>() == true)
                        {
                            var genre = new SCGenre(item);

                            newTracks.Add(genre);
                        }
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
            return SSShoutcast.URLForShoutcast(urlForData);
        }
    }

}