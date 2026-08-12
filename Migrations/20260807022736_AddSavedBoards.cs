using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookBoard.Migrations
{
    /// <inheritdoc />
    public partial class AddSavedBoards : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SavedBoards",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    BoardId = table.Column<int>(type: "INTEGER", nullable: false),
                    SavedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SavedBoards", x => new { x.UserId, x.BoardId });
                    table.ForeignKey(
                        name: "FK_SavedBoards_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SavedBoards_Boards_BoardId",
                        column: x => x.BoardId,
                        principalTable: "Boards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SavedBoards_BoardId",
                table: "SavedBoards",
                column: "BoardId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SavedBoards");
        }
    }
}
