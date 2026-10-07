using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ascendia.Migrations
{
    /// <inheritdoc />
    public partial class AddFullNameAndFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FullName",
                table: "Students",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FullName",
                table: "Students");
        }
    }
}
