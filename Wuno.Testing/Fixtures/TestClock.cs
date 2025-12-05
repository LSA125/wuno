namespace Wuno.Testing.Fixtures
{
    public sealed class TestClock
    {
        public DateTime UtcNow { get; private set; }

        public TestClock(DateTime? now = null)
        {
            UtcNow = now ?? DateTime.UtcNow;
        }

        public DateTimeOffset UtcNowOffset => new(UtcNow);

        public TestClock Advance(TimeSpan by)
        {
            UtcNow = UtcNow.Add(by);
            return this;
        }

        public TestClock Set(DateTime value)
        {
            UtcNow = value;
            return this;
        }
    }
}
