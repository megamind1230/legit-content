using LegitContent.Data;
using LegitContent.Models;
using LegitContent.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace LegitContent.Pages;

public class IndexModel(
	AppDbContext db,
	LinkService linkService,
	TagService tagService,
	UserLinkStateService userLinkState,
	TimeProvider clock) : PageModel
{
		public const int PageSize = 100;

		[BindProperty(SupportsGet = true)]
		public ContentType? Type { get; set; }

		[BindProperty(SupportsGet = true)]
		public string? Tags { get; set; }

		[BindProperty(SupportsGet = true)]
		public TagMode TagMode { get; set; } = TagMode.Any;

		[BindProperty(SupportsGet = true)]
		public FeedSort Sort { get; set; } = FeedSort.MostLegit;

		[BindProperty(SupportsGet = true)]
		public string? Search { get; set; }

		[BindProperty]
		public AddLinkInput NewLink { get; set; } = new();

		public bool ShowAddDialog { get; private set; }

		public IReadOnlyList<LinkCard> Cards { get; private set; } = [];

		public string CurrentUrl => Request.Path + Request.QueryString;

		public IReadOnlyList<string> TagList { get; private set; } = [];

		public bool HasActiveFilters =>
			Type is not null || TagList.Count > 0 || !string.IsNullOrWhiteSpace(Search) || TagMode == TagMode.All;

		public async Task OnGetAsync(CancellationToken cancellationToken)
		{
				TagList = SplitTags(Tags);
				await LoadCardsAsync(cancellationToken);
		}

		public async Task<IActionResult> OnPostAddAsync(CancellationToken cancellationToken)
		{
				if (User.Identity?.IsAuthenticated != true)
						return RedirectToPage("/Account/Login", new { returnUrl = "/" });

				if (!ModelState.IsValid)
						return await RedrawWithDialogAsync(cancellationToken);

				var user = await db.Users.FirstOrDefaultAsync(u => u.Id == CurrentUserId, cancellationToken);
				if (user is null)
						return RedirectToPage("/Account/Login", new { returnUrl = "/" });

				var result = await linkService.AddLinkAsync(user, NewLink, cancellationToken);
				if (!result.Success)
				{
						ModelState.AddModelError("NewLink.Tags", result.Error ?? "Could not add the link.");
						return await RedrawWithDialogAsync(cancellationToken);
				}

				return RedirectToPage("./Index");
		}

		public async Task<IActionResult> OnPostUpvoteAsync(int linkId, string? returnUrl, CancellationToken cancellationToken)
		{
				if (User.Identity?.IsAuthenticated != true)
						return RedirectToPage("/Account/Login", new { returnUrl = returnUrl ?? "/" });

				var userId = CurrentUserId!;
				var existing = await db.Upvotes
					.FirstOrDefaultAsync(v => v.LinkId == linkId && v.UserId == userId, cancellationToken);
				if (existing is not null)
						db.Upvotes.Remove(existing);
				else
						db.Upvotes.Add(new Upvote { LinkId = linkId, UserId = userId, Created = clock.GetUtcNow().UtcDateTime });

				await db.SaveChangesAsync(cancellationToken);

				// The JS client updates the vote in place; only the no-JS fallback gets a redirect.
				if (Request.Headers.TryGetValue("X-Requested-With", out var requestedWith) && requestedWith == "fetch")
				{
						var upvotes = await db.Upvotes.CountAsync(v => v.LinkId == linkId, cancellationToken);
						var voted = await db.Upvotes.AnyAsync(v => v.LinkId == linkId && v.UserId == userId, cancellationToken);
						return new JsonResult(new { upvotes, voted });
				}

				return LocalRedirect(string.IsNullOrWhiteSpace(returnUrl) ? "/" : returnUrl);
		}

		private string? CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier);

		public async Task<IActionResult> OnPostMarkReadAsync(int linkId, string? returnUrl, CancellationToken cancellationToken)
		{
				if (User.Identity?.IsAuthenticated != true)
						return RedirectToPage("/Account/Login", new { returnUrl = returnUrl ?? "/" });

				await userLinkState.MarkReadAsync(CurrentUserId!, linkId, cancellationToken);

				return Request.Headers.TryGetValue("X-Requested-With", out var requestedWith) && requestedWith == "fetch"
						? new JsonResult(new { read = true })
						: LocalRedirect(string.IsNullOrWhiteSpace(returnUrl) ? "/" : returnUrl);
		}

		public async Task<IActionResult> OnPostArchiveAsync(int linkId, string? returnUrl, CancellationToken cancellationToken)
		{
				if (User.Identity?.IsAuthenticated != true)
						return RedirectToPage("/Account/Login", new { returnUrl = returnUrl ?? "/" });

				var link = await db.Links.AsNoTracking().FirstOrDefaultAsync(l => l.Id == linkId, cancellationToken);
				if (link is null)
						return LocalRedirect(string.IsNullOrWhiteSpace(returnUrl) ? "/" : returnUrl);

				await userLinkState.ArchiveAsync(CurrentUserId!, link, cancellationToken);

				return Request.Headers.TryGetValue("X-Requested-With", out var requestedWith) && requestedWith == "fetch"
						? new JsonResult(new { archived = true })
						: LocalRedirect(string.IsNullOrWhiteSpace(returnUrl) ? "/" : returnUrl);
		}

		private async Task<IActionResult> RedrawWithDialogAsync(CancellationToken cancellationToken)
		{
				TagList = SplitTags(Tags);
				ShowAddDialog = true;
				await LoadCardsAsync(cancellationToken);
				return Page();
		}

		private async Task LoadCardsAsync(CancellationToken cancellationToken)
		{
				var now = clock.GetUtcNow().UtcDateTime;
				var userId = CurrentUserId;
				var query = db.Links
					.AsNoTracking()
					.Include(l => l.Upvotes)
					.Include(l => l.Submitter)
					.Include(l => l.Tags)
					.ThenInclude(lt => lt.Tag)
					.AsQueryable();

				if (userId is not null)
						query = query.Where(l => !db.ReadLinks.Any(r => r.LinkId == l.Id && r.UserId == userId));

				if (Type is not null) query = query.Where(l => l.Type == Type);
				if (!string.IsNullOrWhiteSpace(Search))
				{
						var pattern = $"%{Search.Trim()}%";
						query = query.Where(l => EF.Functions.Like(l.Title, pattern) || EF.Functions.Like(l.Description, pattern));
				}

				var links = await query.ToListAsync(cancellationToken);

				if (TagList.Count > 0)
				{
						var selectedTagIds = (await tagService.FindTagsAsync(TagList, cancellationToken))
							.Tags
							.Select(t => t.Id)
							.ToHashSet();
						if (selectedTagIds.Count == 0)
						{
								Cards = [];
								return;
						}
						links = links.Where(l => TagMatcher.Matches(l, selectedTagIds, TagMode)).ToList();
				}

				var visible = links.Where(l => LinkRanker.IsVisible(l, now));
				IEnumerable<Link> ordered = Sort == FeedSort.Newest
					? visible.OrderByDescending(l => l.Created)
					: visible.OrderByDescending(l => LinkRanker.Score(l, now)).ThenByDescending(l => l.Created);

				var paths = await tagService.GetPathsAsync(cancellationToken);
				var archivedUrls = userId is null
					? null
					: (await db.ArchivedLinks
						.Where(a => a.UserId == userId)
						.Select(a => a.Url)
						.ToListAsync(cancellationToken))
						.ToHashSet();
				Cards = ordered
					.Take(PageSize)
					.Select(l => LinkCardMapper.Map(l, paths, userId, archivedUrls, now))
					.ToList();
		}

		public string RemoveTypeUrl() => BuildUrl(null, TagList, TagMode, Sort, Search);

		public string RemoveTagUrl(string tag) =>
			BuildUrl(Type, [.. TagList.Where(t => t != tag)], TagMode, Sort, Search);

		public string RemoveSearchUrl() => BuildUrl(Type, TagList, TagMode, Sort, null);

		public string ToggleTagModeUrl() => BuildUrl(Type, TagList, TagMode.Any, Sort, Search);

		public string ClearUrl() => "/";

		private static IReadOnlyList<string> SplitTags(string? tags) =>
			tags?.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
				.Distinct()
				.ToList() ?? [];

		private string BuildUrl(ContentType? type, IReadOnlyList<string> tags, TagMode mode, FeedSort sort, string? search)
		{
				var parts = new List<string>();
				if (type is not null) parts.Add($"Type={type}");
				if (tags.Count > 0) parts.Add($"Tags={Uri.EscapeDataString(string.Join(',', tags))}");
				if (mode == TagMode.All) parts.Add("TagMode=All");
				if (sort != FeedSort.MostLegit) parts.Add($"Sort={sort}");
				if (!string.IsNullOrWhiteSpace(search)) parts.Add($"Search={Uri.EscapeDataString(search)}");
				return parts.Count == 0 ? "/" : $"?{string.Join('&', parts)}";
		}
}
