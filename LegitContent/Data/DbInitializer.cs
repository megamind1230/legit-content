using LegitContent.Models;
using LegitContent.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace LegitContent.Data;

public static class DbInitializer
{
		private static readonly SeedLink[] Seeds =
		[
			new(200, "Stanford Encyclopedia of Philosophy", "https://plato.stanford.edu/",
			"Peer-reviewed philosophy entries maintained by Stanford. Deep, rigorous, and free.", ContentType.Article, "philosophy", 5),
		new(150, "Khan Academy", "https://www.khanacademy.org/",
			"Free courses in math, science, and more, from counting to calculus.", ContentType.Course, "math", 4),
		new(120, "The Feynman Lectures on Physics", "https://www.feynmanlectures.caltech.edu/",
			"Caltech's legendary physics lectures, complete and free online.", ContentType.Course, "science,science/physics", 5),
		new(110, "Structure and Interpretation of Computer Programs", "https://sarabander.github.io/sicp/",
			"The classic MIT text on programming fundamentals, beautiful free HTML edition.", ContentType.Book, "coding", 4),
		new(100, "Crash Course World History", "https://www.youtube.com/playlist?list=PLBDA2E52FB1EF80C9",
			"John Green's 42-episode whirlwind tour of human history.", ContentType.Video, "history", 4),
		new(90, "3Blue1Brown", "https://www.youtube.com/@3blue1brown",
			"Animated math that builds real intuition, from linear algebra to neural networks.", ContentType.Video, "math,math/algebra", 5),
		new(80, "You and Your Research", "https://www.cs.virginia.edu/~robins/YouAndYourResearch.html",
			"Richard Hamming's classic talk on doing great work. Worth rereading once a year.", ContentType.Article, "science", 4),
		new(75, "Veritasium", "https://www.youtube.com/@veritasium",
			"Science videos with actual depth, built around the counterintuitive truth of things.", ContentType.Video, "science,science/physics", 4),
		new(70, "Paul Graham's Essays", "https://www.paulgraham.com/articles.html",
			"Clear thinking on startups, work, and ideas, in famously readable prose.", ContentType.Article, "other", 3),
		new(65, "Martin Fowler's bliki", "https://martinfowler.com/",
			"Decades of writing on software architecture, refactoring, and design.", ContentType.Article, "coding,coding/web", 3),
		new(60, "Kurzgesagt", "https://www.youtube.com/@kurzgesagt",
			"Beautifully researched animated explainers on science, space, and existential questions.", ContentType.Video, "science,science/physics", 3),
		new(55, "CGP Grey", "https://www.youtube.com/@cgpgrey",
			"Short, dense explainers on history, geography, and how systems actually work.", ContentType.Video, "history", 2),
		new(50, "Radiolab", "https://radiolab.org/",
			"Long-form audio storytelling about science, philosophy, and the human experience.", ContentType.Podcast, "science", 3),
		new(45, "Huberman Lab", "https://hubermanlab.com/",
			"Science-based health protocols from Stanford neurobiologist Andrew Huberman.", ContentType.Podcast, "health", 2),
		new(40, "Atlas Obscura", "https://www.atlasobscura.com/",
			"A curated atlas of the world's wonders, the definitive guide to curious places.", ContentType.Tool, "history", 2),
		new(35, "NASA Image and Video Library", "https://images.nasa.gov/",
			"Public-domain images and footage from NASA missions, searchable in one place.", ContentType.Tool, "science", 2),
		new(30, "A Programmer's Introduction to Mathematics", "https://pimbook.org/",
			"Free book that teaches math through the eyes of a programmer.", ContentType.Book, "math", 1),
		new(25, "The Bitter Lesson", "http://www.incompleteideas.net/IncIdeas/BitterLesson.html",
			"Rich Sutton's famous short essay on why general methods win in AI.", ContentType.Article, "science,science/ai", 2),
		new(20, "Harvard CS50", "https://cs50.harvard.edu/x/",
			"Harvard's legendary introduction to computer science, free for anyone.", ContentType.Course, "coding,coding/web", 4),
	];

		public static async Task SeedAsync(AppDbContext db, string? adminEmail, string? adminPassword, CancellationToken cancellationToken = default)
		{
				if (!await db.Links.AnyAsync(cancellationToken))
				{
						SeedContent(db, DateTime.UtcNow);
						await db.SaveChangesAsync(cancellationToken);
				}
				await EnsureAdminAsync(db, adminEmail, adminPassword, cancellationToken);
		}

		private static void SeedContent(AppDbContext db, DateTime now)
		{
				var hasher = new PasswordHasher<User>();
				var users = new[]
				{
			"curator@legitcontent.local",
			"ada@legitcontent.local",
			"grace@legitcontent.local",
			"alan@legitcontent.local",
			"katherine@legitcontent.local",
		}.Select(email =>
		{
				var user = new User { Email = email, Created = now.AddYears(-1) };
				user.PasswordHash = hasher.HashPassword(user, Guid.NewGuid().ToString());
				return user;
		}).ToList();
				db.Users.AddRange(users);

				var tagsByPath = new Dictionary<string, Tag>();
				Tag TagFor(string path)
				{
						if (tagsByPath.TryGetValue(path, out var existing)) return existing;
						Tag? current = null;
						foreach (var segment in path.Split('/'))
						{
								var candidate = db.Tags.Local.FirstOrDefault(t => t.Parent == current && t.Name == segment);
								if (candidate is null)
								{
										candidate = new Tag { Name = segment, Parent = current };
										db.Tags.Add(candidate);
								}
								current = candidate;
						}
						tagsByPath[path] = current!;
						return current!;
				}

				foreach (var seed in Seeds)
				{
						var link = new Link
						{
								Title = seed.Title,
								Url = seed.Url,
								NormalizedUrl = LinkService.NormalizeUrl(seed.Url),
								Description = seed.Description,
								Type = seed.Type,
								Submitter = users[0],
								Created = now.AddDays(-seed.DaysAgo),
								Tags = seed.Tags.Split(',').Select(path => new LinkTag { Tag = TagFor(path.Trim()) }).ToList(),
						};
						db.Links.Add(link);
						foreach (var voter in users.Take(seed.Votes))
								db.Upvotes.Add(new Upvote { Link = link, User = voter, Created = link.Created });
				}
		}

		private static async Task EnsureAdminAsync(AppDbContext db, string? adminEmail, string? adminPassword, CancellationToken cancellationToken)
		{
				if (string.IsNullOrWhiteSpace(adminEmail) || string.IsNullOrWhiteSpace(adminPassword)) return;

				var email = adminEmail.Trim().ToLowerInvariant();
				var hasher = new PasswordHasher<User>();
				var admin = await db.Users.FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
				if (admin is null)
				{
						var user = new User { Email = email, IsAdmin = true, Created = DateTime.UtcNow };
						user.PasswordHash = hasher.HashPassword(user, adminPassword);
						db.Users.Add(user);
						await db.SaveChangesAsync(cancellationToken);
						return;
				}

				if (!admin.IsAdmin)
				{
						admin.IsAdmin = true;
						await db.SaveChangesAsync(cancellationToken);
				}
		}

		private sealed record SeedLink(int DaysAgo, string Title, string Url, string Description, ContentType Type, string Tags, int Votes);
}
