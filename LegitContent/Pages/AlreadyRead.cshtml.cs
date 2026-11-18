using LegitContent.Data;
using LegitContent.Models;
using LegitContent.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace LegitContent.Pages;

public class AlreadyReadModel(
	AppDbContext db,
	TagService tagService,
	UserLinkStateService userLinkState,
	TimeProvider clock) : PageModel
{
		public IReadOnlyList<LinkCard> Cards { get; private set; } = [];

		public string CurrentUrl => Request.Path + Request.QueryString;

		public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
		{
				var userId = CurrentUserId;
				if (userId is null)
						return RedirectToPage("/Account/Login", new { returnUrl = CurrentUrl });

				var now = clock.GetUtcNow().UtcDateTime;
				var reads = await db.ReadLinks
					.AsNoTracking()
					.Where(r => r.UserId == userId)
					.Include(r => r.Link)
						.ThenInclude(l => l!.Upvotes)
					.Include(r => r.Link)
						.ThenInclude(l => l!.Submitter)
					.Include(r => r.Link)
						.ThenInclude(l => l!.Tags)
							.ThenInclude(lt => lt.Tag)
					.OrderByDescending(r => r.Created)
					.ToListAsync(cancellationToken);

				var paths = await tagService.GetPathsAsync(cancellationToken);
				Cards = reads
					.Where(r => r.Link is not null)
					.Select(r => LinkCardMapper.Map(r.Link!, paths, userId, null, now))
					.ToList();
				ViewData["CardMode"] = "read";
				return Page();
		}

		public async Task<IActionResult> OnPostUnmarkReadAsync(int linkId, string? returnUrl, CancellationToken cancellationToken)
		{
				var userId = CurrentUserId;
				if (userId is null)
						return RedirectToPage("/Account/Login", new { returnUrl = returnUrl ?? CurrentUrl });

				await userLinkState.UnmarkReadAsync(userId, linkId, cancellationToken);

				return Request.Headers.TryGetValue("X-Requested-With", out var requestedWith) && requestedWith == "fetch"
						? new JsonResult(new { read = false })
						: LocalRedirect(string.IsNullOrWhiteSpace(returnUrl) ? "/AlreadyRead" : returnUrl);
		}

		private string? CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier);
}
