using System;

namespace Aurender.Core.Player
{

    public interface IAurenderEndPoint
    {
        String Name { get; }

        String IPV4Address { get; }
        String IPV6Address { get; }

        Int32 WebPort { get; }
		Int32 Port { get; }
    }

    public static class IAurenderEndPointUtil
    {
        public static String ConnectionDescription(this IAurenderEndPoint endPoint)
        {
            return $"Aurender [{endPoint.Name}], IP : {endPoint.IPV4Address}, WebPort: {endPoint.WebPort}";
        }
        /// <summary>
        /// returns http url with path
        /// </summary>
        /// <param name="endPoing"></param>
        /// <param name="path">If it has parameters, it must be URL encoded.</param>
        /// <returns></returns>
        public static String WebURLFor(this IAurenderEndPoint endPoing, String path)
        {
            return $"http://{endPoing.IPV4Address}:{endPoing.WebPort}/{path}";
        }
    }
}