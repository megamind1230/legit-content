using LegitContent.Data;
using LegitContent.Models;
using LegitContent.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace LegitContent.Tests;

public class UserLinkStateServiceTests
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

		private static UserLinkStateService CreateService(AppDbContext db) => new(db, TestClock.At);

		private static User CreateUser(AppDbContext db, string email = "tester@example.com")
		{
				var user = new User { Email = email };
				db.Users.Add(user);
				db.SaveChanges();
				return user;
		}

		private static Link CreateLink(AppDbContext db, User user, string url = "https://example.com/article")
		{
				var link = new Link
				{
						Url = url,
						NormalizedUrl = url,
						Title = "Title",
						Description = "Description",
						Type = ContentType.Article,
						SubmitterId = user.Id,
						Created = TestClock.At.GetUtcNow().UtcDateTime,
				};
				db.Links.Add(link);
				db.SaveChanges();
				return link;
		}

		[Fact]
		public async Task MarkReadAsync_CalledTwice_StoresSingleRow()
		{
				using var db = CreateDb();
				var service = CreateService(db);
				var user = CreateUser(db);
				var link = CreateLink(db, user);

				await service.MarkReadAsync(user.Id, link.Id);
				await service.MarkReadAsync(user.Id, link.Id);

				Assert.Single(db.ReadLinks);
		}

		[Fact]
		public async Task UnmarkReadAsync_RemovesRow()
		{
				using var db = CreateDb();
				var service = CreateService(db);
				var user = CreateUser(db);
				var link = CreateLink(db, user);
				await service.MarkReadAsync(user.Id, link.Id);

				await service.UnmarkReadAsync(user.Id, link.Id);

				Assert.Empty(db.ReadLinks);
		}

		[Fact]
		public async Task ArchiveAsync_SnapshotSurvivesLinkDeletion()
		{
				using var db = CreateDb();
				var service = CreateService(db);
				var user = CreateUser(db);
				var link = CreateLink(db, user);

				await service.ArchiveAsync(user.Id, link);
				db.Links.Remove(link);
				db.SaveChanges();

				var archived = Assert.Single(db.ArchivedLinks);
				Assert.Equal("Title", archived.Title);
				Assert.Equal("https://example.com/article", archived.Url);
		}

		[Fact]
		public async Task ArchiveAsync_DuplicateUrl_IsIdempotent()
		{
				using var db = CreateDb();
				var service = CreateService(db);
				var user = CreateUser(db);
				var link = CreateLink(db, user);

				await service.ArchiveAsync(user.Id, link);
				await service.ArchiveAsync(user.Id, link);

				Assert.Single(db.ArchivedLinks);
		}

		[Fact]
		public async Task DeleteArchivedAsync_IsScopedToOwner()
		{
				using var db = CreateDb();
				var service = CreateService(db);
				var owner = CreateUser(db);
				var other = CreateUser(db, "other@example.com");
				var link = CreateLink(db, owner);
				await service.ArchiveAsync(owner.Id, link);
				var archived = Assert.Single(db.ArchivedLinks);

				await service.DeleteArchivedAsync(other.Id, archived.Id);
				Assert.Single(db.ArchivedLinks);

				await service.DeleteArchivedAsync(owner.Id, archived.Id);
				Assert.Empty(db.ArchivedLinks);
		}
}
