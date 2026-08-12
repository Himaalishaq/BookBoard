using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookBoard.Migrations
{
    /// <inheritdoc />
    public partial class AddBoardAestheticFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AccentColor",
                table: "Boards",
                type: "TEXT",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BackgroundStyle",
                table: "Boards",
                type: "TEXT",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "IconSymbols",
                table: "Boards",
                type: "TEXT",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Theme",
                table: "Boards",
                type: "TEXT",
                maxLength: 50,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AccentColor",
                table: "Boards");

            migrationBuilder.DropColumn(
                name: "BackgroundStyle",
                table: "Boards");

            migrationBuilder.DropColumn(
                name: "IconSymbols",
                table: "Boards");

            migrationBuilder.DropColumn(
                name: "Theme",
                table: "Boards");
        }
    }
}
