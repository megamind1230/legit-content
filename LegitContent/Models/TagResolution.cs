namespace LegitContent.Models;

public sealed record TagResolution(IReadOnlyList<Tag> Tags, string? Error)
{
		public bool Success => Error is null;

		public static TagResolution Ok(IReadOnlyList<Tag> tags) => new(tags, null);

		public static TagResolution Fail(string error) => new([], error);
}
