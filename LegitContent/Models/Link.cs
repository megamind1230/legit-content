namespace LegitContent.Models;

public class Link
{
		public int Id { get; set; }

		public string Url { get; set; } = "";

		public string NormalizedUrl { get; set; } = "";

		public string Title { get; set; } = "";

		public string Description { get; set; } = "";

		public ContentType Type { get; set; }

		public string SubmitterId { get; set; } = "";

		public User? Submitter { get; set; }

		public DateTime Created { get; set; }

		public List<Upvote> Upvotes { get; set; } = [];

		public List<LinkTag> Tags { get; set; } = [];
}
