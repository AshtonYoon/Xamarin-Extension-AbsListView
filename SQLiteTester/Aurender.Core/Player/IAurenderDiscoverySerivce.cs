using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Aurender.Core.Player
{
    public interface IAurenderDiscoverySerivce
    {
        //event EventHandler<IAurenderEndPoint> AurenderDiscovered;

        IEnumerable<IAurenderEndPoint> DiscoveredAurenders { get; }

        void StartDiscovery();
        void StopDiscovery();

		Task<IEnumerable<IAurenderEndPoint>> DiscoverAsync(int timeout = 1000);
    }
}