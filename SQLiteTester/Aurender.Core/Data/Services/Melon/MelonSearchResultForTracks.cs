using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections;
using System.Threading.Tasks;
using Aurender.Core.Contents.Streaming;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using Newtonsoft.Json;

namespace Aurender.Core.Data.Services.Melon
{

    internal class MelonSearchResultForTracks : MelonSearchCollection<IStreamingTrack>
    {
        internal MelonSearchResultForTracks(string title) : base(50, title, token => new MelonTrack(token))
        {
            this.urlForData = "search/listSearchSong.json";
            this.fieldName = "SONGS";
        }
/*
        protected override bool ProcessItems(Dictionary<string, object> sInfo, IList<IStreamingTrack> newItems)
        {
            bool sucess = false;
            int totalCount;

            Object obj = sInfo["CONTENTS"];
            Object hasMore = sInfo["HASMORE"];
            if (obj != null && hasMore != null)
            {
                JArray contents = obj as JArray;
                bool more = (bool) hasMore;


                totalCount = this.CountForLoadedItems + contents.Count + (more ? 1 : 0);

                if (Count == -1)
                    Count = totalCount;

                Debug.Assert(totalCount == Count);

                foreach (var i in contents)
                {
                    var item = (IStreamingTrack)Activator.CreateInstance(Y, i);
                    newItems.Add(item);
                    //this.LP("TIDAL Collection parsing", $"\t {track}");
                }
            }

            if (hasMore == null)
                {
                    this.EP("TIDAL Collection", $"Failed to get totalNumberOfItems \n{sInfo}");
                    this.items.Clear();
                    Count = 0;
                    return sucess;
                }

           return sucess;
        }
 */
    }

}