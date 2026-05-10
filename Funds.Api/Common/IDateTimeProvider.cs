namespace Funds.Api.Common;

public interface IDateTimeProvider
{
    DateTime UtcNow { get; }

    DateTime Today { get; }

    TimeSpan CurrentTimeOfDay { get; }
}