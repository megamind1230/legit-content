namespace LegitContent.Services;

public static class AgeFormatter
{
		public static string Format(DateTime created, DateTime now)
		{
				var age = now - created;
				if (age.TotalMinutes < 1) return "just now";
				if (age.TotalHours < 1) return $"{(int)age.TotalMinutes}m ago";
				if (age.TotalDays < 1) return $"{(int)age.TotalHours}h ago";
				if (age.TotalDays < 30) return $"{(int)age.TotalDays}d ago";
				if (age.TotalDays < 365) return $"{(int)(age.TotalDays / 30)}mo ago";
				return $"{(int)(age.TotalDays / 365)}y ago";
		}
}
