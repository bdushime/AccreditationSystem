using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AccreditationSystem.Pages.Admin
{
    public class DownloadCertificateModel : PageModel
    {
        [BindProperty]
        [Required(ErrorMessage = "Certificate type is required")]
        public string CertificateType { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "Certificate ID is required")]
        public string CertificateId { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "Email address is required")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address")]
        public string Email { get; set; }

        public bool CertificateFound { get; set; } = false;
        public string ErrorMessage { get; set; }
        public Certificate Certificate { get; set; }
        public List<Certificate> RelatedCertificates { get; set; } = new List<Certificate>();

        public void OnGet()
        {
            // Initialize any necessary data
        }

        public IActionResult OnPost()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            // In a real app, this would check the database for the certificate
            // For demonstration, we'll simulate finding a certificate if the CertificateId is "CERT123"
            if (CertificateId == "CERT123" && Email == "test@example.com")
            {
                CertificateFound = true;
                Certificate = GetSampleCertificate();
                RelatedCertificates = GetRelatedCertificates();
            }
            else
            {
                ErrorMessage = "No certificate found with the provided details. Please check your Certificate ID and Email.";
            }

            return Page();
        }

        private Certificate GetSampleCertificate()
        {
            // This would typically come from a database
            return new Certificate
            {
                Id = "CERT123",
                Type = CertificateType,
                Title = GetCertificateTitle(CertificateType),
                InstitutionName = "Sunrise International Academy",
                IssueDate = DateTime.Now.AddMonths(-6),
                ExpiryDate = DateTime.Now.AddYears(2),
                Status = "Active",
                StatusBadgeClass = "bg-success"
            };
        }

        private List<Certificate> GetRelatedCertificates()
        {
            // This would typically come from a database
            return new List<Certificate>
            {
                new Certificate
                {
                    Id = "CERT456",
                    Type = "Program Approval",
                    Title = "International Baccalaureate Program Approval",
                    InstitutionName = "Sunrise International Academy",
                    IssueDate = DateTime.Now.AddYears(-1),
                    ExpiryDate = DateTime.Now.AddYears(1),
                    Status = "Active",
                    StatusBadgeClass = "bg-success"
                },
                new Certificate
                {
                    Id = "CERT789",
                    Type = "Staff Qualification",
                    Title = "Teaching Staff Qualification Certification",
                    InstitutionName = "Sunrise International Academy",
                    IssueDate = DateTime.Now.AddMonths(-8),
                    ExpiryDate = DateTime.Now.AddMonths(4),
                    Status = "Expiring Soon",
                    StatusBadgeClass = "bg-warning"
                }
            };
        }

        private string GetCertificateTitle(string certificateType)
        {
            switch (certificateType)
            {
                case "School Registration":
                    return "Official School Registration";
                case "Accreditation":
                    return "Educational Institution Accreditation";
                case "Program Approval":
                    return "Educational Program Approval";
                case "Staff Qualification":
                    return "Staff Qualification Certification";
                default:
                    return "Education Certificate";
            }
        }
    }

    public class Certificate
    {
        public string Id { get; set; }
        public string Type { get; set; }
        public string Title { get; set; }
        public string InstitutionName { get; set; }
        public DateTime IssueDate { get; set; }
        public DateTime ExpiryDate { get; set; }
        public string Status { get; set; }
        public string StatusBadgeClass { get; set; }
    }
}
