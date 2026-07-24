using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookBoard.Migrations
{
    /// <inheritdoc />
    public partial class AddBookApiFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PublishedYear",
                table: "BoardBooks",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ShortDescription",
                table: "BoardBooks",
                type: "TEXT",
                maxLength: 600,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PublishedYear",
                table: "BoardBooks");

            migrationBuilder.DropColumn(
                name: "ShortDescription",
                table: "BoardBooks");
        }
    }
}
