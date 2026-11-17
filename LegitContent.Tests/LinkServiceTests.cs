using LegitContent.Data;
using LegitContent.Models;
using LegitContent.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace LegitContent.Tests;

public class LinkServiceTests
{
		private static AppDbContext CreateDb()
		{
				var connection = new SqliteConnection("DataSource=:memory:");
				connection.Open();
				var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
				var db = new AppDbContext(options);
				db.Database.EnsureCreated();
				return db;
		}

		private static LinkService CreateService(AppDbContext db) => new(db, new TagService(db), TestClock.At);

		private static User CreateUser(AppDbContext db, string email = "tester@example.com")
		{
				var user = new User { Email = email };
				db.Users.Add(user);
				db.SaveChanges();
				return user;
		}

		private static AddLinkInput Input(string url) => new()
		{
				Url = url,
				Title = "Title",
				Description = "Description",
				Type = ContentType.Article,
				Tags = "science",
		};

		[Fact]
		public async Task AddLinkAsync_ValidNewLink_PersistsLink()
		{
				using var db = CreateDb();
				var service = CreateService(db);
				var user = CreateUser(db);

				var result = await service.AddLinkAsync(user, Input("https://example.com/article/"));

				Assert.True(result.Success);
				var link = Assert.Single(db.Links);
				Assert.Equal("https://example.com/article", link.NormalizedUrl);
				Assert.Equal(user.Id, link.SubmitterId);
		}

		[Fact]
		public async Task AddLinkAsync_DuplicateUrl_IgnoringCaseAndTrailingSlash_IsRejected()
		{
				using var db = CreateDb();
				var service = CreateService(db);
				var user = CreateUser(db);

				await service.AddLinkAsync(user, Input("https://example.com/article"));
				var result = await service.AddLinkAsync(user, Input("https://EXAMPLE.com/article/"));

				Assert.False(result.Success);
				Assert.Contains("already", result.Error);
		}

		[Fact]
		public async Task AddLinkAsync_OverDailyLimit_IsRejected()
		{
				using var db = CreateDb();
				var service = CreateService(db);
				var user = CreateUser(db);

				for (var i = 0; i < LinkService.MaxLinksPerUserPerDay; i++)
				{
						var result = await service.AddLinkAsync(user, Input($"https://example.com/{i}"));
						Assert.True(result.Success);
				}

				var rejected = await service.AddLinkAsync(user, Input("https://example.com/extra"));

				Assert.False(rejected.Success);
				Assert.Contains("24 hours", rejected.Error);
		}

		[Fact]
		public async Task AddLinkAsync_SamePathDifferentQuery_BothPersist()
		{
				using var db = CreateDb();
				var service = CreateService(db);
				var user = CreateUser(db);

				await service.AddLinkAsync(user, Input("https://www.youtube.com/watch?v=a"));
				var result = await service.AddLinkAsync(user, Input("https://www.youtube.com/watch?v=b"));

				Assert.True(result.Success);
				Assert.Equal(2, db.Links.Count());
		}

		[Fact]
		public async Task AddLinkAsync_CreatesTagsFromPaths()
		{
				using var db = CreateDb();
				var service = CreateService(db);
				var user = CreateUser(db);
				var input = Input("https://example.com/tagged");
				input.Tags = "medicine/anatomy, history";

				var result = await service.AddLinkAsync(user, input);

				Assert.True(result.Success);
				var link = Assert.Single(db.Links);
				Assert.Equal(2, link.Tags.Count);
				Assert.Contains(db.Tags, t => t.Name == "medicine");
				Assert.Contains(db.Tags, t => t.Name == "anatomy");
		}

		[Fact]
		public async Task AddLinkAsync_InvalidTagPath_IsRejected()
		{
				using var db = CreateDb();
				var service = CreateService(db);
				var user = CreateUser(db);
				var input = Input("https://example.com/tagged");
				input.Tags = "bad/tag!";

				var result = await service.AddLinkAsync(user, input);

				Assert.False(result.Success);
				Assert.Empty(db.Links);
		}

		[Fact]
		public async Task AddLinkAsync_InvalidUrl_IsRejected()
		{
				using var db = CreateDb();
				var service = CreateService(db);
				var user = CreateUser(db);

				var result = await service.AddLinkAsync(user, Input("not a url"));

				Assert.False(result.Success);
				Assert.Empty(db.Links);
		}
}
