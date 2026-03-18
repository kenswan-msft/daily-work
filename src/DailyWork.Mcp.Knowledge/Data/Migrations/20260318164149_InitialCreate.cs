using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DailyWork.Mcp.Knowledge.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "KnowledgeItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Url = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Language = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KnowledgeItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "KnowledgeTags",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KnowledgeTags", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "KnowledgeItemTag",
                columns: table => new
                {
                    ItemsId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TagsId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KnowledgeItemTag", x => new { x.ItemsId, x.TagsId });
                    table.ForeignKey(
                        name: "FK_KnowledgeItemTag_KnowledgeItems_ItemsId",
                        column: x => x.ItemsId,
                        principalTable: "KnowledgeItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_KnowledgeItemTag_KnowledgeTags_TagsId",
                        column: x => x.TagsId,
                        principalTable: "KnowledgeTags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeItems_Category",
                table: "KnowledgeItems",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeItems_CreatedAt",
                table: "KnowledgeItems",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeItems_Type",
                table: "KnowledgeItems",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeItemTag_TagsId",
                table: "KnowledgeItemTag",
                column: "TagsId");

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeTags_Name",
                table: "KnowledgeTags",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "KnowledgeItemTag");

            migrationBuilder.DropTable(
                name: "KnowledgeItems");

            migrationBuilder.DropTable(
                name: "KnowledgeTags");
        }
    }
}
