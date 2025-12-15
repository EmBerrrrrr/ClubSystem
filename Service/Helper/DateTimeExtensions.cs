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

            var unspecified = DateTime.SpecifyKind(dt.Value, DateTimeKind.Unspecified);
            return TimeZoneInfo.ConvertTime(unspecified, VietnamTimeZone);
        }

        public static DateTime NowVietnam()
        {
            var utcNow = DateTime.UtcNow;
            return TimeZoneInfo.ConvertTimeFromUtc(utcNow, VietnamTimeZone);
        }
    }
}
