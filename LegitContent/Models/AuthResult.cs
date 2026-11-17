namespace LegitContent.Models;

public sealed record AuthResult(User? User, string? Error)
{
		public bool Success => User is not null;

		public static AuthResult Ok(User user) => new(user, null);

		public static AuthResult Fail(string error) => new(null, error);
}
