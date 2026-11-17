using LegitContent.Models;

namespace LegitContent.Services;

public static class TagMatcher
{
		public static bool Matches(Link link, IReadOnlySet<int> tagIds, TagMode mode)
		{
				var linkTagIds = link.Tags.Select(t => t.TagId).ToList();
				return mode == TagMode.All
					? tagIds.IsSubsetOf(linkTagIds)
					: tagIds.Overlaps(linkTagIds);
		}
}
