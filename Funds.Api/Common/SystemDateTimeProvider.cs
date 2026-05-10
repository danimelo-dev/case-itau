namespace Funds.Api.Common;

public class SystemDateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;

    public DateTime Today => DateTime.Today;

    public TimeSpan CurrentTimeOfDay => DateTime.Now.TimeOfDay;
}