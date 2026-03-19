using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DailyWork.Agents.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddChatMessageToolCalls : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ChatMessageToolCalls",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ConversationId = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ToolName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Arguments = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Result = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsError = table.Column<bool>(type: "bit", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatMessageToolCalls", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessageToolCalls_ConversationId_Timestamp",
                table: "ChatMessageToolCalls",
                columns: new[] { "ConversationId", "Timestamp" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChatMessageToolCalls");
        }
    }
}
