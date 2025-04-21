using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;

namespace AccreditationSystem.Pages.Assessment
{
    public class IndexModel : PageModel
    {
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _environment;

        // Input model for the assessment form
        [BindProperty]
        public AssessmentFormInput FormInput { get; set; }

        // These properties will capture the form fields that aren't part of the AssessmentFormInput
        [BindProperty]
        public string Email { get; set; }

        [BindProperty]
        public string SchoolEmail { get; set; }

        [BindProperty]
        public string InstitutionName { get; set; }

        [BindProperty]
        public string ContactPerson { get; set; }

        // Output model for assessment results
        public AssessmentResult Result { get; set; }

        public IndexModel(IConfiguration configuration, IWebHostEnvironment environment)
        {
            _configuration = configuration;
            _environment = environment;
            FormInput = new AssessmentFormInput();
            Result = new AssessmentResult();
        }

        public void OnGet()
        {
            // Check for trade in query string FIRST (this is the key fix!)
            if (!string.IsNullOrEmpty(Request.Query["trade"]))
            {
                FormInput.Trade = Request.Query["trade"].ToString();
            }
            // Only then check route data as fallback
            else if (RouteData.Values.ContainsKey("trade"))
            {
                FormInput.Trade = RouteData.Values["trade"].ToString();
            }
            else
            {
                FormInput.Trade = "General";
            }

            // Log for debugging
            System.Diagnostics.Debug.WriteLine($"Trade captured: {FormInput.Trade}");
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            try
            {
                // Calculate the assessment score
                Result = CalculateAssessmentScore(FormInput);

                // Save the assessment results to the database
                using (SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    await connection.OpenAsync();

                    string query = @"INSERT INTO Applications 
                        (Email, SchoolEmail, InstitutionName, Trade, AssessmentScore, 
                        ExperienceScore, CertificationScore, CurriculumScore, FacilitiesScore, 
                        PlacementScore, ApplicationDate, Status, Comments, ContactPerson)
                        VALUES 
                        (@Email, @SchoolEmail, @InstitutionName, @Trade, @AssessmentScore, 
                        @ExperienceScore, @CertificationScore, @CurriculumScore, @FacilitiesScore, 
                        @PlacementScore, @ApplicationDate, @Status, @Comments, @ContactPerson);
                        
                        SELECT SCOPE_IDENTITY();"; // Return the ID of the inserted record

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        // Set the parameters with actual form values
                        command.Parameters.AddWithValue("@Email", Email);
                        command.Parameters.AddWithValue("@SchoolEmail", SchoolEmail);
                        command.Parameters.AddWithValue("@InstitutionName", InstitutionName);
                        command.Parameters.AddWithValue("@Trade", FormInput.Trade);
                        command.Parameters.AddWithValue("@AssessmentScore", Result.TotalScore);
                        command.Parameters.AddWithValue("@ExperienceScore", FormInput.Experience);
                        command.Parameters.AddWithValue("@CertificationScore", FormInput.Certification);
                        command.Parameters.AddWithValue("@CurriculumScore", FormInput.Curriculum);
                        command.Parameters.AddWithValue("@FacilitiesScore", FormInput.Facilities);
                        command.Parameters.AddWithValue("@PlacementScore", FormInput.Placement);
                        command.Parameters.AddWithValue("@ApplicationDate", DateTime.Now);
                        command.Parameters.AddWithValue("@Status", "Pending");
                        command.Parameters.AddWithValue("@Comments", Result.Feedback);
                        command.Parameters.AddWithValue("@ContactPerson", ContactPerson);

                        // Execute the INSERT query and get the new ID
                        var result = await command.ExecuteScalarAsync();
                        if (result != null)
                        {
                            // Log success or use the ID if needed
                            TempData["NewApplicationId"] = result.ToString();
                        }
                    }
                }

                // Set success message
                TempData["SuccessMessage"] = "Your assessment has been saved and submitted for accreditation.";
            }
            catch (Exception ex)
            {
                // Log the exception and display an error message
                TempData["ErrorMessage"] = $"Error saving assessment: {ex.Message}";
                return Page();
            }

            // Return the current page with the results
            return Page();
        }

        private AssessmentResult CalculateAssessmentScore(AssessmentFormInput input)
        {
            var result = new AssessmentResult
            {
                Trade = input.Trade,
                ExperienceScore = input.Experience,
                CertificationScore = input.Certification,
                CurriculumScore = input.Curriculum,
                FacilitiesScore = input.Facilities,
                PlacementScore = input.Placement
            };

            result.TotalScore = input.Experience + input.Certification +
                                input.Curriculum + input.Facilities + input.Placement;

            result.Percentage = (result.TotalScore / 25.0) * 100;

            // Determine accreditation recommendation
            if (result.TotalScore <= 10)
            {
                result.Feedback = "Your institution may need significant improvements before applying for accreditation.";
                result.IsRecommendedForAccreditation = false;
            }
            else if (result.TotalScore <= 15)
            {
                result.Feedback = "Your institution meets basic requirements but would benefit from improvements before applying.";
                result.IsRecommendedForAccreditation = false;
            }
            else if (result.TotalScore <= 20)
            {
                result.Feedback = "Your institution meets most requirements for accreditation. Consider applying while addressing noted areas for improvement.";
                result.IsRecommendedForAccreditation = true;
            }
            else
            {
                result.Feedback = "Your institution meets or exceeds all requirements for accreditation. You are strongly encouraged to apply.";
                result.IsRecommendedForAccreditation = true;
            }

            return result;
        }
    }

    public class AssessmentFormInput
    {
        public string Trade { get; set; }

        [System.ComponentModel.DataAnnotations.Required]
        [System.ComponentModel.DataAnnotations.Range(1, 5, ErrorMessage = "Please answer all questions.")]
        public int Experience { get; set; }

        [System.ComponentModel.DataAnnotations.Required]
        [System.ComponentModel.DataAnnotations.Range(1, 5, ErrorMessage = "Please answer all questions.")]
        public int Certification { get; set; }

        [System.ComponentModel.DataAnnotations.Required]
        [System.ComponentModel.DataAnnotations.Range(1, 5, ErrorMessage = "Please answer all questions.")]
        public int Curriculum { get; set; }

        [System.ComponentModel.DataAnnotations.Required]
        [System.ComponentModel.DataAnnotations.Range(1, 5, ErrorMessage = "Please answer all questions.")]
        public int Facilities { get; set; }

        [System.ComponentModel.DataAnnotations.Required]
        [System.ComponentModel.DataAnnotations.Range(1, 5, ErrorMessage = "Please answer all questions.")]
        public int Placement { get; set; }
    }

    public class AssessmentResult
    {
        public string Trade { get; set; }
        public int ExperienceScore { get; set; }
        public int CertificationScore { get; set; }
        public int CurriculumScore { get; set; }
        public int FacilitiesScore { get; set; }
        public int PlacementScore { get; set; }
        public int TotalScore { get; set; }
        public double Percentage { get; set; }
        public string Feedback { get; set; }
        public bool IsRecommendedForAccreditation { get; set; }
    }
}
