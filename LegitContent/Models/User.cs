namespace LegitContent.Models;

public class User
{
		public string Id { get; set; } = Guid.NewGuid().ToString();

		public string Email { get; set; } = "";

		public string PasswordHash { get; set; } = "";

		public bool IsAdmin { get; set; }

		public DateTime Created { get; set; }
}
