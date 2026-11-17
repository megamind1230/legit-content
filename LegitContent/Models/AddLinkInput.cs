using System.ComponentModel.DataAnnotations;

namespace LegitContent.Models;

public class AddLinkInput
{
		[Required, Url, MaxLength(2048)]
		public string Url { get; set; } = "";

		[Required, MaxLength(200)]
		public string Title { get; set; } = "";

		[Required, MaxLength(500)]
		public string Description { get; set; } = "";

		[Required]
		public ContentType? Type { get; set; }

		[Required, MaxLength(300)]
		public string Tags { get; set; } = "";
}
