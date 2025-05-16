using System;

namespace org.wakanda.framework.Utilities
{

    public static class UnixTimeHelper
    {
        private static readonly DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);


        public static long ToUnixTimeSeconds(this DateTime dateTime)
        {
            if (dateTime == DateTime.MinValue)
                return 0;

            return (long)(dateTime.ToUniversalTime() - UnixEpoch).TotalSeconds;
        }


        public static DateTime FromUnixTimeSeconds(long seconds)
        {
            if (seconds <= 0)
                return DateTime.MinValue;

            return UnixEpoch.AddSeconds(seconds).ToLocalTime();
        }


        public static long CurrentUnixTimestamp()
        {
            return DateTime.UtcNow.ToUnixTimeSeconds();
        }

        public static long ToUnixTimeMilliseconds(this DateTime dateTime)
        {
            if (dateTime == DateTime.MinValue)
                return 0;

            return (long)(dateTime.ToUniversalTime() - UnixEpoch).TotalMilliseconds;
        }


        public static DateTime FromUnixTimeMilliseconds(long milliseconds)
        {
            if (milliseconds <= 0)
                return DateTime.MinValue;

            return UnixEpoch.AddMilliseconds(milliseconds).ToLocalTime();
        }

        public static long CurrentUnixTimestampMilliseconds()
        {
            return DateTime.UtcNow.ToUnixTimeMilliseconds();
        }
    }
}

