namespace LegitContent.Models;

public sealed record LinkCard(
	int Id,
	string Title,
	string Url,
	string Domain,
	string Description,
	ContentType Type,
	IReadOnlyList<string> Tags,
	int Upvotes,
	bool UpvotedByMe,
	string Submitter,
	string Age,
	bool ArchivedByMe);
