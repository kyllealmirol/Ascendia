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
        }
    }
}