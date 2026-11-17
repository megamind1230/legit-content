using LegitContent.Models;
using LegitContent.Services;

namespace LegitContent.Tests;

public class LinkRankerTests
{
		private static readonly DateTime Now = new(2026, 11, 17, 12, 0, 0, DateTimeKind.Utc);

		private static Link Link(int upvotes, double ageDays)
		{
				var link = new Link { Created = Now.AddDays(-ageDays) };
				for (var i = 0; i < upvotes; i++) link.Upvotes.Add(new Upvote());
				return link;
		}

		[Fact]
		public void Score_NewLinkWithoutVotes_EqualsFreshBonus()
		{
				Assert.Equal(2, LinkRanker.Score(Link(0, 1), Now));
		}

		[Fact]
		public void Score_AgedLinkWithoutVotes_IsZero()
		{
				Assert.Equal(0, LinkRanker.Score(Link(0, 5), Now));
		}

		[Fact]
		public void Score_UpvotesOutrankFreshness()
		{
				Assert.True(LinkRanker.Score(Link(5, 10), Now) > LinkRanker.Score(Link(0, 0.5), Now));
		}

		[Fact]
		public void Score_FutureLink_ClampsFreshBonus()
		{
				Assert.Equal(LinkRanker.FreshBonus, LinkRanker.Score(Link(0, -10), Now));
		}

		[Fact]
		public void IsVisible_YoungLinkWithoutUpvotes_IsTrue()
		{
				Assert.True(LinkRanker.IsVisible(Link(0, 10), Now));
		}

		[Fact]
		public void IsVisible_AgedLinkWithUpvote_IsTrue()
		{
				Assert.True(LinkRanker.IsVisible(Link(1, 90), Now));
		}

		[Fact]
		public void IsVisible_AgedLinkWithoutUpvotes_IsFalse()
		{
				Assert.False(LinkRanker.IsVisible(Link(0, 31), Now));
		}

		[Fact]
		public void IsVisible_AtExactlyThirtyDaysWithoutUpvotes_IsFalse()
		{
				Assert.False(LinkRanker.IsVisible(Link(0, LinkRanker.HideAfterDays), Now));
		}
}
