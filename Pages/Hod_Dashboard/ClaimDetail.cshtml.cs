using AccreditationSystem.Pages.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;

namespace AccreditationSystem.Pages.Hod_Dashboard
{


    public class ClaimDetailModel : PageModel
    {
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _environment;
        private readonly IEmailService _emailService;

        // Property to hold the accreditation claim details
        public AccreditationClaim Claim { get; set; }

        [BindProperty]
        public string Feedback { get; set; }

        public ClaimDetailModel(IConfiguration configuration, IWebHostEnvironment environment, IEmailService emailService)
        {
            _configuration = configuration;
            _environment = environment;
            _emailService = emailService;
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
                                    // Handle the possible mismatch between string and int for ReviewerID
                                    ReviewerID = reader.IsDBNull(reader.GetOrdinal("ReviewerID")) ? null :
                                               (reader.GetFieldType(reader.GetOrdinal("ReviewerID")) == typeof(int) ?
                                                reader.GetInt32(reader.GetOrdinal("ReviewerID")).ToString() :
                                                reader.GetString(reader.GetOrdinal("ReviewerID"))),
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

        // Handler for approving the claim with feedback
        public async Task<IActionResult> OnPostApproveAsync(int id)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(Feedback))
                {
                    TempData["ErrorMessage"] = "Please provide feedback for your decision";
                    return RedirectToPage("/Hod_Dashboard/ClaimDetail", new { id });
                }

                string hodName = HttpContext.Session.GetString("UserName") ?? "Accreditation Officer";
                int reviewerId = HttpContext.Session.GetInt32("UserId") ?? 1;

                using (SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    await connection.OpenAsync();

                    // First, load the claim details to have information for the email
                    await LoadClaimDetailsAsync(id);

                    // Update the claim status
                    string updateQuery = @"
                        UPDATE AccreditationClaims 
                        SET Status = 'Approved', 
                            ApprovalDate = @ApprovalDate,
                            LastUpdatedDate = @LastUpdatedDate,
                            ReviewerID = @ReviewerID,
                            ReviewNotes = @ReviewNotes
                        WHERE ClaimID = @ClaimID";

                    using (SqlCommand command = new SqlCommand(updateQuery, connection))
                    {
                        command.Parameters.AddWithValue("@ClaimID", id);
                        command.Parameters.AddWithValue("@ApprovalDate", DateTime.Now);
                        command.Parameters.AddWithValue("@LastUpdatedDate", DateTime.Now);
                        command.Parameters.AddWithValue("@ReviewerID", reviewerId);
                        command.Parameters.AddWithValue("@ReviewNotes", Feedback);

                        int rowsAffected = await command.ExecuteNonQueryAsync();

                        if (rowsAffected > 0)
                        {
                            // Send email notification
                            await SendDecisionEmailAsync("Approved", hodName);

                            // Log the notification in database if needed
                            await LogNotificationAsync(id, "Approved");

                            TempData["SuccessMessage"] = "Claim approved successfully. The school has been notified.";
                        }
                        else
                        {
                            TempData["ErrorMessage"] = "Failed to approve claim. Claim not found or already processed.";
                        }
                    }
                }

                return RedirectToPage("/Hod_Dashboard/ClaimDetail", new { id });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error approving claim: {ex.Message}";
                return RedirectToPage("/Hod_Dashboard/ClaimDetail", new { id });
            }
        }

        // Handler for rejecting the claim with feedback
        public async Task<IActionResult> OnPostRejectAsync(int id)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(Feedback))
                {
                    TempData["ErrorMessage"] = "Please provide feedback for your decision";
                    return RedirectToPage("/Hod_Dashboard/ClaimDetail", new { id });
                }

                string hodName = HttpContext.Session.GetString("UserName") ?? "Accreditation Officer";
                int reviewerId = HttpContext.Session.GetInt32("UserId") ?? 1;

                using (SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    await connection.OpenAsync();

                    // First, load the claim details to have information for the email
                    await LoadClaimDetailsAsync(id);

                    string updateQuery = @"
                        UPDATE AccreditationClaims 
                        SET Status = 'Rejected', 
                            ReviewDate = @ReviewDate,
                            LastUpdatedDate = @LastUpdatedDate,
                            ReviewerID = @ReviewerID,
                            ReviewNotes = @ReviewNotes
                        WHERE ClaimID = @ClaimID";

                    using (SqlCommand command = new SqlCommand(updateQuery, connection))
                    {
                        command.Parameters.AddWithValue("@ClaimID", id);
                        command.Parameters.AddWithValue("@ReviewDate", DateTime.Now);
                        command.Parameters.AddWithValue("@LastUpdatedDate", DateTime.Now);
                        command.Parameters.AddWithValue("@ReviewerID", reviewerId);
                        command.Parameters.AddWithValue("@ReviewNotes", Feedback);

                        int rowsAffected = await command.ExecuteNonQueryAsync();

                        if (rowsAffected > 0)
                        {
                            // Send email notification
                            await SendDecisionEmailAsync("Rejected", hodName);

                            // Log the notification in database if needed
                            await LogNotificationAsync(id, "Rejected");

                            TempData["SuccessMessage"] = "Claim rejected successfully. The school has been notified.";
                        }
                        else
                        {
                            TempData["ErrorMessage"] = "Failed to reject claim. Claim not found or already processed.";
                        }
                    }
                }

                return RedirectToPage("/Hod_Dashboard/ClaimDetail", new { id });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error rejecting claim: {ex.Message}";
                return RedirectToPage("/Hod_Dashboard/ClaimDetail", new { id });
            }
        }

        private async Task LoadClaimDetailsAsync(int claimId)
        {
            using (SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                await connection.OpenAsync();

                string query = "SELECT * FROM AccreditationClaims WHERE ClaimID = @ClaimID";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@ClaimID", claimId);

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
                                ContactName = reader.IsDBNull(reader.GetOrdinal("ContactName")) ? null : reader.GetString(reader.GetOrdinal("ContactName")),
                                ContactPosition = reader.IsDBNull(reader.GetOrdinal("ContactPosition")) ? null : reader.GetString(reader.GetOrdinal("ContactPosition")),
                                ContactEmail = reader.IsDBNull(reader.GetOrdinal("ContactEmail")) ? null : reader.GetString(reader.GetOrdinal("ContactEmail")),
                                ContactPhone = reader.IsDBNull(reader.GetOrdinal("ContactPhone")) ? null : reader.GetString(reader.GetOrdinal("ContactPhone")),
                                AdditionalComments = reader.IsDBNull(reader.GetOrdinal("AdditionalComments")) ? null : reader.GetString(reader.GetOrdinal("AdditionalComments"))
                            };
                        }
                        else
                        {
                            throw new Exception($"Claim with ID {claimId} not found.");
                        }
                    }
                }
            }
        }

        private async Task SendDecisionEmailAsync(string decision, string decisionMaker)
        {
            // Email subject based on decision
            string subject = decision == "Approved"
                ? "Congratulations! Your Accreditation Claim Has Been Approved"
                : "Important Update on Your Accreditation Claim";

            // Build the email body
            string emailBody = BuildDecisionEmailBody(decision, decisionMaker);

            // List to keep track of sent emails to avoid duplicates
            List<string> emailsSent = new List<string>();

            // Send to school contact email if available
            if (!string.IsNullOrEmpty(Claim.ContactEmail) && !emailsSent.Contains(Claim.ContactEmail.ToLower()))
            {
                await _emailService.SendEmailAsync(Claim.ContactEmail, subject, emailBody);
                emailsSent.Add(Claim.ContactEmail.ToLower());
            }

            // Also send to school email if different from contact email
            if (!string.IsNullOrEmpty(Claim.SchoolEmail) && !emailsSent.Contains(Claim.SchoolEmail.ToLower()))
            {
                await _emailService.SendEmailAsync(Claim.SchoolEmail, subject, emailBody);
                emailsSent.Add(Claim.SchoolEmail.ToLower());
            }
        }

        private string BuildDecisionEmailBody(string decision, string decisionMaker)
        {
            string greeting = decision == "Approved" ? "Congratulations!" : "Important Update";
            string statusColor = decision == "Approved" ? "#28a745" : "#dc3545";
            string statusText = decision == "Approved" ? "APPROVED" : "NOT APPROVED";

            string emailBody = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>Accreditation Claim Decision</title>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; }}
        .header {{ background-color: #0369a1; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; }}
        .footer {{ background-color: #f5f5f5; padding: 15px; text-align: center; font-size: 0.8em; }}
        .status {{ display: inline-block; padding: 5px 10px; font-weight: bold; color: white; background-color: {statusColor}; }}
        .details {{ margin: 20px 0; padding: 15px; background-color: #f9f9f9; border-left: 4px solid #0369a1; }}
        .feedback {{ margin: 20px 0; padding: 15px; background-color: #f9f9f9; border: 1px solid #ddd; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>Accreditation Claim Decision</h1>
        </div>
        <div class='content'>
            <p>Dear {Claim.ContactName ?? "School Representative"},</p>
            
            <p>{greeting} We have completed the review of your accreditation claim for {Claim.AccreditationType} accreditation.</p>
            
            <p>Your claim has been: <span class='status'>{statusText}</span></p>
            
            <div class='details'>
                <p><strong>Claim ID:</strong> {Claim.ClaimID}</p>
                <p><strong>Accreditation Type:</strong> {Claim.AccreditationType}</p>
                <p><strong>Accreditation Level:</strong> {Claim.AccreditationLevel}</p>
                <p><strong>Requested Period:</strong> {Claim.StartDate.ToShortDateString()} to {Claim.EndDate.ToShortDateString()}</p>
                <p><strong>Decision Date:</strong> {DateTime.Now.ToString("MMMM dd, yyyy")}</p>
            </div>
            
            <h3>Feedback from the Accreditation Committee:</h3>
            <div class='feedback'>
                <p>{Feedback}</p>
            </div>
";

            // Additional content for approved claims
            if (decision == "Approved")
            {
                emailBody += @"
                <p>We are pleased to inform you that your institution has met our accreditation standards. 
                You will receive your official accreditation certificate within the next 14 business days.</p>
                
                <p>Please note that to maintain your accreditation status, you must:</p>
                <ul>
                    <li>Submit annual reports by the end of each academic year</li>
                    <li>Notify us of any significant changes to curriculum or facilities</li>
                    <li>Participate in periodic review processes</li>
                </ul>
";
            }
            // Additional content for rejected claims
            else
            {
                emailBody += @"
                <p>While your claim has not been approved at this time, we encourage you to address the feedback 
                provided above and resubmit your application. Our team is available to provide guidance 
                on how to meet the necessary requirements.</p>
                
                <p>You may resubmit your application after 90 days from the date of this notification.</p>
";
            }

            // Common closing for both types
            emailBody += $@"
            <p>If you have any questions regarding this decision, please contact our accreditation 
            support team at <a href='mailto:accreditation@example.com'>accreditation@example.com</a> 
            or call +250 788 123 456.</p>
            
            <p>Thank you for your commitment to educational excellence.</p>
            
            <p>Sincerely,<br>
            {decisionMaker}<br>
            Accreditation Committee</p>
        </div>
        <div class='footer'>
            <p>This is an automated email. Please do not reply to this message.</p>
            <p>&copy; 2025 Accreditation System. All rights reserved.</p>
        </div>
    </div>
</body>
</html>
";

            return emailBody;
        }

        private async Task LogNotificationAsync(int claimId, string decision)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    await connection.OpenAsync();

                    // Create NotificationsLog table if it doesn't exist
                    string createTableQuery = @"
                    IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[NotificationsLog]') AND type in (N'U'))
                    BEGIN
                        CREATE TABLE [dbo].[NotificationsLog](
                            [NotificationID] [int] IDENTITY(1,1) NOT NULL,
                            [RecipientEmail] [nvarchar](255) NOT NULL,
                            [Subject] [nvarchar](255) NOT NULL,
                            [NotificationType] [nvarchar](50) NOT NULL,
                            [RelatedEntityType] [nvarchar](50) NOT NULL,
                            [RelatedEntityID] [int] NOT NULL,
                            [SentDate] [datetime] NOT NULL DEFAULT (getdate()),
                            [Status] [nvarchar](50) NOT NULL,
                            [ErrorMessage] [nvarchar](max) NULL,
                            CONSTRAINT [PK_NotificationsLog] PRIMARY KEY CLUSTERED 
                            (
                                [NotificationID] ASC
                            )
                        )
                    END";

                    using (SqlCommand createCommand = new SqlCommand(createTableQuery, connection))
                    {
                        await createCommand.ExecuteNonQueryAsync();
                    }

                    // Log the notification
                    string insertQuery = @"
                        INSERT INTO NotificationsLog 
                        (RecipientEmail, Subject, NotificationType, RelatedEntityType, RelatedEntityID, SentDate, Status)
                        VALUES 
                        (@RecipientEmail, @Subject, @NotificationType, @RelatedEntityType, @RelatedEntityID, @SentDate, @Status)";

                    using (SqlCommand command = new SqlCommand(insertQuery, connection))
                    {
                        // Log for school email
                        command.Parameters.Clear();
                        command.Parameters.AddWithValue("@RecipientEmail", Claim.SchoolEmail);
                        command.Parameters.AddWithValue("@Subject", decision == "Approved" ? "Accreditation Claim Approved" : "Accreditation Claim Not Approved");
                        command.Parameters.AddWithValue("@NotificationType", "Email");
                        command.Parameters.AddWithValue("@RelatedEntityType", "AccreditationClaim");
                        command.Parameters.AddWithValue("@RelatedEntityID", claimId);
                        command.Parameters.AddWithValue("@SentDate", DateTime.Now);
                        command.Parameters.AddWithValue("@Status", "Sent");

                        await command.ExecuteNonQueryAsync();

                        // Log for contact email if different
                        if (!string.IsNullOrEmpty(Claim.ContactEmail) && Claim.ContactEmail != Claim.SchoolEmail)
                        {
                            command.Parameters.Clear();
                            command.Parameters.AddWithValue("@RecipientEmail", Claim.ContactEmail);
                            command.Parameters.AddWithValue("@Subject", decision == "Approved" ? "Accreditation Claim Approved" : "Accreditation Claim Not Approved");
                            command.Parameters.AddWithValue("@NotificationType", "Email");
                            command.Parameters.AddWithValue("@RelatedEntityType", "AccreditationClaim");
                            command.Parameters.AddWithValue("@RelatedEntityID", claimId);
                            command.Parameters.AddWithValue("@SentDate", DateTime.Now);
                            command.Parameters.AddWithValue("@Status", "Sent");

                            await command.ExecuteNonQueryAsync();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Just log the error but don't throw - we don't want to fail if logging fails
                System.Diagnostics.Debug.WriteLine($"Error logging notification: {ex.Message}");
            }
        }
    }

    // We're using your existing AccreditationClaim class, keeping it here for completeness
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