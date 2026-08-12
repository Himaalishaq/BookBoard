using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookBoard.Migrations
{
    /// <inheritdoc />
    public partial class AddBoardVisualItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BoardVisualItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BoardId = table.Column<int>(type: "INTEGER", nullable: false),
                    ItemType = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    Content = table.Column<string>(type: "TEXT", maxLength: 700, nullable: false),
                    TileStyle = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BoardVisualItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BoardVisualItems_Boards_BoardId",
                        column: x => x.BoardId,
                        principalTable: "Boards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BoardVisualItems_BoardId",
                table: "BoardVisualItems",
                column: "BoardId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BoardVisualItems");
        }
    }
}
