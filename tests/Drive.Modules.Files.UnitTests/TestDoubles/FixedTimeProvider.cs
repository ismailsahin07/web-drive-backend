namespace Drive.Modules.Files.UnitTests.TestDoubles
{
    internal sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
