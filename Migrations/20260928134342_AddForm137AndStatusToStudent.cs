using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ascendia.Migrations
{
    /// <inheritdoc />
    public partial class AddForm137AndStatusToStudent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "RequestType",
                table: "Students",
                newName: "TorPath");

            migrationBuilder.RenameColumn(
                name: "RequestReason",
                table: "Students",
                newName: "GoodMoralPath");

            migrationBuilder.AddColumn<string>(
                name: "Form137Path",
                table: "Students",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Form138Path",
                table: "Students",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Form137Path",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "Form138Path",
                table: "Students");

            migrationBuilder.RenameColumn(
                name: "TorPath",
                table: "Students",
                newName: "RequestType");

            migrationBuilder.RenameColumn(
                name: "GoodMoralPath",
                table: "Students",
                newName: "RequestReason");
        }
    }
}
