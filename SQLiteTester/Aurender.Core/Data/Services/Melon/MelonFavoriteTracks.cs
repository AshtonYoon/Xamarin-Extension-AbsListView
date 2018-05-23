using System.Collections.Generic;
using Aurender.Core.Contents.Streaming;

namespace Aurender.Core.Data.Services.Melon
{

    internal class MelonFavoriteTracks : MelonFavoriteCollection<IStreamingTrack>
    {
        internal MelonFavoriteTracks(string title) : base(1000, title, token => new MelonTrack(token))
        {
            this.urlForData = "mymusic/likeSongList.json?imgW=300&imgH=300";
        }
        protected override bool ProcessItems(Dictionary<string, object> sInfo, IList<IStreamingTrack> newItems)
        {
            return base.ProcessItems(sInfo, newItems);
        }

        /*     protected override bool ProcessItems(Dictionary<string, object> sInfo, IList<IStreamingTrack> newItems)
             {
                 bool sucess = false;
                 int totalCount;
                 if (sInfo.ContainsKey("ERRORMSG"))
                 {
                     this.EP("Melon Collection", $"No favorite albums." );
                     return true;
                 }

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