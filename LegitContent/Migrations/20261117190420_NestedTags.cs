using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LegitContent.Migrations
{
		/// <inheritdoc />
		public partial class NestedTags : Migration
		{
				/// <inheritdoc />
				protected override void Up(MigrationBuilder migrationBuilder)
				{
						migrationBuilder.CreateTable(
								name: "Tags",
								columns: table => new
								{
										Id = table.Column<int>(type: "INTEGER", nullable: false)
												.Annotation("Sqlite:Autoincrement", true),
										Name = table.Column<string>(type: "TEXT", nullable: false),
										ParentId = table.Column<int>(type: "INTEGER", nullable: true)
								},
								constraints: table =>
								{
										table.PrimaryKey("PK_Tags", x => x.Id);
										table.ForeignKey(
												name: "FK_Tags_Tags_ParentId",
												column: x => x.ParentId,
												principalTable: "Tags",
												principalColumn: "Id");
								});

						migrationBuilder.CreateTable(
								name: "LinkTags",
								columns: table => new
								{
										LinkId = table.Column<int>(type: "INTEGER", nullable: false),
										TagId = table.Column<int>(type: "INTEGER", nullable: false)
								},
								constraints: table =>
								{
										table.PrimaryKey("PK_LinkTags", x => new { x.LinkId, x.TagId });
										table.ForeignKey(
												name: "FK_LinkTags_Links_LinkId",
												column: x => x.LinkId,
												principalTable: "Links",
												principalColumn: "Id",
												onDelete: ReferentialAction.Cascade);
										table.ForeignKey(
												name: "FK_LinkTags_Tags_TagId",
												column: x => x.TagId,
												principalTable: "Tags",
												principalColumn: "Id",
												onDelete: ReferentialAction.Cascade);
								});

						migrationBuilder.CreateIndex(
								name: "IX_LinkTags_TagId",
								table: "LinkTags",
								column: "TagId");

						migrationBuilder.CreateIndex(
								name: "IX_Tags_ParentId_Name",
								table: "Tags",
								columns: new[] { "ParentId", "Name" },
								unique: true);

						migrationBuilder.Sql("""
                INSERT INTO Tags (Name)
                SELECT DISTINCT lower(Topic)
                FROM Links
                WHERE Topic IS NOT NULL;

                INSERT INTO LinkTags (LinkId, TagId)
                SELECT l.Id, t.Id
                FROM Links l
                JOIN Tags t ON t.Name = lower(l.Topic)
                WHERE l.Topic IS NOT NULL;
                """);

						migrationBuilder.DropColumn(
								name: "Topic",
								table: "Links");
				}

				/// <inheritdoc />
				protected override void Down(MigrationBuilder migrationBuilder)
				{
						migrationBuilder.DropTable(
								name: "LinkTags");

						migrationBuilder.DropTable(
								name: "Tags");

						migrationBuilder.AddColumn<string>(
								name: "Topic",
								table: "Links",
								type: "TEXT",
								nullable: false,
								defaultValue: "");
				}
		}
}
