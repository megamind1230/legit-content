using LegitContent.Data;
using LegitContent.Models;
using LegitContent.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace LegitContent.Tests;

public class UserServiceTests
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

		private static UserService CreateService(AppDbContext db) => new(db, TestClock.At);

		[Fact]
		public async Task RegisterAsync_NewEmail_CreatesVerifiableUser()
		{
				using var db = CreateDb();
				var service = CreateService(db);

				var result = await service.RegisterAsync("Reader@Example.com", "longpassword");

				Assert.True(result.Success);
				var verified = await service.VerifyAsync("reader@example.com", "longpassword");
				Assert.NotNull(verified);
				Assert.Equal(result.User!.Id, verified.Id);
		}

		[Fact]
		public async Task RegisterAsync_DuplicateEmail_IsRejected()
		{
				using var db = CreateDb();
				var service = CreateService(db);
				await service.RegisterAsync("reader@example.com", "longpassword");

				var second = await service.RegisterAsync("reader@example.com", "otherpassword");

				Assert.False(second.Success);
				Assert.Contains("already", second.Error);
		}

		[Fact]
		public async Task RegisterAsync_ShortPassword_IsRejected()
		{
				using var db = CreateDb();
				var service = CreateService(db);

				var result = await service.RegisterAsync("reader@example.com", "short");

				Assert.False(result.Success);
				Assert.Empty(db.Users);
		}

		[Fact]
		public async Task VerifyAsync_WrongPassword_ReturnsNull()
		{
				using var db = CreateDb();
				var service = CreateService(db);
				await service.RegisterAsync("reader@example.com", "longpassword");

				var verified = await service.VerifyAsync("reader@example.com", "wrongpassword");

				Assert.Null(verified);
		}
}
