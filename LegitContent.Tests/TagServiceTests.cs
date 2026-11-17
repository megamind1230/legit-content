using LegitContent.Data;
using LegitContent.Models;
using LegitContent.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace LegitContent.Tests;

public class TagServiceTests
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

		[Fact]
		public async Task ResolveTagsAsync_CreatesHierarchy()
		{
				using var db = CreateDb();
				var service = new TagService(db);

				var result = await service.ResolveTagsAsync(["medicine/anatomy"]);

				Assert.True(result.Success);
				var leaf = Assert.Single(result.Tags);
				Assert.Equal("anatomy", leaf.Name);
				Assert.NotNull(leaf.Parent);
				Assert.Equal("medicine", leaf.Parent!.Name);
				Assert.Equal(2, db.Tags.Count());
		}

		[Fact]
		public async Task ResolveTagsAsync_ReusesExistingHierarchy_IgnoringCase()
		{
				using var db = CreateDb();
				var service = new TagService(db);
				await service.ResolveTagsAsync(["medicine/anatomy"]);

				var second = await service.ResolveTagsAsync(["Medicine / Anatomy"]);

				var leaf = Assert.Single(second.Tags);
				Assert.Equal(2, db.Tags.Count());
		}

		[Fact]
		public async Task ResolveTagsAsync_TooManyTags_IsRejected()
		{
				using var db = CreateDb();
				var service = new TagService(db);
				var paths = Enumerable.Range(0, TagService.MaxTagsPerLink + 1).Select(i => $"tag-{i}");

				var result = await service.ResolveTagsAsync(paths);

				Assert.False(result.Success);
				Assert.Contains("at most", result.Error);
		}

		[Fact]
		public async Task ResolveTagsAsync_TooDeep_IsRejected()
		{
				using var db = CreateDb();
				var service = new TagService(db);

				var result = await service.ResolveTagsAsync(["a/b/c/d/e"]);

				Assert.False(result.Success);
				Assert.Contains("levels deep", result.Error);
		}

		[Fact]
		public async Task ResolveTagsAsync_InvalidCharacters_IsRejected()
		{
				using var db = CreateDb();
				var service = new TagService(db);

				var result = await service.ResolveTagsAsync(["bad/tag!"]);

				Assert.False(result.Success);
				Assert.Empty(db.Tags);
		}

		[Fact]
		public async Task FindTagsAsync_UnknownTag_IsRejectedWithoutCreating()
		{
				using var db = CreateDb();
				var service = new TagService(db);

				var result = await service.FindTagsAsync(["medicine/anatomy"]);

				Assert.False(result.Success);
				Assert.Empty(db.Tags);
		}

		[Fact]
		public async Task SearchAsync_ReturnsFullPath_WithLinkCounts()
		{
				using var db = CreateDb();
				var service = new TagService(db);
				var leaf = (await service.ResolveTagsAsync(["medicine/anatomy"])).Tags.Single();
				var user = new User { Email = "tester@example.com" };
				db.Users.Add(user);
				db.Links.Add(new Link { Url = "https://example.com/a", NormalizedUrl = "https://example.com/a", SubmitterId = user.Id, Tags = [new LinkTag { TagId = leaf.Id }] });
				db.SaveChanges();

				var results = await service.SearchAsync("ana");

				var item = Assert.Single(results);
				Assert.Equal("medicine/anatomy", item.Path);
				Assert.Equal(1, item.LinkCount);
		}

		[Fact]
		public async Task SearchAsync_MatchesParentPath()
		{
				using var db = CreateDb();
				var service = new TagService(db);
				await service.ResolveTagsAsync(["medicine/anatomy"]);

				var results = await service.SearchAsync("medicine");

				Assert.Contains(results, item => item.Path == "medicine/anatomy");
				Assert.Contains(results, item => item.Path == "medicine");
		}

		[Fact]
		public async Task GetPathsAsync_BuildsFullPaths()
		{
				using var db = CreateDb();
				var service = new TagService(db);
				var leaf = (await service.ResolveTagsAsync(["medicine/anatomy"])).Tags.Single();

				var paths = await service.GetPathsAsync();

				Assert.Equal("medicine/anatomy", paths[leaf.Id]);
		}
}
