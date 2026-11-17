using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LegitContent.Migrations
{
		/// <inheritdoc />
		public partial class InitialCreate : Migration
		{
				/// <inheritdoc />
				protected override void Up(MigrationBuilder migrationBuilder)
				{
						migrationBuilder.CreateTable(
								name: "Users",
								columns: table => new
								{
										Id = table.Column<string>(type: "TEXT", nullable: false),
										Email = table.Column<string>(type: "TEXT", nullable: false),
										PasswordHash = table.Column<string>(type: "TEXT", nullable: false),
										IsAdmin = table.Column<bool>(type: "INTEGER", nullable: false),
										Created = table.Column<DateTime>(type: "TEXT", nullable: false)
								},
								constraints: table =>
								{
										table.PrimaryKey("PK_Users", x => x.Id);
								});

						migrationBuilder.CreateTable(
								name: "Links",
								columns: table => new
								{
										Id = table.Column<int>(type: "INTEGER", nullable: false)
												.Annotation("Sqlite:Autoincrement", true),
										Url = table.Column<string>(type: "TEXT", nullable: false),
										NormalizedUrl = table.Column<string>(type: "TEXT", nullable: false),
										Title = table.Column<string>(type: "TEXT", nullable: false),
										Description = table.Column<string>(type: "TEXT", nullable: false),
										Type = table.Column<string>(type: "TEXT", nullable: false),
										Topic = table.Column<string>(type: "TEXT", nullable: false),
										SubmitterId = table.Column<string>(type: "TEXT", nullable: false),
										Created = table.Column<DateTime>(type: "TEXT", nullable: false)
								},
								constraints: table =>
								{
										table.PrimaryKey("PK_Links", x => x.Id);
										table.ForeignKey(
												name: "FK_Links_Users_SubmitterId",
												column: x => x.SubmitterId,
												principalTable: "Users",
												principalColumn: "Id",
												onDelete: ReferentialAction.Cascade);
								});

						migrationBuilder.CreateTable(
								name: "Upvotes",
								columns: table => new
								{
										Id = table.Column<int>(type: "INTEGER", nullable: false)
												.Annotation("Sqlite:Autoincrement", true),
										UserId = table.Column<string>(type: "TEXT", nullable: false),
										LinkId = table.Column<int>(type: "INTEGER", nullable: false),
										Created = table.Column<DateTime>(type: "TEXT", nullable: false)
								},
								constraints: table =>
								{
										table.PrimaryKey("PK_Upvotes", x => x.Id);
										table.ForeignKey(
												name: "FK_Upvotes_Links_LinkId",
												column: x => x.LinkId,
												principalTable: "Links",
												principalColumn: "Id",
												onDelete: ReferentialAction.Cascade);
										table.ForeignKey(
												name: "FK_Upvotes_Users_UserId",
												column: x => x.UserId,
												principalTable: "Users",
												principalColumn: "Id",
												onDelete: ReferentialAction.Cascade);
								});

						migrationBuilder.CreateIndex(
								name: "IX_Links_NormalizedUrl",
								table: "Links",
								column: "NormalizedUrl");

						migrationBuilder.CreateIndex(
								name: "IX_Links_SubmitterId",
								table: "Links",
								column: "SubmitterId");

						migrationBuilder.CreateIndex(
								name: "IX_Upvotes_LinkId",
								table: "Upvotes",
								column: "LinkId");

						migrationBuilder.CreateIndex(
								name: "IX_Upvotes_UserId_LinkId",
								table: "Upvotes",
								columns: new[] { "UserId", "LinkId" },
								unique: true);

						migrationBuilder.CreateIndex(
								name: "IX_Users_Email",
								table: "Users",
								column: "Email",
								unique: true);
				}

				/// <inheritdoc />
				protected override void Down(MigrationBuilder migrationBuilder)
				{
						migrationBuilder.DropTable(
								name: "Upvotes");

						migrationBuilder.DropTable(
								name: "Links");

						migrationBuilder.DropTable(
								name: "Users");
				}
		}
}
