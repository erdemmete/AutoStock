namespace AutoStock.Services.Interfaces
{
    public interface IDateTimeProvider
    {
        DateTime Now { get; }

        DateTime Today { get; }

        DateTime UtcNow { get; }

        DateTime TodayStartUtc { get; }

        DateTime TomorrowStartUtc { get; }
    }
}