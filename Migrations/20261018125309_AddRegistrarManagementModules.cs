using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ascendia.Migrations
{
    /// <inheritdoc />
    public partial class AddRegistrarManagementModules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Instructors",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FullName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Department = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "TEXT", maxLength: 254, nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Instructors", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Notices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Message = table.Column<string>(type: "TEXT", nullable: true),
                    Category = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    ImagePath = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    DatePostedUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ExpiryDateUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsPinned = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notices", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ShiftingCreditingApplications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    StudentId = table.Column<int>(type: "INTEGER", nullable: true),
                    StudentNumber = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    StudentName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    ApplicationType = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    CurrentCourse = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    DesiredCourseCode = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    PreviousSchool = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    CreditedSubjects = table.Column<string>(type: "TEXT", nullable: true),
                    Remarks = table.Column<string>(type: "TEXT", nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    SubmittedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ReviewedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShiftingCreditingApplications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShiftingCreditingApplications_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "SubjectSections",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CourseCode = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    SubjectCode = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    SubjectName = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    SectionName = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    YearLevel = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    Semester = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    Units = table.Column<int>(type: "INTEGER", nullable: false),
                    Schedule = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Room = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    InstructorId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubjectSections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SubjectSections_Instructors_InstructorId",
                        column: x => x.InstructorId,
                        principalTable: "Instructors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ShiftingCreditingApplications_StudentId",
                table: "ShiftingCreditingApplications",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_SubjectSections_CourseCode_SubjectCode_SectionName_YearLevel_Semester",
                table: "SubjectSections",
                columns: new[] { "CourseCode", "SubjectCode", "SectionName", "YearLevel", "Semester" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SubjectSections_InstructorId",
                table: "SubjectSections",
                column: "InstructorId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Notices");

            migrationBuilder.DropTable(
                name: "ShiftingCreditingApplications");

            migrationBuilder.DropTable(
                name: "SubjectSections");

            migrationBuilder.DropTable(
                name: "Instructors");
        }
    }
}
