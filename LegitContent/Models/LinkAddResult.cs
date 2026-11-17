namespace LegitContent.Models;

public sealed record LinkAddResult(bool Success, string? Error)
{
		public static LinkAddResult Ok() => new(true, null);

		public static LinkAddResult Fail(string error) => new(false, error);
}
