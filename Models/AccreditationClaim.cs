using System;
using System.ComponentModel.DataAnnotations;

namespace AccreditationSystem.Data
{
    public class AccreditationClaim
    {
        public int ClaimID { get; set; }

        [Required(ErrorMessage = "School email is required")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address")]
        public string SchoolEmail { get; set; }

        [Required(ErrorMessage = "Accreditation type is required")]
        public string AccreditationType { get; set; }

        [Required(ErrorMessage = "Accreditation level is required")]
        public string AccreditationLevel { get; set; }

        public string PreviousStatus { get; set; }

        [Required(ErrorMessage = "Start date is required")]
        public DateTime StartDate { get; set; }

        [Required(ErrorMessage = "End date is required")]
        public DateTime EndDate { get; set; }

        [Required(ErrorMessage = "Self-assessment report path is required")]
        public string SelfAssessmentReportPath { get; set; }

        [Required(ErrorMessage = "Curriculum documentation path is required")]
        public string CurriculumDocumentationPath { get; set; }

        [Required(ErrorMessage = "Faculty credentials path is required")]
        public string FacultyCredentialsPath { get; set; }

        public string AdditionalDocumentationPath { get; set; }

        [Required(ErrorMessage = "Academic standards information is required")]
        public string AcademicStandards { get; set; }

        [Required(ErrorMessage = "Faculty standards information is required")]
        public string FacultyStandards { get; set; }

        [Required(ErrorMessage = "Facility standards information is required")]
        public string FacilityStandards { get; set; }

        [Required(ErrorMessage = "Student services information is required")]
        public string StudentServices { get; set; }

        [Required(ErrorMessage = "Contact name is required")]
        public string ContactName { get; set; }

        [Required(ErrorMessage = "Contact position is required")]
        public string ContactPosition { get; set; }

        [Required(ErrorMessage = "Contact email is required")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address")]
        public string ContactEmail { get; set; }

        [Required(ErrorMessage = "Contact phone is required")]
        [Phone(ErrorMessage = "Please enter a valid phone number")]
        public string ContactPhone { get; set; }

        public string AdditionalComments { get; set; }

        [Required(ErrorMessage = "Certification is required")]
        public bool Certified { get; set; }

        [Required(ErrorMessage = "Status is required")]
        public string Status { get; set; }

        public int? ReviewerID { get; set; }

        public string ReviewNotes { get; set; }

        public DateTime SubmissionDate { get; set; }

        public DateTime LastUpdatedDate { get; set; }

        public DateTime? ReviewDate { get; set; }

        public DateTime? ApprovalDate { get; set; }
    }
}