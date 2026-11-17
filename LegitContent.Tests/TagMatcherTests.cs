using LegitContent.Models;
using LegitContent.Services;

namespace LegitContent.Tests;

public class TagMatcherTests
{
		private static Link LinkWithTags(params int[] tagIds) =>
			new() { Tags = tagIds.Select(id => new LinkTag { TagId = id }).ToList() };

		[Fact]
		public void Matches_AnyMode_AnyOverlapIsEnough()
		{
				var link = LinkWithTags(1, 2);
				Assert.True(TagMatcher.Matches(link, new HashSet<int> { 2, 3 }, TagMode.Any));
		}

		[Fact]
		public void Matches_AnyMode_NoOverlapFails()
		{
				var link = LinkWithTags(1, 2);
				Assert.False(TagMatcher.Matches(link, new HashSet<int> { 3 }, TagMode.Any));
		}

		[Fact]
		public void Matches_AllMode_RequiresEveryTag()
		{
				var link = LinkWithTags(1, 2);
				Assert.True(TagMatcher.Matches(link, new HashSet<int> { 1, 2 }, TagMode.All));
				Assert.False(TagMatcher.Matches(link, new HashSet<int> { 1, 3 }, TagMode.All));
		}

		[Fact]
		public void Matches_AllMode_WithExtraLinkTags_StillMatches()
		{
				var link = LinkWithTags(1, 2, 3);
				Assert.True(TagMatcher.Matches(link, new HashSet<int> { 1, 3 }, TagMode.All));
		}
}
