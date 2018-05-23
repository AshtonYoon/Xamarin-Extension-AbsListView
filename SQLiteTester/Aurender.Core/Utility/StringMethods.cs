using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aurender.Core.Utility
{
    public static class StringMethod
    {
        public static String URLEncodedString(this String str)
        {
            return System.Net.WebUtility.UrlEncode(str);
        }

        public static T ToEnum<T>(this string str, T defaultValue) where T : struct
        {
            if (string.IsNullOrEmpty(str))
            {
                return defaultValue;
            }

            T result;
            bool sucess = Enum.TryParse<T>(str, true, out result);

            if (sucess)
                return result;
            
            return defaultValue;
        }
    }
}
