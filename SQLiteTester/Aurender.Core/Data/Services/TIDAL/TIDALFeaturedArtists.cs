using Aurender.Core.Contents.Streaming;
using System;

namespace Aurender.Core.Data.Services.TIDAL
{

    class TIDALFeaturedArtists : TIDALCollectionBase<IStreamingArtist>
    {
        public TIDALFeaturedArtists(string title, string url) : base(50, title, token => new TIDALArtist(token))
        {
            this.urlForData = ServiceManager.Service(ContentType.TIDAL).URLFor(url);
        }
    }

    class TIDALSearchResultForArtists : TIDALSearchCollectionBase<IStreamingArtist>
    {
        public TIDALSearchResultForArtists(String title) : base (50, title, "ARTISTS", token => new TIDALArtist(token))
        {
            
        }
    }

}
