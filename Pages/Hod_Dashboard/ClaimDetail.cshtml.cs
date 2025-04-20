using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading.Tasks;

namespace AccreditationSystem.Pages.Hod_Dashboard
{
    public class ClaimDetailsModel : PageModel
    {
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _environment;

        // Property to hold the accreditation claim details
        public AccreditationClaim Claim { get; set; }

        public ClaimDetailsModel(IConfiguration configuration, IWebHostEnvironment environment)
        {
            _configuration = configuration;
            _environment = environment;
        }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            try
            {
                // Validate the ID parameter
                if (id <= 0)
                {
                    TempData["ErrorMessage"] = "Invalid claim ID";
                    return RedirectToPage("/Hod_Dashboard/HOD_Claims");
                }

                using (SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    await connection.OpenAsync();

                    string query = @"
                        SELECT * FROM AccreditationClaims 
                        WHERE ClaimID = @ClaimID";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@ClaimID", id);

                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                Claim = new AccreditationClaim
                                {
                                    ClaimID = reader.GetInt32(reader.GetOrdinal("ClaimID")),
                                    SchoolEmail = reader.GetString(reader.GetOrdinal("SchoolEmail")),
                                    AccreditationType = reader.GetString(reader.GetOrdinal("AccreditationType")),
                                    AccreditationLevel = reader.GetString(reader.GetOrdinal("AccreditationLevel")),
                                    PreviousStatus = reader.IsDBNull(reader.GetOrdinal("PreviousStatus")) ? null : reader.GetString(reader.GetOrdinal("PreviousStatus")),
                                    StartDate = reader.GetDateTime(reader.GetOrdinal("StartDate")),
                                    EndDate = reader.GetDateTime(reader.GetOrdinal("EndDate")),
                                    Status = reader.GetString(reader.GetOrdinal("Status")),
                                    SubmissionDate = reader.GetDateTime(reader.GetOrdinal("SubmissionDate")),

                                    // Academic Standards
                                    AcademicStandards = reader.IsDBNull(reader.GetOrdinal("AcademicStandards")) ? null : reader.GetString(reader.GetOrdinal("AcademicStandards")),
                                    FacultyStandards = reader.IsDBNull(reader.GetOrdinal("FacultyStandards")) ? null : reader.GetString(reader.GetOrdinal("FacultyStandards")),
                                    FacilityStandards = reader.IsDBNull(reader.GetOrdinal("FacilityStandards")) ? null : reader.GetString(reader.GetOrdinal("FacilityStandards")),
                                    StudentServices = reader.IsDBNull(reader.GetOrdinal("StudentServices")) ? null : reader.GetString(reader.GetOrdinal("StudentServices")),

                                    // Contact Information
                                    ContactName = reader.IsDBNull(reader.GetOrdinal("ContactName")) ? null : reader.GetString(reader.GetOrdinal("ContactName")),
                                    ContactPosition = reader.IsDBNull(reader.GetOrdinal("ContactPosition")) ? null : reader.GetString(reader.GetOrdinal("ContactPosition")),
                                    ContactEmail = reader.IsDBNull(reader.GetOrdinal("ContactEmail")) ? null : reader.GetString(reader.GetOrdinal("ContactEmail")),
                                    ContactPhone = reader.IsDBNull(reader.GetOrdinal("ContactPhone")) ? null : reader.GetString(reader.GetOrdinal("ContactPhone")),

                                    // Documentation Paths
                                    SelfAssessmentReportPath = reader.IsDBNull(reader.GetOrdinal("SelfAssessmentReportPath")) ? null : reader.GetString(reader.GetOrdinal("SelfAssessmentReportPath")),
                                    CurriculumDocumentationPath = reader.IsDBNull(reader.GetOrdinal("CurriculumDocumentationPath")) ? null : reader.GetString(reader.GetOrdinal("CurriculumDocumentationPath")),
                                    FacultyCredentialsPath = reader.IsDBNull(reader.GetOrdinal("FacultyCredentialsPath")) ? null : reader.GetString(reader.GetOrdinal("FacultyCredentialsPath")),
                                    AdditionalDocumentationPath = reader.IsDBNull(reader.GetOrdinal("AdditionalDocumentationPath")) ? null : reader.GetString(reader.GetOrdinal("AdditionalDocumentationPath")),

                                    // Additional Information
                                    AdditionalComments = reader.IsDBNull(reader.GetOrdinal("AdditionalComments")) ? null : reader.GetString(reader.GetOrdinal("AdditionalComments")),

                                    // Review Information
                                    ReviewerID = reader.IsDBNull(reader.GetOrdinal("ReviewerID")) ? null : reader.GetString(reader.GetOrdinal("ReviewerID")),
                                    ReviewNotes = reader.IsDBNull(reader.GetOrdinal("ReviewNotes")) ? null : reader.GetString(reader.GetOrdinal("ReviewNotes")),
                                    ReviewDate = reader.IsDBNull(reader.GetOrdinal("ReviewDate")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("ReviewDate")),
                                    ApprovalDate = reader.IsDBNull(reader.GetOrdinal("ApprovalDate")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("ApprovalDate")),
                                    LastUpdatedDate = reader.IsDBNull(reader.GetOrdinal("LastUpdatedDate")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("LastUpdatedDate"))
                                };
                            }
                            else
                            {
                                // Claim not found
                                TempData["ErrorMessage"] = "Claim not found";
                                return RedirectToPage("/Hod_Dashboard/HOD_Claims");
                            }
                        }
                    }
                }

                return Page();
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error retrieving claim details: {ex.Message}";
                return RedirectToPage("/Hod_Dashboard/HOD_Claims");
            }
        }

        // Handler for approving the claim
        public async Task<IActionResult> OnPostApproveAsync(int id)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    await connection.OpenAsync();

                    string updateQuery = @"
                        UPDATE AccreditationClaims 
                        SET Status = 'Approved', 
                            ApprovalDate = @ApprovalDate,
                            LastUpdatedDate = @LastUpdatedDate,
                            ReviewerID = @ReviewerID
                        WHERE ClaimID = @ClaimID";

                    using (SqlCommand command = new SqlCommand(updateQuery, connection))
                    {
                        command.Parameters.AddWithValue("@ClaimID", id);
                        command.Parameters.AddWithValue("@ApprovalDate", DateTime.Now);
                        command.Parameters.AddWithValue("@LastUpdatedDate", DateTime.Now);
                        command.Parameters.AddWithValue("@ReviewerID", "HOD"); // You might want to get this from user session

                        int rowsAffected = await command.ExecuteNonQueryAsync();

                        if (rowsAffected > 0)
                        {
                            TempData["SuccessMessage"] = "Claim approved successfully";
                        }
                        else
                        {
                            TempData["ErrorMessage"] = "Failed to approve claim. Claim not found or already processed.";
                        }
                    }
                }

                return RedirectToPage("/Hod_Dashboard/ClaimDetails", new { id });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error approving claim: {ex.Message}";
                return RedirectToPage("/Hod_Dashboard/ClaimDetails", new { id });
            }
        }

        // Handler for rejecting the claim
        public async Task<IActionResult> OnPostRejectAsync(int id)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    await connection.OpenAsync();

                    string updateQuery = @"
                        UPDATE AccreditationClaims 
                        SET Status = 'Rejected', 
                            ReviewDate = @ReviewDate,
                            LastUpdatedDate = @LastUpdatedDate,
                            ReviewerID = @ReviewerID
                        WHERE ClaimID = @ClaimID";

                    using (SqlCommand command = new SqlCommand(updateQuery, connection))
                    {
                        command.Parameters.AddWithValue("@ClaimID", id);
                        command.Parameters.AddWithValue("@ReviewDate", DateTime.Now);
                        command.Parameters.AddWithValue("@LastUpdatedDate", DateTime.Now);
                        command.Parameters.AddWithValue("@ReviewerID", "HOD"); // You might want to get this from user session

                        int rowsAffected = await command.ExecuteNonQueryAsync();

                        if (rowsAffected > 0)
                        {
                            TempData["SuccessMessage"] = "Claim rejected successfully";
                        }
                        else
                        {
                            TempData["ErrorMessage"] = "Failed to reject claim. Claim not found or already processed.";
                        }
                    }
                }

                return RedirectToPage("/Hod_Dashboard/ClaimDetails", new { id });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error rejecting claim: {ex.Message}";
                return RedirectToPage("/Hod_Dashboard/ClaimDetails", new { id });
            }
        }
    }

     //AccreditationClaim model class - complete model including all properties
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

        // Academic Standards
        public string AcademicStandards { get; set; }
        public string FacultyStandards { get; set; }
        public string FacilityStandards { get; set; }
        public string StudentServices { get; set; }

        // Contact Information
        public string ContactName { get; set; }
        public string ContactPosition { get; set; }
        public string ContactEmail { get; set; }
        public string ContactPhone { get; set; }

        // Documentation Paths
        public string SelfAssessmentReportPath { get; set; }
        public string CurriculumDocumentationPath { get; set; }
        public string FacultyCredentialsPath { get; set; }
        public string AdditionalDocumentationPath { get; set; }

        // Additional Information
        public string AdditionalComments { get; set; }

        // Review Information
        public string ReviewerID { get; set; }
        public string ReviewNotes { get; set; }
        public DateTime? ReviewDate { get; set; }
        public DateTime? ApprovalDate { get; set; }
        public DateTime? LastUpdatedDate { get; set; }
    }
}