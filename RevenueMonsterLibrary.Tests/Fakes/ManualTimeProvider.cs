namespace RevenueMonsterLibrary.Tests.Fakes;

/// <summary>
///     A clock that only moves when the test moves it.
/// </summary>
internal sealed class ManualTimeProvider(DateTimeOffset now) : TimeProvider
{
    public DateTimeOffset Now { get; private set; } = now;

    public override DateTimeOffset GetUtcNow()
    {
        return Now;
    }

    public void Advance(TimeSpan duration)
    {
        Now += duration;
    }
}
