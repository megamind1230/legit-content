namespace LegitContent.Models;

public class Tag
{
		public int Id { get; set; }

		public string Name { get; set; } = "";

		public int? ParentId { get; set; }

		public Tag? Parent { get; set; }

		public List<LinkTag> Links { get; set; } = [];
}
