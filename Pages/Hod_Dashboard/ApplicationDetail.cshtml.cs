using System.Data;
using AccreditationSystem.Pages.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;

namespace AccreditationSystem.Pages.Hod_Dashboard
{
    public class ApplicationDetailModel : PageModel
    {
        private readonly IConfiguration _configuration;
        private readonly IEmailService _emailService;

        public Application Application { get; set; }

        [BindProperty]
        public string Feedback { get; set; }

        public ApplicationDetailModel(IConfiguration configuration, IEmailService emailService)
        {
            _configuration = configuration;
            _emailService = emailService;
        }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            try
            {
                if (id <= 0)
                {
                    TempData["ErrorMessage"] = "Invalid application ID";
                    return RedirectToPage("/Hod_Dashboard/HOD_Applications");
                }

                using (SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    await connection.OpenAsync();

                    string query = @"
                        SELECT ID, SchoolEmail, InstitutionName, Email, Trade, Status, ApplicationDate, 
                               ContactPerson, PhoneNumber, Address, Description, ReviewerID, ReviewNotes, 
                               ReviewDate, ApprovalDate, LastUpdatedDate, Comments
                        FROM Applications 
                        WHERE ID = @ApplicationID";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@ApplicationID", id);

                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                Application = new Application
                                {
                                    ID = reader.GetInt32("ID"),
                                    SchoolEmail = reader.GetString("SchoolEmail"),
                                    InstitutionName = reader.GetString("InstitutionName"),
                                    Email = reader.GetString("Email"),
                                    Trade = reader.GetString("Trade"),
                                    Status = reader.GetString("Status"),
                                    ApplicationDate = reader.GetDateTime("ApplicationDate"),
                                    ContactPerson = reader.IsDBNull("ContactPerson") ? null : reader.GetString("ContactPerson"),
                                    PhoneNumber = reader.IsDBNull("PhoneNumber") ? null : reader.GetString("PhoneNumber"),
                                    Address = reader.IsDBNull("Address") ? null : reader.GetString("Address"),
                                    Description = reader.IsDBNull("Description") ? null : reader.GetString("Description"),
                                    ReviewerID = reader.IsDBNull("ReviewerID") ? (int?)null : reader.GetInt32("ReviewerID"),
                                    ReviewNotes = reader.IsDBNull("ReviewNotes") ? null : reader.GetString("ReviewNotes"),
                                    ReviewDate = reader.IsDBNull("ReviewDate") ? (DateTime?)null : reader.GetDateTime("ReviewDate"),
                                    ApprovalDate = reader.IsDBNull("ApprovalDate") ? (DateTime?)null : reader.GetDateTime("ApprovalDate"),
                                    LastUpdatedDate = reader.IsDBNull("LastUpdatedDate") ? (DateTime?)null : reader.GetDateTime("LastUpdatedDate"),
                                    Comments = reader.IsDBNull("Comments") ? null : reader.GetString("Comments")
                                };
                            }
                            else
                            {
                                TempData["ErrorMessage"] = "Application not found";
                                return RedirectToPage("/Hod_Dashboard/HOD_Applications");
                            }
                        }
                    }
                }

                return Page();
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error retrieving application details: {ex.Message}";
                return RedirectToPage("/Hod_Dashboard/HOD_Applications");
            }
        }

        // FIXED: Handler for approving the application with feedback
        public async Task<IActionResult> OnPostApproveAsync(int id)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(Feedback))
                {
                    TempData["ErrorMessage"] = "Please provide feedback for your decision";
                    return RedirectToPage("/Hod_Dashboard/ApplicationDetail", new { id });
                }

                string hodName = HttpContext.Session.GetString("UserName") ?? "Accreditation Officer";
                int reviewerId = HttpContext.Session.GetInt32("UserId") ?? 1;

                using (SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    await connection.OpenAsync();

                    // IMPORTANT: Load application details BEFORE updating status
                    await LoadApplicationDetailsAsync(id);

                    string updateQuery = @"
                        UPDATE Applications 
                        SET Status = 'Approved', 
                            ApprovalDate = @ApprovalDate,
                            LastUpdatedDate = @LastUpdatedDate,
                            ReviewerID = @ReviewerID,
                            ReviewNotes = @ReviewNotes
                        WHERE ID = @ApplicationID";

                    using (SqlCommand command = new SqlCommand(updateQuery, connection))
                    {
                        command.Parameters.AddWithValue("@ApplicationID", id);
                        command.Parameters.AddWithValue("@ApprovalDate", DateTime.Now);
                        command.Parameters.AddWithValue("@LastUpdatedDate", DateTime.Now);
                        command.Parameters.AddWithValue("@ReviewerID", reviewerId);
                        command.Parameters.AddWithValue("@ReviewNotes", Feedback);

                        int rowsAffected = await command.ExecuteNonQueryAsync();

                        if (rowsAffected > 0)
                        {
                            try
                            {
                                // Send email notification - Application should be loaded at this point
                                await SendDecisionEmailAsync("Approved", hodName);
                                TempData["SuccessMessage"] = "Application approved successfully. The school has been notified.";
                            }
                            catch (Exception emailEx)
                            {
                                // Still show success but with email error details
                                TempData["SuccessMessage"] = $"Application approved successfully, but email notification failed: {emailEx.Message}";
                                System.Diagnostics.Debug.WriteLine($"Email Error Details: {emailEx}");
                            }
                        }
                        else
                        {
                            TempData["ErrorMessage"] = "Failed to approve application.";
                        }
                    }
                }

                return RedirectToPage("/Hod_Dashboard/ApplicationDetail", new { id });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error approving application: {ex.Message}";
                return RedirectToPage("/Hod_Dashboard/ApplicationDetail", new { id });
            }
        }

        // FIXED: Handler for rejecting the application with feedback
        public async Task<IActionResult> OnPostRejectAsync(int id)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(Feedback))
                {
                    TempData["ErrorMessage"] = "Please provide feedback for your decision";
                    return RedirectToPage("/Hod_Dashboard/ApplicationDetail", new { id });
                }

                string hodName = HttpContext.Session.GetString("UserName") ?? "Accreditation Officer";
                int reviewerId = HttpContext.Session.GetInt32("UserId") ?? 1;

                using (SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    await connection.OpenAsync();

                    // IMPORTANT: Load application details BEFORE updating status
                    await LoadApplicationDetailsAsync(id);

                    string updateQuery = @"
                        UPDATE Applications 
                        SET Status = 'Rejected', 
                            ReviewDate = @ReviewDate,
                            LastUpdatedDate = @LastUpdatedDate,
                            ReviewerID = @ReviewerID,
                            ReviewNotes = @ReviewNotes
                        WHERE ID = @ApplicationID";

                    using (SqlCommand command = new SqlCommand(updateQuery, connection))
                    {
                        command.Parameters.AddWithValue("@ApplicationID", id);
                        command.Parameters.AddWithValue("@ReviewDate", DateTime.Now);
                        command.Parameters.AddWithValue("@LastUpdatedDate", DateTime.Now);
                        command.Parameters.AddWithValue("@ReviewerID", reviewerId);
                        command.Parameters.AddWithValue("@ReviewNotes", Feedback);

                        int rowsAffected = await command.ExecuteNonQueryAsync();

                        if (rowsAffected > 0)
                        {
                            try
                            {
                                // Send email notification - Application should be loaded at this point
                                await SendDecisionEmailAsync("Rejected", hodName);
                                TempData["SuccessMessage"] = "Application rejected successfully. The school has been notified.";
                            }
                            catch (Exception emailEx)
                            {
                                // Still show success but with email error details
                                TempData["SuccessMessage"] = $"Application rejected successfully, but email notification failed: {emailEx.Message}";
                                System.Diagnostics.Debug.WriteLine($"Email Error Details: {emailEx}");
                            }
                        }
                        else
                        {
                            TempData["ErrorMessage"] = "Failed to reject application.";
                        }
                    }
                }

                return RedirectToPage("/Hod_Dashboard/ApplicationDetail", new { id });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error rejecting application: {ex.Message}";
                return RedirectToPage("/Hod_Dashboard/ApplicationDetail", new { id });
            }
        }

        // FIXED: LoadApplicationDetailsAsync method
        private async Task LoadApplicationDetailsAsync(int applicationId)
        {
            using (SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                await connection.OpenAsync();

                string query = @"
                    SELECT ID, SchoolEmail, InstitutionName, Email, Trade, Status, ApplicationDate, ContactPerson
                    FROM Applications 
                    WHERE ID = @ApplicationID";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@ApplicationID", applicationId);

                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            Application = new Application
                            {
                                ID = reader.GetInt32("ID"),
                                SchoolEmail = reader.GetString("SchoolEmail"),
                                InstitutionName = reader.GetString("InstitutionName"),
                                Email = reader.GetString("Email"),
                                Trade = reader.GetString("Trade"),
                                Status = reader.GetString("Status"),
                                ApplicationDate = reader.GetDateTime("ApplicationDate"),
                                ContactPerson = reader.IsDBNull("ContactPerson") ? null : reader.GetString("ContactPerson")
                            };
                        }
                        else
                        {
                            throw new Exception($"Application with ID {applicationId} not found.");
                        }
                    }
                }
            }
        }

        // FIXED: SendDecisionEmailAsync with debugging
        private async Task SendDecisionEmailAsync(string decision, string decisionMaker)
        {
            try
            {
                // Debug: Check if Application object is loaded
                if (Application == null)
                {
                    throw new Exception("Application object is null - cannot send email");
                }

                // Debug: Check email addresses
                if (string.IsNullOrEmpty(Application.Email) && string.IsNullOrEmpty(Application.SchoolEmail))
                {
                    throw new Exception("No email addresses found for this application");
                }

                string subject = decision == "Approved"
                    ? "Congratulations! Your Application Has Been Approved"
                    : "Important Update on Your Application";

                string emailBody = BuildDecisionEmailBody(decision, decisionMaker);

                List<string> emailsSent = new List<string>();
                List<string> emailsToSend = new List<string>();

                // Collect valid email addresses
                if (!string.IsNullOrEmpty(Application.Email))
                    emailsToSend.Add(Application.Email);

                if (!string.IsNullOrEmpty(Application.SchoolEmail) && Application.SchoolEmail != Application.Email)
                    emailsToSend.Add(Application.SchoolEmail);

                // Debug: Log what emails we're trying to send to
                System.Diagnostics.Debug.WriteLine($"Attempting to send emails to: {string.Join(", ", emailsToSend)}");

                // Send emails
                foreach (string email in emailsToSend)
                {
                    if (!emailsSent.Contains(email.ToLower()))
                    {
                        await _emailService.SendEmailAsync(email, subject, emailBody);
                        emailsSent.Add(email.ToLower());
                        System.Diagnostics.Debug.WriteLine($"Email sent successfully to: {email}");
                    }
                }

                if (emailsSent.Count == 0)
                {
                    throw new Exception("No emails were sent - check email addresses");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Email sending failed: {ex.Message}");
                throw;
            }
        }

        private string BuildDecisionEmailBody(string decision, string decisionMaker)
        {
            string greeting = decision == "Approved" ? "Congratulations!" : "Important Update";
            string statusColor = decision == "Approved" ? "#28a745" : "#dc3545";
            string statusText = decision == "Approved" ? "APPROVED" : "NOT APPROVED";

            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>Application Decision</title>
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
            <h1>Application Decision</h1>
        </div>
        <div class='content'>
            <p>Dear {Application.ContactPerson ?? "Institution Representative"},</p>
            
            <p>{greeting} We have completed the review of your application for {Application.Trade} program.</p>
            
            <p>Your application has been: <span class='status'>{statusText}</span></p>
            
            <div class='details'>
                <p><strong>Application ID:</strong> {Application.ID}</p>
                <p><strong>Institution:</strong> {Application.InstitutionName}</p>
                <p><strong>Trade/Program:</strong> {Application.Trade}</p>
                <p><strong>Application Date:</strong> {Application.ApplicationDate.ToShortDateString()}</p>
                <p><strong>Decision Date:</strong> {DateTime.Now.ToString("MMMM dd, yyyy")}</p>
            </div>
            
            <h3>Feedback from the Review Committee:</h3>
            <div class='feedback'>
                <p>{Feedback}</p>
            </div>
            
            <p>If you have any questions, please contact our support team at 
            <a href='mailto:accreditation@example.com'>accreditation@example.com</a></p>
            
            <p>Sincerely,<br>
            {decisionMaker}<br>
            Application Review Committee</p>
        </div>
        <div class='footer'>
            <p>&copy; 2025 Accreditation System. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";
        }
    }

    public class Application
    {
        public int ID { get; set; }
        public string SchoolEmail { get; set; }
        public string InstitutionName { get; set; }
        public string Email { get; set; }
        public string Trade { get; set; }
        public string Status { get; set; }
        public DateTime ApplicationDate { get; set; }
        public string ContactPerson { get; set; }
        public string PhoneNumber { get; set; }
        public string Address { get; set; }
        public string Description { get; set; }
        public int? ReviewerID { get; set; }
        public string ReviewNotes { get; set; }
        public DateTime? ReviewDate { get; set; }
        public DateTime? ApprovalDate { get; set; }
        public DateTime? LastUpdatedDate { get; set; }
        public string Comments { get; set; }
    }
}

