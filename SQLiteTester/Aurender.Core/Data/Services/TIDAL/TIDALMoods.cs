using System;
using Aurender.Core.Contents.Streaming;
using Newtonsoft.Json.Linq;

namespace Aurender.Core.Data.Services.TIDAL
{

    internal class TIDALMoods : TIDALGenre
    {

        public override bool Equals(object obj)
        {
            TIDALMoods b = obj as TIDALMoods;
            if (b != null)
            {
                return (b.ServiceType == this.ServiceType) && (b.StreamingID.Equals(this.StreamingID));
            }

            return false;
        }
        public override int GetHashCode()
        {
            return $"{ServiceType.GetName()}:m:{this.StreamingID}".GetHashCode();
        }

        public override string ToString()
        {
            return $"{ServiceType} Mood { StreamingID} : [{Name}]";
        }

        public TIDALMoods(JToken token) : base(token)
        {
            this.supportsAlbums = false;
            this.supportsPlaylists = true;
        }
        public override string ImageUrl => String.Format(albumCover, 320, 320);

        public override string ImageUrlForMediumSize => String.Format(albumCover, 426, 426);

        public override string ImageUrlForLargeSize => String.Format(albumCover, 426, 426);

    }

}