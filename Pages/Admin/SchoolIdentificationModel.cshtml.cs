using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;

namespace AccreditationSystem.Pages.Admin
{
    public class SchoolIdentificationModel : PageModel
    {
        private readonly IConfiguration _configuration;

        [BindProperty]
        public SchoolIdentificationInput FormInput { get; set; }

        public SchoolIdentificationResult Result { get; set; }

        public SchoolIdentificationModel(IConfiguration configuration)
        {
            _configuration = configuration;
            FormInput = new SchoolIdentificationInput();
            Result = new SchoolIdentificationResult();
        }

        public void OnGet()
        {
            // Get the combination from the query string
            if (!string.IsNullOrEmpty(Request.Query["combination"]))
            {
                FormInput.Combination = Request.Query["combination"].ToString();
            }
            else
            {
                FormInput.Combination = "General";
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            try
            {
                // Calculate the total score
                int totalScore = CalculateScore();

                // Save to database
                await SaveToDatabase(totalScore);

                // Set success message
                TempData["SuccessMessage"] = "Your school identification assessment has been submitted successfully.";
                TempData["Score"] = totalScore;

                // Redirect to result page or show modal on the same page
                return Page();
            }
            catch (Exception ex)
            {
                // Log the error
                TempData["ErrorMessage"] = "An error occurred while saving your assessment: " + ex.Message;
                return Page();
            }
        }

        private int CalculateScore()
        {
            // Calculate the total score based on all question responses
            int score = 0;

            score += FormInput.YearsOffered;
            score += FormInput.StaffQualifications;
            score += FormInput.Facilities;
            score += FormInput.StudentPerformance;
            score += FormInput.CurriculumUpdates;
            score += FormInput.HigherEducation;
            score += FormInput.Extracurricular;
            score += FormInput.Collaborations;
            score += FormInput.AssessmentMethods;
            score += FormInput.DigitalResources;

            return score;
        }

        private async Task SaveToDatabase(int totalScore)
        {
            using (SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                await connection.OpenAsync();

                string query = @"INSERT INTO SchoolIdentifications 
                    (SchoolName, SchoolEmail, ContactPerson, ContactPhone, Combination, 
                    YearsOffered, StaffQualifications, Facilities, StudentPerformance, 
                    CurriculumUpdates, HigherEducation, Extracurricular, Collaborations,
                    AssessmentMethods, DigitalResources, TotalScore, SubmissionDate)
                    VALUES 
                    (@SchoolName, @SchoolEmail, @ContactPerson, @ContactPhone, @Combination,
                    @YearsOffered, @StaffQualifications, @Facilities, @StudentPerformance,
                    @CurriculumUpdates, @HigherEducation, @Extracurricular, @Collaborations,
                    @AssessmentMethods, @DigitalResources, @TotalScore, @SubmissionDate)";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    // Add parameters
                    command.Parameters.AddWithValue("@SchoolName", FormInput.SchoolName);
                    command.Parameters.AddWithValue("@SchoolEmail", FormInput.SchoolEmail);
                    command.Parameters.AddWithValue("@ContactPerson", FormInput.ContactPerson);
                    command.Parameters.AddWithValue("@ContactPhone", FormInput.ContactPhone);
                    command.Parameters.AddWithValue("@Combination", FormInput.Combination);
                    command.Parameters.AddWithValue("@YearsOffered", FormInput.YearsOffered);
                    command.Parameters.AddWithValue("@StaffQualifications", FormInput.StaffQualifications);
                    command.Parameters.AddWithValue("@Facilities", FormInput.Facilities);
                    command.Parameters.AddWithValue("@StudentPerformance", FormInput.StudentPerformance);
                    command.Parameters.AddWithValue("@CurriculumUpdates", FormInput.CurriculumUpdates);
                    command.Parameters.AddWithValue("@HigherEducation", FormInput.HigherEducation);
                    command.Parameters.AddWithValue("@Extracurricular", FormInput.Extracurricular);
                    command.Parameters.AddWithValue("@Collaborations", FormInput.Collaborations);
                    command.Parameters.AddWithValue("@AssessmentMethods", FormInput.AssessmentMethods);
                    command.Parameters.AddWithValue("@DigitalResources", FormInput.DigitalResources);
                    command.Parameters.AddWithValue("@TotalScore", totalScore);
                    command.Parameters.AddWithValue("@SubmissionDate", DateTime.Now);

                    // Execute the query
                    await command.ExecuteNonQueryAsync();
                }
            }
        }
    }

    public class SchoolIdentificationInput
    {
        [Required]
        public string Combination { get; set; }

        [Required]
        [Range(1, 5, ErrorMessage = "Please answer this question.")]
        public int YearsOffered { get; set; }

        [Required]
        [Range(1, 5, ErrorMessage = "Please answer this question.")]
        public int StaffQualifications { get; set; }

        [Required]
        [Range(1, 5, ErrorMessage = "Please answer this question.")]
        public int Facilities { get; set; }

        [Required]
        [Range(1, 5, ErrorMessage = "Please answer this question.")]
        public int StudentPerformance { get; set; }

        [Required]
        [Range(1, 5, ErrorMessage = "Please answer this question.")]
        public int CurriculumUpdates { get; set; }

        [Required]
        [Range(1, 5, ErrorMessage = "Please answer this question.")]
        public int HigherEducation { get; set; }

        [Required]
        [Range(1, 5, ErrorMessage = "Please answer this question.")]
        public int Extracurricular { get; set; }

        [Required]
        [Range(1, 5, ErrorMessage = "Please answer this question.")]
        public int Collaborations { get; set; }

        [Required]
        [Range(1, 5, ErrorMessage = "Please answer this question.")]
        public int AssessmentMethods { get; set; }

        [Required]
        [Range(1, 5, ErrorMessage = "Please answer this question.")]
        public int DigitalResources { get; set; }

        [Required]
        [StringLength(255, ErrorMessage = "School name cannot exceed 255 characters.")]
        public string SchoolName { get; set; }

        [Required]
        [EmailAddress]
        [StringLength(255, ErrorMessage = "Email cannot exceed 255 characters.")]
        public string SchoolEmail { get; set; }

        [Required]
        [StringLength(255, ErrorMessage = "Contact person name cannot exceed 255 characters.")]
        public string ContactPerson { get; set; }

        [Required]
        [Phone]
        [StringLength(50, ErrorMessage = "Phone number cannot exceed 50 characters.")]
        public string ContactPhone { get; set; }
    }

    public class SchoolIdentificationResult
    {
        public int TotalScore { get; set; }
        public string Feedback { get; set; }
        public List<string> Strengths { get; set; } = new List<string>();
        public List<string> AreasForImprovement { get; set; } = new List<string>();
        public bool IsRecommendedForAccreditation { get; set; }
        public string RecommendationLevel { get; set; }
    }
}