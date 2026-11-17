using LegitContent.Data;
using LegitContent.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace LegitContent.Services;

public class UserService(AppDbContext db, TimeProvider clock)
{
		private const int MinPasswordLength = 8;

		private readonly PasswordHasher<User> _hasher = new();

		public async Task<AuthResult> RegisterAsync(string email, string password, CancellationToken cancellationToken = default)
		{
				var normalizedEmail = email.Trim().ToLowerInvariant();
				if (normalizedEmail.Length == 0)
						return AuthResult.Fail("Enter an email address.");
				if (password.Length < MinPasswordLength)
						return AuthResult.Fail($"Password must be at least {MinPasswordLength} characters.");

				var emailTaken = await db.Users.AnyAsync(u => u.Email == normalizedEmail, cancellationToken);
				if (emailTaken)
						return AuthResult.Fail("That email is already registered.");

				var user = new User { Email = normalizedEmail, Created = clock.GetUtcNow().UtcDateTime };
				user.PasswordHash = _hasher.HashPassword(user, password);
				try
				{
						db.Users.Add(user);
						await db.SaveChangesAsync(cancellationToken);
						return AuthResult.Ok(user);
				}
				catch (DbUpdateException)
				{
						return AuthResult.Fail("That email is already registered.");
				}
		}

		public async Task<User?> VerifyAsync(string email, string password, CancellationToken cancellationToken = default)
		{
				var normalizedEmail = email.Trim().ToLowerInvariant();
				var user = await db.Users.FirstOrDefaultAsync(u => u.Email == normalizedEmail, cancellationToken);
				if (user is null) return null;

				var result = _hasher.VerifyHashedPassword(user, user.PasswordHash, password);
				if (result == PasswordVerificationResult.Failed) return null;

				if (result == PasswordVerificationResult.SuccessRehashNeeded)
				{
						user.PasswordHash = _hasher.HashPassword(user, password);
						await db.SaveChangesAsync(cancellationToken);
				}
				return user;
		}
}
