using LegitContent.Data;
using LegitContent.Models;
using Microsoft.EntityFrameworkCore;

namespace LegitContent.Services;

public class UserLinkStateService(AppDbContext db, TimeProvider clock)
{
		public async Task MarkReadAsync(string userId, int linkId, CancellationToken cancellationToken = default)
		{
				var exists = await db.ReadLinks
					.AnyAsync(r => r.UserId == userId && r.LinkId == linkId, cancellationToken);
				if (exists) return;

				db.ReadLinks.Add(new ReadLink
				{
						UserId = userId,
						LinkId = linkId,
						Created = clock.GetUtcNow().UtcDateTime,
				});
				await db.SaveChangesAsync(cancellationToken);
		}

		public async Task UnmarkReadAsync(string userId, int linkId, CancellationToken cancellationToken = default)
		{
				var read = await db.ReadLinks
					.FirstOrDefaultAsync(r => r.UserId == userId && r.LinkId == linkId, cancellationToken);
				if (read is null) return;

				db.ReadLinks.Remove(read);
				await db.SaveChangesAsync(cancellationToken);
		}

		public async Task ArchiveAsync(string userId, Link link, CancellationToken cancellationToken = default)
		{
				var exists = await db.ArchivedLinks
					.AnyAsync(a => a.UserId == userId && a.Url == link.Url, cancellationToken);
				if (exists) return;

				db.ArchivedLinks.Add(new ArchivedLink
				{
						UserId = userId,
						Title = link.Title,
						Url = link.Url,
						Created = clock.GetUtcNow().UtcDateTime,
				});
				await db.SaveChangesAsync(cancellationToken);
		}

		public async Task DeleteArchivedAsync(string userId, int archivedId, CancellationToken cancellationToken = default)
		{
				var archived = await db.ArchivedLinks
					.FirstOrDefaultAsync(a => a.Id == archivedId && a.UserId == userId, cancellationToken);
				if (archived is null) return;

				db.ArchivedLinks.Remove(archived);
				await db.SaveChangesAsync(cancellationToken);
		}
}
