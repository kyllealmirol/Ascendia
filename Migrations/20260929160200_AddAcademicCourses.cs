using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Ascendia.Migrations
{
    /// <inheritdoc />
    public partial class AddAcademicCourses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Courses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CourseCode = table.Column<string>(type: "TEXT", nullable: false),
                    CourseName = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Courses", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "Courses",
                columns: new[] { "Id", "CourseCode", "CourseName", "Description" },
                values: new object[,]
                {
                    { 1, "BSCpE", "BS Computer Engineering", "This program provides comprehensive training, technical knowledge, and hands-on skills designed to equip students for professional careers in the industry." },
                    { 2, "BSIT", "BS Information Technology", "Focuses on the installation, implementation, and administration of computer systems and networks." },
                    { 3, "BSCS", "BS Computer Science", "Focuses on the mathematical and theoretical foundations of computing and algorithmic design." }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Courses");
        }
    }
}
