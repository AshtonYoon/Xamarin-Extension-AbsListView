using System;
using System.Linq;
using System.Collections.Generic;
using Aurender.Core.Contents.Streaming;
using Aurender.Core.Player;
using Aurender.Core.Utility;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Aurender.Core.Data.Services.Shoutcast
{

    class SCRecommendedStionCollection : SCFavoriteStationCollection
    {
        internal SCRecommendedStionCollection(String name, String api) : base(name, api) 
        {

        }

    }

}