using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;

namespace AccreditationSystem.Pages.Admin
{
    public class TrackApplicationModel : PageModel
    {
        private readonly IConfiguration _configuration;

        [BindProperty]
        [Required(ErrorMessage = "Application ID is required")]
        public string ApplicationId { get; set; }

        public bool ApplicationFound { get; set; } = false;
        public string ErrorMessage { get; set; }
        public ApplicationDetails Application { get; set; }

        // Pass configuration through dependency injection
        public TrackApplicationModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void OnGet()
        {
            // Initial page load - nothing to do
        }

        public IActionResult OnPost()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            try
            {
                // Fetch application data from database
                int appId;
                if (!int.TryParse(ApplicationId, out appId))
                {
                    ErrorMessage = "Invalid application ID format. Please enter a numeric ID.";
                    return Page();
                }

                // For demonstration purposes, we'll try both methods:
                // 1. First try to load from database
                // 2. If that fails, fall back to sample data
                Application = GetApplicationFromDatabase(appId);

                // If not found in DB, use the sample data
                if (Application == null)
                {
                    Application = GetSampleApplication(appId);
                }

                if (Application != null)
                {
                    ApplicationFound = true;
                }
                else
                {
                    ErrorMessage = "No application found with the provided ID. Please check your application ID and try again.";
                }
            }
            catch (Exception ex)
            {
                // Log the detailed exception
                System.Diagnostics.Debug.WriteLine($"Error retrieving application: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");

                // Set a user-friendly error message
                ErrorMessage = "An error occurred while retrieving your application. Please try again later.";

                // Additional details if it's a SQL exception
                if (ex is SqlException sqlEx)
                {
                    System.Diagnostics.Debug.WriteLine($"SQL error number: {sqlEx.Number}");
                    // Don't expose SQL error details to end users, just log them
                }
            }

            return Page();
        }

        private ApplicationDetails GetApplicationFromDatabase(int id)
        {
            try
            {
                string connectionString = _configuration.GetConnectionString("DefaultConnection");

                // If connection string is not configured, log and return null
                if (string.IsNullOrEmpty(connectionString))
                {
                    System.Diagnostics.Debug.WriteLine("Connection string 'DefaultConnection' is not configured.");
                    return null;
                }

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    string query = @"
                        SELECT ID, SchoolEmail, InstitutionName, Email, Trade, 
                               AssessmentScore, ExperienceScore, CertificationScore, 
                               CurriculumScore, FacilitiesScore, PlacementScore,
                               ApplicationDate, Status, ReviewerID, ReviewDate,
                               ApprovalDate, ExpirationDate, Comments, ContactPerson
                        FROM Applications 
                        WHERE ID = @ApplicationId";

                    SqlCommand command = new SqlCommand(query, connection);
                    command.Parameters.AddWithValue("@ApplicationId", id);

                    connection.Open();
                    SqlDataReader reader = command.ExecuteReader();

                    if (reader.Read())
                    {
                        return new ApplicationDetails
                        {
                            Id = reader["ID"].ToString(),
                            SchoolEmail = reader["SchoolEmail"]?.ToString(),
                            InstitutionName = reader["InstitutionName"]?.ToString(),
                            Email = reader["Email"]?.ToString(),
                            Trade = reader["Trade"]?.ToString(),
                            AssessmentScore = reader["AssessmentScore"] != DBNull.Value ? Convert.ToInt32(reader["AssessmentScore"]) : 0,
                            ExperienceScore = reader["ExperienceScore"] != DBNull.Value ? Convert.ToInt32(reader["ExperienceScore"]) : 0,
                            CertificationScore = reader["CertificationScore"] != DBNull.Value ? Convert.ToInt32(reader["CertificationScore"]) : 0,
                            CurriculumScore = reader["CurriculumScore"] != DBNull.Value ? Convert.ToInt32(reader["CurriculumScore"]) : 0,
                            FacilitiesScore = reader["FacilitiesScore"] != DBNull.Value ? Convert.ToInt32(reader["FacilitiesScore"]) : 0,
                            PlacementScore = reader["PlacementScore"] != DBNull.Value ? Convert.ToInt32(reader["PlacementScore"]) : 0,
                            ApplicationDate = reader["ApplicationDate"] != DBNull.Value ? Convert.ToDateTime(reader["ApplicationDate"]) : DateTime.Now,
                            Status = reader["Status"]?.ToString() ?? "Unknown",
                            ReviewerID = reader["ReviewerID"] != DBNull.Value ? reader["ReviewerID"].ToString() : null,
                            ReviewDate = reader["ReviewDate"] != DBNull.Value ? Convert.ToDateTime(reader["ReviewDate"]) : (DateTime?)null,
                            ApprovalDate = reader["ApprovalDate"] != DBNull.Value ? Convert.ToDateTime(reader["ApprovalDate"]) : (DateTime?)null,
                            ExpirationDate = reader["ExpirationDate"] != DBNull.Value ? Convert.ToDateTime(reader["ExpirationDate"]) : (DateTime?)null,
                            Comments = reader["Comments"]?.ToString(),
                            ContactPerson = reader["ContactPerson"]?.ToString(),
                            // Set status class based on status value
                            StatusClass = GetStatusClassFromStatus(reader["Status"]?.ToString())
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                // Log the exception but don't throw it - we'll fall back to sample data
                System.Diagnostics.Debug.WriteLine($"Database error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Will attempt to use fallback sample data");
            }

            return null;
        }

        private string GetStatusClassFromStatus(string status)
        {
            if (string.IsNullOrEmpty(status))
                return "secondary";

            switch (status.ToLower())
            {
                case "approved":
                    return "success";
                case "pending":
                    return "warning";
                case "rejected":
                    return "danger";
                case "in review":
                    return "info";
                default:
                    return "secondary";
            }
        }

        private ApplicationDetails GetSampleApplication(int id)
        {
            // Sample data for demonstration purposes
            var sampleApplications = new List<ApplicationDetails>
            {
                new ApplicationDetails
                {
                    Id = "1",
                    SchoolEmail = "school@example.com",
                    InstitutionName = "Default Institution",
                    Email = "default@example.com",
                    Trade = "General",
                    AssessmentScore = 10,
                    ExperienceScore = 2,
                    CertificationScore = 2,
                    CurriculumScore = 2,
                    FacilitiesScore = 2,
                    PlacementScore = 2,
                    ApplicationDate = DateTime.Parse("2025-04-20 09:50:01.247"),
                    Status = "Pending",
                    Comments = "Your institution may need significant improvements before applying for accreditation.",
                    ContactPerson = "Default Contact",
                    StatusClass = "warning"
                },
                new ApplicationDetails
                {
                    Id = "2",
                    SchoolEmail = "school@example.com",
                    InstitutionName = "Default Institution",
                    Email = "default@example.com",
                    Trade = "General",
                    AssessmentScore = 15,
                    ExperienceScore = 4,
                    CertificationScore = 2,
                    CurriculumScore = 3,
                    FacilitiesScore = 2,
                    PlacementScore = 4,
                    ApplicationDate = DateTime.Parse("2025-04-20 09:52:03.537"),
                    Status = "Pending",
                    Comments = "Your institution meets basic requirements but would benefit from improvements before applying.",
                    ContactPerson = "Default Contact",
                    StatusClass = "warning"
                },
                new ApplicationDetails
                {
                    Id = "3",
                    SchoolEmail = "school@example.com",
                    InstitutionName = "Default Institution",
                    Email = "default@example.com",
                    Trade = "General",
                    AssessmentScore = 13,
                    ExperienceScore = 3,
                    CertificationScore = 4,
                    CurriculumScore = 2,
                    FacilitiesScore = 2,
                    PlacementScore = 2,
                    ApplicationDate = DateTime.Parse("2025-04-20 10:05:42.613"),
                    Status = "Pending",
                    Comments = "Your institution meets basic requirements but would benefit from improvements before applying.",
                    ContactPerson = "Default Contact",
                    StatusClass = "warning"
                },
                new ApplicationDetails
                {
                    Id = "4",
                    SchoolEmail = "collegesaintandre@gmail.com",
                    InstitutionName = "College saint andre",
                    Email = "collegesaintandre@gmail.com",
                    Trade = "General",
                    AssessmentScore = 13,
                    ExperienceScore = 3,
                    CertificationScore = 2,
                    CurriculumScore = 2,
                    FacilitiesScore = 3,
                    PlacementScore = 3,
                    ApplicationDate = DateTime.Parse("2025-04-20 10:09:09.527"),
                    Status = "Pending",
                    Comments = "Your institution meets basic requirements but would benefit from improvements before applying.",
                    ContactPerson = "0786125117",
                    StatusClass = "warning"
                },
                new ApplicationDetails
                {
                    Id = "5",
                    SchoolEmail = "collegesaintandre@gmail.com",
                    InstitutionName = "College saint andre",
                    Email = "collegesaintandre@gmail.com",
                    Trade = "Culinary Arts",
                    AssessmentScore = 15,
                    ExperienceScore = 3,
                    CertificationScore = 3,
                    CurriculumScore = 3,
                    FacilitiesScore = 3,
                    PlacementScore = 3,
                    ApplicationDate = DateTime.Parse("2025-04-20 10:15:06.890"),
                    Status = "Pending",
                    Comments = "Your institution meets basic requirements but would benefit from improvements before applying.",
                    ContactPerson = "0786125117",
                    StatusClass = "warning"
                },
                new ApplicationDetails
                {
                    Id = "6",
                    SchoolEmail = "collegesaintandre@gmail.com",
                    InstitutionName = "College saint andre",
                    Email = "collegesaintandre@gmail.com",
                    Trade = "PCM",
                    AssessmentScore = 15,
                    ExperienceScore = 5,
                    CertificationScore = 3,
                    CurriculumScore = 1,
                    FacilitiesScore = 4,
                    PlacementScore = 2,
                    ApplicationDate = DateTime.Parse("2025-04-20 11:00:04.590"),
                    Status = "Pending",
                    Comments = "Your institution meets basic requirements but would benefit from improvements before applying.",
                    ContactPerson = "0786125117",
                    StatusClass = "warning"
                }
            };

            return sampleApplications.FirstOrDefault(a => a.Id == id.ToString());
        }
    }

    public class ApplicationDetails
    {
        public string Id { get; set; }
        public string SchoolEmail { get; set; }
        public string InstitutionName { get; set; }
        public string Email { get; set; }
        public string Trade { get; set; }
        public int AssessmentScore { get; set; }
        public int ExperienceScore { get; set; }
        public int CertificationScore { get; set; }
        public int CurriculumScore { get; set; }
        public int FacilitiesScore { get; set; }
        public int PlacementScore { get; set; }
        public DateTime ApplicationDate { get; set; }
        public string Status { get; set; }
        public string StatusClass { get; set; } // For styling: success, warning, danger, etc.
        public string ReviewerID { get; set; }
        public DateTime? ReviewDate { get; set; }
        public DateTime? ApprovalDate { get; set; }
        public DateTime? ExpirationDate { get; set; }
        public string Comments { get; set; }
        public string ContactPerson { get; set; }
    }
}
