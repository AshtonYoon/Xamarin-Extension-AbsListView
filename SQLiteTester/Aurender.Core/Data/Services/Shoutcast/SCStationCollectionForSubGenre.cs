using System;
using Aurender.Core.Utility;

namespace Aurender.Core.Data.Services.Shoutcast
{

    class SCStationCollectionForSubGenre : SCStationCollection
    {
        internal SCStationCollectionForSubGenre(String name, int count) : base(name, $"/legacy/genresearch?genre={name.URLEncodedString()}")
        {
            this.Count = count;
        }
    }

}