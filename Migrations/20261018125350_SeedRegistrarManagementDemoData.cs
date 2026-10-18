using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Ascendia.Migrations
{
    /// <inheritdoc />
    public partial class SeedRegistrarManagementDemoData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Instructors",
                columns: new[] { "Id", "Department", "Email", "FullName", "Status" },
                values: new object[] { 1, "Computer Engineering", "instructor@example.edu", "Sample Instructor", "Active" });

            migrationBuilder.InsertData(
                table: "Notices",
                columns: new[] { "Id", "Category", "DatePostedUtc", "ExpiryDateUtc", "ImagePath", "IsActive", "IsPinned", "Message", "Title" },
                values: new object[] { 1, "Enrollment Schedule", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, true, true, "Please review the available programs and submit your application online.", "Enrollment is open" });

            migrationBuilder.InsertData(
                table: "ShiftingCreditingApplications",
                columns: new[] { "Id", "ApplicationType", "CreditedSubjects", "CurrentCourse", "DesiredCourseCode", "PreviousSchool", "Remarks", "ReviewedAtUtc", "Status", "StudentId", "StudentName", "StudentNumber", "SubmittedAtUtc" },
                values: new object[] { 1, "Subject Evaluation", "Introduction to Computing\nProgramming Fundamentals", "BS Information Technology", null, "Sample College", null, null, "For Evaluation", null, "Sample Student", "DEMO-0001", new DateTime(2026, 1, 2, 0, 0, 0, 0, DateTimeKind.Utc) });

            migrationBuilder.InsertData(
                table: "SubjectSections",
                columns: new[] { "Id", "CourseCode", "InstructorId", "Room", "Schedule", "SectionName", "Semester", "SubjectCode", "SubjectName", "Units", "YearLevel" },
                values: new object[,]
                {
                    { 2, "BSIT", null, null, null, "3B", "1st Sem", "IT 301", "Information Assurance and Security", 3, "3rd Year" },
                    { 1, "BSCpE", 1, null, null, "4A", "1st Sem", "CPE 411", "Engineering Management", 3, "4th Year" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Notices",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "ShiftingCreditingApplications",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "SubjectSections",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "SubjectSections",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Instructors",
                keyColumn: "Id",
                keyValue: 1);
        }
    }
}
