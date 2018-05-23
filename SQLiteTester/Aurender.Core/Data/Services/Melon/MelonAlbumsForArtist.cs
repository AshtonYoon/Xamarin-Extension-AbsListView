using System;

using Newtonsoft.Json.Linq;

using Aurender.Core.Contents.Streaming;
using System.Collections.Generic;
using System.Diagnostics;

namespace Aurender.Core.Data.Services.Melon
{
    internal class MelonAlbumsForArtist : MelonCollectionBase<IStreamingAlbum>
    {
        private MelonArtist melonArtist;

        internal MelonAlbumsForArtist(MelonArtist melonArtist) : base("AlbumsForArtist", token => new MelonAlbum(token))
        {
            this.melonArtist = melonArtist;
            this.urlForData = $"detail/listArtistAlbum.json?artistId={melonArtist.StreamingID}&listType=A&orderBy=NEW&pageSize=300&imagW=300&imgH=300";
        }


        internal MelonAlbumsForArtist(MelonArtist melonArtist, JToken data) : this(melonArtist)
        {
            JToken contents = data["CONTENTS"];

            Debug.Assert(contents != null && contents.Type == JTokenType.Array);

            JArray albums = contents as JArray;

            foreach (var album in albums)
            {
                var melonAlbum = new MelonAlbum(album);
                this.items.Add(melonAlbum);
            }

            this.Count = data["COUNT"].ToObject<int>();
        }

    }
}