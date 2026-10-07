namespace Ascendia.Models
{
    public class Student
    {
        public int Id { get; set; }
        public string? StudentNumber { get; set; }
        public string? FullName { get; set; }
        public string? StudentName { get; set; }
        public string? Course { get; set; }
        public string? CourseCode { get; set; }
        public int? CourseId { get; set; }
        public AcademicCourse? AcademicCourse { get; set; }
        public string? Section { get; set; }
        public string? StudentType { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Gender { get; set; }
        [System.ComponentModel.DataAnnotations.DataType(System.ComponentModel.DataAnnotations.DataType.Date)]
        [System.ComponentModel.DataAnnotations.DisplayFormat(
            DataFormatString = "{0:yyyy-MM-dd}",
            ApplyFormatInEditMode = true)]
        public DateTime BirthDate { get; set; }
        public string? YearAndSemester { get; set; }
        public string? Address { get; set; }
        public string? GuardianName { get; set; }
        public string? GuardianPhone { get; set; }
        
        public string? Status { get; set; } = "Pending";

        public decimal? RemainingBalance { get; set; }
        public string? PaymentStatus { get; set; }
        public string? PaymentReceiptPath { get; set; }

        public string? ProfilePicturePath { get; set; }
        public string? PsaBirthCertificatePath { get; set; }
        public string? Form138Path { get; set; }
        public string? Form137Path { get; set; }
        public string? GoodMoralPath { get; set; }
        public string? TorPath { get; set; }
    }
}