using LegitContent.Data;
using LegitContent.Models;
using Microsoft.EntityFrameworkCore;

namespace LegitContent.Services;

public class LinkService(AppDbContext db, TagService tagService, TimeProvider clock)
{
		public const int MaxLinksPerUserPerDay = 5;

		public async Task<LinkAddResult> AddLinkAsync(User submitter, AddLinkInput input, CancellationToken cancellationToken = default)
		{
				var now = clock.GetUtcNow().UtcDateTime;
				if (!Uri.TryCreate(input.Url.Trim(), UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https"))
						return LinkAddResult.Fail("Enter a valid http(s) URL.");

				var tagPaths = input.Tags.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
				var tags = await tagService.ResolveTagsAsync(tagPaths, cancellationToken);
				if (!tags.Success)
						return LinkAddResult.Fail(tags.Error!);

				var normalizedUrl = NormalizeUrl(input.Url);
				var recentCount = await db.Links
					.CountAsync(l => l.SubmitterId == submitter.Id && l.Created > now.AddDays(-1), cancellationToken);
				if (recentCount >= MaxLinksPerUserPerDay)
						return LinkAddResult.Fail($"You already added {MaxLinksPerUserPerDay} links in the last 24 hours. Come back tomorrow.");

				var duplicateExists = await db.Links.AnyAsync(l => l.NormalizedUrl == normalizedUrl, cancellationToken);
				if (duplicateExists)
						return LinkAddResult.Fail("That link is already on the site.");

				db.Links.Add(new Link
				{
						Url = uri.ToString(),
						NormalizedUrl = normalizedUrl,
						Title = input.Title.Trim(),
						Description = input.Description.Trim(),
						Type = input.Type!.Value,
						SubmitterId = submitter.Id,
						Created = now,
						Tags = tags.Tags.Select(t => new LinkTag { TagId = t.Id }).ToList(),
				});
				await db.SaveChangesAsync(cancellationToken);
				return LinkAddResult.Ok();
		}

		public static string NormalizeUrl(string url)
		{
				if (!Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri)) return url.Trim();
				var scheme = uri.Scheme.ToLowerInvariant();
				var host = uri.Host.ToLowerInvariant();
				var defaultPort = (scheme, uri.Port) is ("http", 80) or ("https", 443);
				var port = defaultPort ? "" : $":{uri.Port}";
				var path = uri.AbsolutePath.TrimEnd('/');
				// Query strings must be kept: different YouTube videos share one path but differ in ?v=
				var query = uri.Query;
				return $"{scheme}://{host}{port}{path}{query}";
		}
}
