using Microsoft.EntityFrameworkCore;
using Ascendia.Models;

namespace Ascendia.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Student> Students { get; set; } = default!;
        public DbSet<AcademicCourse> Courses { get; set; } = default!;
        public DbSet<RegistrarAccount> RegistrarAccounts { get; set; } = default!;
        public DbSet<AcademicRequest> AcademicRequests { get; set; } = default!;
        public DbSet<ShiftingCreditingApplication> ShiftingCreditingApplications { get; set; } = default!;
        public DbSet<SchoolNotice> Notices { get; set; } = default!;
        public DbSet<Instructor> Instructors { get; set; } = default!;
        public DbSet<SubjectSection> SubjectSections { get; set; } = default!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Student>()
                .HasIndex(student => student.StudentNumber)
                .IsUnique()
                .HasFilter("\"StudentNumber\" IS NOT NULL AND \"StudentNumber\" <> ''");

            modelBuilder.Entity<RegistrarAccount>()
                .HasIndex(account => account.NormalizedUsername)
                .IsUnique();

            modelBuilder.Entity<AcademicCourse>()
                .HasIndex(course => course.CourseCode)
                .IsUnique();

            modelBuilder.Entity<ShiftingCreditingApplication>()
                .HasOne(application => application.Student)
                .WithMany()
                .HasForeignKey(application => application.StudentId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Instructor>()
                .HasMany(instructor => instructor.SubjectSections)
                .WithOne(section => section.Instructor)
                .HasForeignKey(section => section.InstructorId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<SubjectSection>()
                .HasIndex(section => new
                {
                    section.CourseCode,
                    section.SubjectCode,
                    section.SectionName,
                    section.YearLevel,
                    section.Semester
                })
                .IsUnique();

            modelBuilder.Entity<AcademicRequest>()
                .HasOne(request => request.Student)
                .WithMany()
                .HasForeignKey(request => request.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Student>()
                .HasOne(student => student.AcademicCourse)
                .WithMany()
                .HasForeignKey(student => student.CourseId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<AcademicCourse>().HasData(
                new AcademicCourse
                {
                    Id = 1,
                    CourseCode = "BSCpE",
                    CourseName = "BS Computer Engineering",
                    Description = "This program provides comprehensive training, technical knowledge, and hands-on skills designed to equip students for professional careers in the industry."
                },
                new AcademicCourse
                {
                    Id = 2,
                    CourseCode = "BSIT",
                    CourseName = "BS Information Technology",
                    Description = "Focuses on the installation, implementation, and administration of computer systems and networks."
                },
                new AcademicCourse
                {
                    Id = 3,
                    CourseCode = "BSCS",
                    CourseName = "BS Computer Science",
                    Description = "Focuses on the mathematical and theoretical foundations of computing and algorithmic design."
                });

            modelBuilder.Entity<Instructor>().HasData(
                new Instructor
                {
                    Id = 1,
                    FullName = "Sample Instructor",
                    Department = "Computer Engineering",
                    Email = "instructor@example.edu",
                    Status = "Active"
                });

            modelBuilder.Entity<SubjectSection>().HasData(
                new SubjectSection
                {
                    Id = 1,
                    CourseCode = "BSCpE",
                    SubjectCode = "CPE 411",
                    SubjectName = "Engineering Management",
                    SectionName = "4A",
                    YearLevel = "4th Year",
                    Semester = "1st Sem",
                    Units = 3,
                    InstructorId = 1
                },
                new SubjectSection
                {
                    Id = 2,
                    CourseCode = "BSIT",
                    SubjectCode = "IT 301",
                    SubjectName = "Information Assurance and Security",
                    SectionName = "3B",
                    YearLevel = "3rd Year",
                    Semester = "1st Sem",
                    Units = 3
                });

            modelBuilder.Entity<SchoolNotice>().HasData(
                new SchoolNotice
                {
                    Id = 1,
                    Title = "Enrollment is open",
                    Message = "Please review the available programs and submit your application online.",
                    Category = "Enrollment Schedule",
                    DatePostedUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    IsPinned = true,
                    IsActive = true
                });

            modelBuilder.Entity<ShiftingCreditingApplication>().HasData(
                new ShiftingCreditingApplication
                {
                    Id = 1,
                    StudentNumber = "DEMO-0001",
                    StudentName = "Sample Student",
                    ApplicationType = "Subject Evaluation",
                    CurrentCourse = "BS Information Technology",
                    PreviousSchool = "Sample College",
                    CreditedSubjects = "Introduction to Computing\nProgramming Fundamentals",
                    Status = "For Evaluation",
                    SubmittedAtUtc = new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc)
                });
        }
    }
}