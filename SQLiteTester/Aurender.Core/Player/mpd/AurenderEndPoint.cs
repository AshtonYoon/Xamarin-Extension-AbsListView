using System;
using System.Diagnostics;

namespace Aurender.Core.Player.mpd
{
    [DebuggerDisplayAttribute("Aurender endpoint : {Name} {IPV4Address}")]
    public class AurenderEndPoint : Object, IAurenderEndPoint
    {
        public AurenderEndPoint(String name, String ipV4, Int32 port = 12019, Int32 webPort = 80, String ipV6 = "")
        {
            _name = name;
            _ipv4 = ipV4;
            _ipv6 = ipV6;
            _port = port;
            _webPort = webPort;
        }

        private  String _name;
        private readonly String _ipv4;
        private readonly String _ipv6;
        private readonly Int32 _port;
        private readonly Int32 _webPort;

        String IAurenderEndPoint.Name { get => _name; }

        String IAurenderEndPoint.IPV4Address { get => _ipv4; }

        String IAurenderEndPoint.IPV6Address { get => _ipv6; }

        Int32 IAurenderEndPoint.WebPort { get => _webPort; }

        Int32 IAurenderEndPoint.Port { get => _port; }

        public override string ToString()
        {
            // return "[AurenderEndPoint: {Name}] {IPV4Address}:{Port}";

            return $"{_name} : {_ipv4}";
        }

        internal void UpdateName(String newName)
        {
            _name = newName;
        }
   
    }

}
