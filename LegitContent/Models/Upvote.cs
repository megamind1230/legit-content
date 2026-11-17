namespace LegitContent.Models;

public class Upvote
{
		public int Id { get; set; }

		public string UserId { get; set; } = "";

		public User? User { get; set; }

		public int LinkId { get; set; }

		public Link? Link { get; set; }

		public DateTime Created { get; set; }
}
