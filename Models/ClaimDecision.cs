using System.ComponentModel.DataAnnotations;

namespace AccreditationSystem.Models
{
    public class ClaimDecision
    {
        public int ClaimID { get; set; }

        [Required(ErrorMessage = "Please select a decision")]
        public string Decision { get; set; } // "Approved" or "Rejected"

        [Required(ErrorMessage = "Please provide feedback for your decision")]
        public string Feedback { get; set; }

        public string DecisionMaker { get; set; } // HOD name
        public DateTime DecisionDate { get; set; } = DateTime.Now;
    }

    // This might already exist in your project, but adding it here for completeness
    public class AccreditationClaim
    {
        public int ClaimID { get; set; }
        public string SchoolEmail { get; set; }
        public string AccreditationType { get; set; }
        public string AccreditationLevel { get; set; }
        public string PreviousStatus { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Status { get; set; }
        public DateTime SubmissionDate { get; set; }
        public string AdditionalComments { get; set; }
        public string ContactPhone { get; set; }
        public string ContactName { get; set; }
        public string ContactPosition { get; set; }
        public string ContactEmail { get; set; }
        // Additional properties as needed
    }
}
