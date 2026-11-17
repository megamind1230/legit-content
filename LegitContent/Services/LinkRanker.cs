using LegitContent.Models;

namespace LegitContent.Services;

public static class LinkRanker
{
		public const int FreshWindowDays = 3;
		public const double FreshBonus = 3;
		public const int HideAfterDays = 30;
		public const int MinimumUpvotesToStay = 1;

		public static double Score(Link link, DateTime now)
		{
				var ageDays = (now - link.Created).TotalDays;
				return link.Upvotes.Count + Math.Clamp(FreshBonus - ageDays, 0, FreshBonus);
		}

		public static bool IsVisible(Link link, DateTime now)
		{
				if ((now - link.Created).TotalDays < HideAfterDays) return true;
				return link.Upvotes.Count >= MinimumUpvotesToStay;
		}
}
