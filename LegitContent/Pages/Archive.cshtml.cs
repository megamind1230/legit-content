using LegitContent.Data;
using LegitContent.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace LegitContent.Pages;

public class ArchiveModel(AppDbContext db, UserLinkStateService userLinkState, TimeProvider clock) : PageModel
{
		public IReadOnlyList<ArchivedCard> Items { get; private set; } = [];

		public string CurrentUrl => Request.Path + Request.QueryString;

		public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
		{
				var userId = CurrentUserId;
				if (userId is null)
						return RedirectToPage("/Account/Login", new { returnUrl = CurrentUrl });

				var now = clock.GetUtcNow().UtcDateTime;
				var items = await db.ArchivedLinks
					.AsNoTracking()
					.Where(a => a.UserId == userId)
					.OrderByDescending(a => a.Created)
					.ToListAsync(cancellationToken);

				Items = items
					.Select(a => new ArchivedCard(a.Id, a.Title, a.Url, AgeFormatter.Format(a.Created, now)))
					.ToList();
				return Page();
		}

		public async Task<IActionResult> OnPostDeleteAsync(int archivedId, string? returnUrl, CancellationToken cancellationToken)
		{
				var userId = CurrentUserId;
				if (userId is null)
						return RedirectToPage("/Account/Login", new { returnUrl = returnUrl ?? CurrentUrl });

				await userLinkState.DeleteArchivedAsync(userId, archivedId, cancellationToken);

				return Request.Headers.TryGetValue("X-Requested-With", out var requestedWith) && requestedWith == "fetch"
						? new JsonResult(new { deleted = true })
						: LocalRedirect(string.IsNullOrWhiteSpace(returnUrl) ? "/Archive" : returnUrl);
		}

		private string? CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier);
}

public sealed record ArchivedCard(int Id, string Title, string Url, string Saved);
