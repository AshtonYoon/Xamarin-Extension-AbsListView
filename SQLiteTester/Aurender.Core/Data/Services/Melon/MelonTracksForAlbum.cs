using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Aurender.Core.Contents.Streaming;
using Newtonsoft.Json.Linq;

namespace Aurender.Core.Data.Services.Melon
{

    internal class MelonTracksForAlbum : MelonCollectionBase<IStreamingTrack>
    {

        public MelonTracksForAlbum(MelonAlbum melonAlbum) : base(melonAlbum.AlbumTitle, token => new MelonTrack(token))
        {
            this.urlForData = $"detail/listSongAlbum.json?albumId={melonAlbum.StreamingID}&imgW=300&imgH=300";
            this.Count = -1;
        }


        //public Dictionary<string, List<IStreamingAlbum>> AlbumsByType {
        //    get => throw new System.NotImplementedException();
        //    set => throw new System.NotImplementedException();
        //}

  protected override bool ProcessItems(Dictionary<string, object> sInfo, IList<IStreamingTrack> newItems)
        {
            Object obj;

            if (sInfo.ContainsKey("ERRORMSG"))
            {
                this.EP("Melon Collection", $"Failed to get data {sInfo["ERRORMSG"]}\n{sInfo}");
                this.Count = 0;
                return true;
            }

            obj = sInfo["CDLIST"];

            if (obj != null)
            {
                Object hasMore = null;
                if (sInfo.ContainsKey("COUNT"))
                    hasMore = sInfo["COUNT"];


                JArray cds = obj as JArray;

                foreach (var cd in cds)
                {
                    JArray songs = cd["SONGLIST"] as JArray;

                    foreach (var i in songs)
                    {
                        if (i["SONGID"] != null)
                        {
                            var item = constructor(i);
                            newItems.Add(item);
                        }
                    }
                }
                this.Count = newItems.Count;
                return true;
            }
            this.EP("Melon Collection", $"Failed to get totalNumberOfItems \n{sInfo}");
            this.items.Clear();
            Count = 0;
            return false;

        }
     }
     internal class MelonTracksForPlaylist : MelonFeaturedTracks
    {

        public MelonTracksForPlaylist(MelonPlaylist melonAlbum) : base(400, melonAlbum.Name, $"melondj/playListInform.json?plylstSeq={melonAlbum.StreamingID}&imgW=300&imgH=300", token => new MelonTrack(token))
        {
        }

    }

}