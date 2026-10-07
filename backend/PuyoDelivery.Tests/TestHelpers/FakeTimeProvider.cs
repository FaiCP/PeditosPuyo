namespace PuyoDelivery.Tests.TestHelpers;

/// <summary>Reloj controlable para testear timeouts del motor sin esperar en tiempo real.</summary>
public class FakeTimeProvider : TimeProvider
{
    private DateTimeOffset _now;
    public FakeTimeProvider(DateTimeOffset start) => _now = start;
    public override DateTimeOffset GetUtcNow() => _now;

    public void Advance(TimeSpan by) => _now = _now.Add(by);
    public void AdvTime(TimeSpan by) => _now = _now.Add(by);
    public void Set(DateTimeOffset to) => _now = to;
}
