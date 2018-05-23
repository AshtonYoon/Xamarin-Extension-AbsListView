using Newtonsoft.Json.Linq;

namespace Aurender.Core.Data.Services.Melon
{

    class MelonDJAlbum : MelonPlaylist
    {
        public MelonDJAlbum(JToken token) : base(token)
        {
        }

        public override string ImageUrl => base.ImageUrl;

        public override string ImageUrlForMediumSize => base.ImageUrlForMediumSize;

        public override string ImageUrlForLargeSize => base.ImageUrlForLargeSize;

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override string ToString()
        {
            return base.ToString();
        }
    }

}