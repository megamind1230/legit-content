using LegitContent.Services;

namespace LegitContent.Tests;

internal sealed class TestClock(DateTimeOffset utcNow) : TimeProvider
{
		public static readonly TestClock At = new(new DateTimeOffset(2026, 11, 17, 12, 0, 0, TimeSpan.Zero));

		public override DateTimeOffset GetUtcNow() => utcNow;
}
