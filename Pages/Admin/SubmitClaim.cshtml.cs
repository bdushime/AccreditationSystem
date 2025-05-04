using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using System.Text.Json;

namespace AccreditationSystem.Pages.Admin
{
    public class SubmitClaimModel : PageModel
    {
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _environment;

        public SubmitClaimModel(IConfiguration configuration, IWebHostEnvironment environment)
        {
            _configuration = configuration;
            _environment = environment;
        }

        [BindProperty]
        public string Email { get; set; }

        [BindProperty]
        public string AccreditationType { get; set; }

        [BindProperty]
        public string AccreditationLevel { get; set; }

        [BindProperty]
        public string PreviousStatus { get; set; }

        [BindProperty]
        public DateTime StartDate { get; set; }

        [BindProperty]
        public DateTime EndDate { get; set; }

        [BindProperty]
        public IFormFile SelfAssessmentReport { get; set; }

        [BindProperty]
        public IFormFile CurriculumDocumentation { get; set; }

        [BindProperty]
        public IFormFile FacultyCredentials { get; set; }

        [BindProperty]
        public IFormFile[] AdditionalDocumentation { get; set; }

        [BindProperty]
        public string AcademicStandards { get; set; }

        [BindProperty]
        public string FacultyStandards { get; set; }

        [BindProperty]
        public string FacilityStandards { get; set; }

        [BindProperty]
        public string StudentServices { get; set; }

        [BindProperty]
        public string ContactName { get; set; }

        [BindProperty]
        public string ContactPosition { get; set; }

        [BindProperty]
        public string ContactEmail { get; set; }

        [BindProperty]
        public string ContactPhone { get; set; }

        [BindProperty]
        public string AdditionalComments { get; set; }

        [BindProperty]
        public bool Certify { get; set; }

        [BindProperty]
        public bool FeeAcknowledge { get; set; }

        public void OnGet()
        {
            // Initialize any needed data for the form
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // Track validation errors
            List<string> validationErrors = new List<string>();

            // Validate model state
            if (!ModelState.IsValid)
            {
                foreach (var state in ModelState)
                {
                    foreach (var error in state.Value.Errors)
                    {
                        validationErrors.Add($"{state.Key}: {error.ErrorMessage}");
                    }
                }

                // Log specific errors
                string errorDetails = string.Join(", ", validationErrors);
                TempData["ErrorMessage"] = $"Please correct the following errors: {errorDetails}";
                return Page();
            }

            // Additional date validation
            if (EndDate <= StartDate)
            {
                ModelState.AddModelError("EndDate", "End date must be after start date");
                TempData["ErrorMessage"] = "End date must be after start date.";
                return Page();
            }

            try
            {
                // File validation with better error messages
                if (SelfAssessmentReport == null || SelfAssessmentReport.Length == 0)
                {
                    TempData["ErrorMessage"] = "Self-assessment report is required.";
                    return Page();
                }

                // 1. Upload files and get file paths
                string selfAssessmentPath = await UploadFile(SelfAssessmentReport, "self-assessment");
                string curriculumPath = await UploadFile(CurriculumDocumentation, "curriculum");
                string facultyCredentialsPath = await UploadFile(FacultyCredentials, "faculty");

                // Handle additional documentation (can be multiple files)
                List<string> additionalDocPaths = new List<string>();
                if (AdditionalDocumentation != null)
                {
                    foreach (var doc in AdditionalDocumentation)
                    {
                        if (doc != null && doc.Length > 0)
                        {
                            string path = await UploadFile(doc, "additional");
                            additionalDocPaths.Add(path);
                        }
                    }
                }
                string additionalDocumentationPath = string.Join(";", additionalDocPaths);

                // 2. Save to database
                using (SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    await connection.OpenAsync();

                    string sql = @"
                        INSERT INTO AccreditationClaims (
                            SchoolEmail, AccreditationType, AccreditationLevel, PreviousStatus, 
                            StartDate, EndDate, SelfAssessmentReportPath, CurriculumDocumentationPath, 
                            FacultyCredentialsPath, AdditionalDocumentationPath, AcademicStandards, 
                            FacultyStandards, FacilityStandards, StudentServices, ContactName, 
                            ContactPosition, ContactEmail, ContactPhone, AdditionalComments, Status
                        ) VALUES (
                            @SchoolEmail, @AccreditationType, @AccreditationLevel, @PreviousStatus, 
                            @StartDate, @EndDate, @SelfAssessmentReportPath, @CurriculumDocumentationPath, 
                            @FacultyCredentialsPath, @AdditionalDocumentationPath, @AcademicStandards, 
                            @FacultyStandards, @FacilityStandards, @StudentServices, @ContactName, 
                            @ContactPosition, @ContactEmail, @ContactPhone, @AdditionalComments, 'Submitted'
                        );
                        SELECT SCOPE_IDENTITY();";

                    using (SqlCommand command = new SqlCommand(sql, connection))
                    {
                        // Add parameters to prevent SQL injection
                        command.Parameters.AddWithValue("@SchoolEmail", Email);
                        command.Parameters.AddWithValue("@AccreditationType", AccreditationType);
                        command.Parameters.AddWithValue("@AccreditationLevel", AccreditationLevel);
                        command.Parameters.AddWithValue("@PreviousStatus", (object)PreviousStatus ?? DBNull.Value);
                        command.Parameters.AddWithValue("@StartDate", StartDate);
                        command.Parameters.AddWithValue("@EndDate", EndDate);
                        command.Parameters.AddWithValue("@SelfAssessmentReportPath", selfAssessmentPath);
                        command.Parameters.AddWithValue("@CurriculumDocumentationPath", curriculumPath);
                        command.Parameters.AddWithValue("@FacultyCredentialsPath", facultyCredentialsPath);
                        command.Parameters.AddWithValue("@AdditionalDocumentationPath", (object)additionalDocumentationPath ?? DBNull.Value);
                        command.Parameters.AddWithValue("@AcademicStandards", AcademicStandards);
                        command.Parameters.AddWithValue("@FacultyStandards", FacultyStandards);
                        command.Parameters.AddWithValue("@FacilityStandards", FacilityStandards);
                        command.Parameters.AddWithValue("@StudentServices", StudentServices);
                        command.Parameters.AddWithValue("@ContactName", ContactName);
                        command.Parameters.AddWithValue("@ContactPosition", ContactPosition);
                        command.Parameters.AddWithValue("@ContactEmail", ContactEmail);
                        command.Parameters.AddWithValue("@ContactPhone", ContactPhone);
                        command.Parameters.AddWithValue("@AdditionalComments", (object)AdditionalComments ?? DBNull.Value);


                        // Execute the query and get the new claim ID
                        var result = await command.ExecuteScalarAsync();
                        int claimId = Convert.ToInt32(result);

                        // Send notification email or other post-processing here

                        TempData["SuccessMessage"] = $"Your accreditation claim has been submitted successfully. Claim ID: {claimId}";
                        return RedirectToPage("/Index");
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An error occurred while submitting your claim: {ex.Message}";
                return Page();
            }
        }

        private async Task<string> UploadFile(IFormFile file, string prefix)
        {
            if (file == null || file.Length == 0)
            {
                throw new Exception("No file was uploaded.");
            }

            // Generate a unique file name
            string uniqueFileName = $"{prefix}-{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";

            // Create the uploads directory if it doesn't exist
            string uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "accreditation-claims");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            string filePath = Path.Combine(uploadsFolder, uniqueFileName);

            // Save the file
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }

            // Return the relative path for storage in the database
            return $"/uploads/accreditation-claims/{uniqueFileName}";
        }
    }
}