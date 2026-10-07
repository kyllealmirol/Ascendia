using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ascendia.Migrations
{
    /// <inheritdoc />
    public partial class LinkStudentsToCourses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CourseId",
                table: "Students",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE Courses
                SET CourseCode = UPPER(TRIM(CourseCode));

                UPDATE Students
                SET CourseId = (
                    SELECT Courses.Id
                    FROM Courses
                    WHERE Courses.CourseCode = Students.CourseCode COLLATE NOCASE
                       OR Courses.CourseCode = Students.Course COLLATE NOCASE
                       OR Courses.CourseName = Students.Course COLLATE NOCASE
                    ORDER BY Courses.Id
                    LIMIT 1
                )
                WHERE CourseId IS NULL;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Students_CourseId",
                table: "Students",
                column: "CourseId");

            migrationBuilder.Sql(
                "CREATE UNIQUE INDEX IX_Courses_CourseCode ON Courses (CourseCode COLLATE NOCASE);");

            migrationBuilder.AddForeignKey(
                name: "FK_Students_Courses_CourseId",
                table: "Students",
                column: "CourseId",
                principalTable: "Courses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Students_Courses_CourseId",
                table: "Students");

            migrationBuilder.DropIndex(
                name: "IX_Students_CourseId",
                table: "Students");

            migrationBuilder.DropIndex(
                name: "IX_Courses_CourseCode",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "CourseId",
                table: "Students");
        }
    }
}
