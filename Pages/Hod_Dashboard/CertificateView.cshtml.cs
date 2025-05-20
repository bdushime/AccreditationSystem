using System.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;

namespace AccreditationSystem.Pages.Hod_Dashboard
{
    public class CertificateViewModel : PageModel
    {
        private readonly IConfiguration _configuration;
        public CertificateData Certificate { get; set; }
        public bool CertificateExists { get; set; }

        public CertificateViewModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<IActionResult> OnGetAsync(int applicationId)
        {
            try
            {
                await LoadCertificateDataAsync(applicationId);
                return Page();
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error loading certificate data: {ex.Message}";
                return RedirectToPage("/Hod_Dashboard/HOD_ApprovedSchools");
            }
        }

        public async Task<IActionResult> OnPostGeneratePdfAsync(int applicationId)
        {
            try
            {
                await LoadCertificateDataAsync(applicationId);

                if (!CertificateExists)
                {
                    TempData["ErrorMessage"] = "No certificate found for this application.";
                    return RedirectToPage();
                }

                // In a real implementation, you would generate a PDF here
                // For now, we'll just mark it as downloaded
                using (SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    await connection.OpenAsync();
                    string updateQuery = @"
                        UPDATE Certificates 
                        SET DownloadCount = ISNULL(DownloadCount, 0) + 1,
                            LastDownloaded = @LastDownloaded
                        WHERE ApplicationID = @ApplicationID";

                    using (SqlCommand command = new SqlCommand(updateQuery, connection))
                    {
                        command.Parameters.AddWithValue("@ApplicationID", applicationId);
                        command.Parameters.AddWithValue("@LastDownloaded", DateTime.Now);
                        await command.ExecuteNonQueryAsync();
                    }
                }

                TempData["SuccessMessage"] = "Certificate PDF generated successfully!";
                return RedirectToPage(new { applicationId });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error generating PDF: {ex.Message}";
                return RedirectToPage(new { applicationId });
            }
        }

        private async Task LoadCertificateDataAsync(int applicationId)
        {
            using (SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                await connection.OpenAsync();

                string query = @"
                    SELECT 
                        a.ID as ApplicationID,
                        a.InstitutionName,
                        a.Trade,
                        a.Email,
                        a.ContactPerson,
                        a.Address,
                        a.ApprovalDate,
                        a.ReviewNotes,
                        c.CertificateNumber,
                        c.IssueDate,
                        c.IssuedBy,
                        c.Status as CertificateStatus,
                        c.DownloadCount,
                        c.LastDownloaded
                    FROM Applications a
                    LEFT JOIN Certificates c ON a.ID = c.ApplicationID
                    WHERE a.ID = @ApplicationID AND a.Status = 'Approved'";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@ApplicationID", applicationId);

                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            Certificate = new CertificateData
                            {
                                ApplicationID = reader.GetInt32("ApplicationID"),
                                InstitutionName = reader.GetString("InstitutionName"),
                                Trade = reader.GetString("Trade"),
                                Email = reader.GetString("Email"),
                                ContactPerson = reader.IsDBNull("ContactPerson") ? null : reader.GetString("ContactPerson"),
                                Address = reader.IsDBNull("Address") ? null : reader.GetString("Address"),
                                ApprovalDate = reader.IsDBNull("ApprovalDate") ? null : reader.GetDateTime("ApprovalDate"),
                                ReviewNotes = reader.IsDBNull("ReviewNotes") ? null : reader.GetString("ReviewNotes"),
                                CertificateNumber = reader.IsDBNull("CertificateNumber") ? null : reader.GetString("CertificateNumber"),
                                IssueDate = reader.IsDBNull("IssueDate") ? null : reader.GetDateTime("IssueDate"),
                                IssuedBy = reader.IsDBNull("IssuedBy") ? null : reader.GetString("IssuedBy"),
                                CertificateStatus = reader.IsDBNull("CertificateStatus") ? null : reader.GetString("CertificateStatus"),
                                DownloadCount = reader.IsDBNull("DownloadCount") ? 0 : reader.GetInt32("DownloadCount"),
                                LastDownloaded = reader.IsDBNull("LastDownloaded") ? null : reader.GetDateTime("LastDownloaded")
                            };

                            CertificateExists = !string.IsNullOrEmpty(Certificate.CertificateNumber);
                        }
                        else
                        {
                            throw new Exception($"Application with ID {applicationId} not found or not approved.");
                        }
                    }
                }
            }
        }

        public class CertificateData
        {
            public int ApplicationID { get; set; }
            public string InstitutionName { get; set; }
            public string Trade { get; set; }
            public string Email { get; set; }
            public string ContactPerson { get; set; }
            public string Address { get; set; }
            public DateTime? ApprovalDate { get; set; }
            public string ReviewNotes { get; set; }
            public string CertificateNumber { get; set; }
            public DateTime? IssueDate { get; set; }
            public string IssuedBy { get; set; }
            public string CertificateStatus { get; set; }
            public int DownloadCount { get; set; }
            public DateTime? LastDownloaded { get; set; }
        }
    }
}