using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ascendia.Migrations
{
    /// <inheritdoc />
    public partial class AddStudentPaymentAssessment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PaymentStatus",
                table: "Students",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "RemainingBalance",
                table: "Students",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PaymentStatus",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "RemainingBalance",
                table: "Students");
        }
    }
}
