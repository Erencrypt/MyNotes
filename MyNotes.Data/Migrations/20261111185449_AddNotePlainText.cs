using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyNotes.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddNotePlainText : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PlainText",
                table: "Notes",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PlainText",
                table: "Notes");
        }
    }
}
