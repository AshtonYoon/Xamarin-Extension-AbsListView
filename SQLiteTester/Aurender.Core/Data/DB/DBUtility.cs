using System;

namespace Aurender.Core.Data.DB
{
    public static class DBUtility

    {
        /// <summary>
        /// If days = 1, it means yesterday.
        /// </summary>
        /// <param name="days"></param>
        /// <returns></returns>
        internal static Int32 InSecToMinusDaysFromTodayToLinuxSecFrom1970(Int32 days)
        {
            Int32 sec = 0;

            DateTime origin = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            TimeSpan diff = DateTime.Today.AddDays(days * -1).ToUniversalTime() - origin;

            sec = (Int32) diff.TotalSeconds;

            return sec;
        }
    }
}
