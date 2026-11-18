using LegitContent.Models;

namespace LegitContent.Services;

public static class LinkCardMapper
{
		public static LinkCard Map(
			Link link,
			IReadOnlyDictionary<int, string> tagPaths,
			string? userId,
			ISet<string>? archivedUrls,
			DateTime now) =>
				new(
					link.Id,
					link.Title,
					link.Url,
					Domain(link.Url),
					link.Description,
					link.Type,
					link.Tags
						.Select(lt => tagPaths.GetValueOrDefault(lt.TagId, lt.Tag?.Name ?? ""))
						.Where(path => path.Length > 0)
						.ToList(),
					link.Upvotes.Count,
					userId is not null && link.Upvotes.Any(v => v.UserId == userId),
					link.Submitter?.Email.Split('@')[0] ?? "unknown",
					AgeFormatter.Format(link.Created, now),
					archivedUrls?.Contains(link.Url) == true);

		private static string Domain(string url) =>
			Uri.TryCreate(url, UriKind.Absolute, out var uri) ? uri.Host : url;
}
