using System;

namespace Service.Helper
{
    public static class DateTimeExtensions
    {
        private static readonly TimeZoneInfo VietnamTimeZone =
            TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");

        public static DateTime? ToVietnamTime(this DateTime? dt)
        {
            if (!dt.HasValue) return null;

            if (dt.Value.Kind == DateTimeKind.Utc)
            {
                return TimeZoneInfo.ConvertTimeFromUtc(dt.Value, VietnamTimeZone);
            }

            return TimeZoneInfo.ConvertTime(dt.Value, VietnamTimeZone);
        }


        public static DateTime NowVietnam()
        {
            var utcNow = DateTime.UtcNow;
            return TimeZoneInfo.ConvertTimeFromUtc(utcNow, VietnamTimeZone);
        }
    }
}
