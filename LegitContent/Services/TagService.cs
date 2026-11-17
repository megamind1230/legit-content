using System.Text;
using LegitContent.Data;
using LegitContent.Models;
using Microsoft.EntityFrameworkCore;

namespace LegitContent.Services;

public class TagService(AppDbContext db)
{
		public const int MaxTagsPerLink = 5;
		public const int MaxTagDepth = 4;
		public const int MaxSegmentLength = 30;
		public const int SearchResultsLimit = 10;

		public Task<TagResolution> ResolveTagsAsync(IEnumerable<string> paths, CancellationToken cancellationToken = default) =>
			ResolveCoreAsync(paths, createMissing: true, cancellationToken);

		public Task<TagResolution> FindTagsAsync(IEnumerable<string> paths, CancellationToken cancellationToken = default) =>
			ResolveCoreAsync(paths, createMissing: false, cancellationToken);

		public async Task<IReadOnlyList<TagSearchItem>> SearchAsync(string query, CancellationToken cancellationToken = default)
		{
				query = query.Trim().ToLowerInvariant();
				if (query.Length == 0) return [];

				var tags = await db.Tags.AsNoTracking().ToListAsync(cancellationToken);
				var byId = tags.ToDictionary(t => t.Id);
				var linkCounts = await db.LinkTags
					.GroupBy(lt => lt.TagId)
					.Select(g => new { TagId = g.Key, Count = g.Count() })
					.ToDictionaryAsync(g => g.TagId, g => g.Count, cancellationToken);

				return tags
					.Select(t => new TagSearchItem(FullPath(t, byId), linkCounts.GetValueOrDefault(t.Id)))
					.Where(item => item.Path.Contains(query, StringComparison.Ordinal))
					.OrderByDescending(item => item.LinkCount)
					.ThenBy(item => item.Path)
					.Take(SearchResultsLimit)
					.ToList();
		}

		public async Task<IReadOnlyDictionary<int, string>> GetPathsAsync(CancellationToken cancellationToken = default)
		{
				var tags = await db.Tags.AsNoTracking().ToListAsync(cancellationToken);
				var byId = tags.ToDictionary(t => t.Id);
				return tags.ToDictionary(t => t.Id, t => FullPath(t, byId));
		}

		private async Task<TagResolution> ResolveCoreAsync(IEnumerable<string> paths, bool createMissing, CancellationToken cancellationToken)
		{
				var normalizedPaths = new List<string>();
				foreach (var raw in paths)
				{
						var path = NormalizePath(raw);
						if (path.Length == 0 || normalizedPaths.Contains(path)) continue;
						if (path.Split('/').Length > MaxTagDepth)
								return TagResolution.Fail($"Tags can be at most {MaxTagDepth} levels deep ('{path}').");
						if (!IsValidPath(path))
								return TagResolution.Fail($"Tag '{path}' may only contain letters, numbers, hyphens, and slashes.");
						normalizedPaths.Add(path);
				}

				if (normalizedPaths.Count > MaxTagsPerLink)
						return TagResolution.Fail($"Use at most {MaxTagsPerLink} tags.");

				var tags = await db.Tags.ToListAsync(cancellationToken);
				var index = tags.GroupBy(t => (Parent: t.Parent, t.Name)).ToDictionary(g => g.Key, g => g.First());

				var leaves = new List<Tag>();
				foreach (var path in normalizedPaths)
				{
						Tag? current = null;
						foreach (var segment in path.Split('/'))
						{
								if (!index.TryGetValue((current, segment), out var tag))
								{
										if (!createMissing)
												return TagResolution.Fail($"Unknown tag '{path}'.");
										tag = new Tag { Name = segment, Parent = current };
										db.Tags.Add(tag);
										index[(current, segment)] = tag;
								}
								current = tag;
						}
						if (current is not null) leaves.Add(current);
				}

				if (createMissing)
						await db.SaveChangesAsync(cancellationToken);

				return TagResolution.Ok(leaves);
		}

		private static string NormalizePath(string raw)
		{
				var segments = raw
					.Split('/', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
					.Select(segment => segment.ToLowerInvariant().Replace(' ', '-'));
				return string.Join('/', segments);
		}

		private static bool IsValidPath(string path) =>
			path.Split('/').All(segment =>
				segment.Length is > 0 and <= MaxSegmentLength &&
				segment.All(c => char.IsAsciiLetterOrDigit(c) || c == '-'));

		private static string FullPath(Tag tag, IReadOnlyDictionary<int, Tag> byId)
		{
				var builder = new StringBuilder(tag.Name);
				var current = tag;
				while (current.ParentId is { } parentId && byId.TryGetValue(parentId, out var parent))
				{
						builder.Insert(0, '/');
						builder.Insert(0, parent.Name);
						current = parent;
				}
				return builder.ToString();
		}
}
