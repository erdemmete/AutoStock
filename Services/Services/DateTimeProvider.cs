using AutoStock.Services.Interfaces;

namespace AutoStock.Services.Services
{
    public class DateTimeProvider : IDateTimeProvider
    {
        private static readonly TimeZoneInfo TurkeyTimeZone = GetTurkeyTimeZone();

        public DateTime Now =>
            TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TurkeyTimeZone);

        public DateTime Today => Now.Date;

        public DateTime UtcNow => DateTime.UtcNow;

        public DateTime TodayStartUtc =>
            TimeZoneInfo.ConvertTimeToUtc(Today, TurkeyTimeZone);

        public DateTime TomorrowStartUtc =>
            TodayStartUtc.AddDays(1);

        private static TimeZoneInfo GetTurkeyTimeZone()
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById("Turkey Standard Time");
            }
            catch
            {
                try
                {
                    return TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul");
                }
                catch
                {
                    return TimeZoneInfo.CreateCustomTimeZone(
                        "TurkeyFallback",
                        TimeSpan.FromHours(3),
                        "Turkey Fallback",
                        "Turkey Fallback");
                }
            }
        }
    }
}