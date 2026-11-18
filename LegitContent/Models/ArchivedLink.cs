namespace LegitContent.Models;

public class ArchivedLink
{
		public int Id { get; set; }

		public string UserId { get; set; } = "";

		public User? User { get; set; }

		public string Title { get; set; } = "";

		public string Url { get; set; } = "";

		public DateTime Created { get; set; }
}
