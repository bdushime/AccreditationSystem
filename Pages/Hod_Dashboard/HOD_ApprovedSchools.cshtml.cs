using System.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;

namespace AccreditationSystem.Pages.Hod_Dashboard
{
    public class HOD_ApprovedSchoolsModel : PageModel
    {
        private readonly IConfiguration _configuration;
        public List<ApprovedSchool> ApprovedSchools { get; set; }
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalPages { get; set; }
        public int TotalRecords { get; set; }

        public HOD_ApprovedSchoolsModel(IConfiguration configuration)
        {
            _configuration = configuration;
            ApprovedSchools = new List<ApprovedSchool>();
        }

        public async Task OnGetAsync(int currentPage = 1, string searchTerm = "", string tradeFilter = "")
        {
            CurrentPage = currentPage;
            await LoadApprovedSchoolsAsync(searchTerm, tradeFilter);
        }

        private async Task LoadApprovedSchoolsAsync(string searchTerm = "", string tradeFilter = "")
        {
            try
            {
                ApprovedSchools.Clear();

                using (SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    await connection.OpenAsync();

                    // Build the WHERE clause dynamically
                    string whereClause = "WHERE Status = 'Approved'";
                    var parameters = new List<SqlParameter>();

                    if (!string.IsNullOrEmpty(searchTerm))
                    {
                        whereClause += " AND (InstitutionName LIKE @SearchTerm OR Email LIKE @SearchTerm)";
                        parameters.Add(new SqlParameter("@SearchTerm", $"%{searchTerm}%"));
                    }

                    if (!string.IsNullOrEmpty(tradeFilter))
                    {
                        whereClause += " AND Trade = @TradeFilter";
                        parameters.Add(new SqlParameter("@TradeFilter", tradeFilter));
                    }

                    // Count total records
                    string countQuery = $@"
                        SELECT COUNT(*) 
                        FROM Applications 
                        {whereClause}";

                    using (SqlCommand countCommand = new SqlCommand(countQuery, connection))
                    {
                        countCommand.Parameters.AddRange(parameters.ToArray());
                        TotalRecords = (int)await countCommand.ExecuteScalarAsync();
                        TotalPages = (int)Math.Ceiling((double)TotalRecords / PageSize);
                    }

                    // Get paginated data
                    int offset = (CurrentPage - 1) * PageSize;
                    string query = $@"
                        SELECT ID, SchoolEmail, InstitutionName, Email, Trade, 
                               Status, ApplicationDate, ContactPerson, ApprovalDate,
                               ReviewNotes, Address, PhoneNumber
                        FROM Applications 
                        {whereClause}
                        ORDER BY ApprovalDate DESC
                        OFFSET @Offset ROWS
                        FETCH NEXT @PageSize ROWS ONLY";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddRange(parameters.ToArray());
                        command.Parameters.Add(new SqlParameter("@Offset", offset));
                        command.Parameters.Add(new SqlParameter("@PageSize", PageSize));

                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                ApprovedSchools.Add(new ApprovedSchool
                                {
                                    ID = reader.GetInt32("ID"),
                                    SchoolEmail = reader.GetString("SchoolEmail"),
                                    InstitutionName = reader.GetString("InstitutionName"),
                                    Email = reader.GetString("Email"),
                                    Trade = reader.GetString("Trade"),
                                    Status = reader.GetString("Status"),
                                    ApplicationDate = reader.GetDateTime("ApplicationDate"),
                                    ContactPerson = reader.IsDBNull("ContactPerson") ? null : reader.GetString("ContactPerson"),
                                    ApprovalDate = reader.IsDBNull("ApprovalDate") ? null : reader.GetDateTime("ApprovalDate"),
                                    ReviewNotes = reader.IsDBNull("ReviewNotes") ? null : reader.GetString("ReviewNotes"),
                                    Address = reader.IsDBNull("Address") ? null : reader.GetString("Address"),
                                    PhoneNumber = reader.IsDBNull("PhoneNumber") ? null : reader.GetString("PhoneNumber")
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error retrieving approved schools: {ex.Message}";
            }
        }

        public async Task<IActionResult> OnPostGenerateCertificateAsync(int schoolId)
        {
            try
            {
                // Check if certificate already exists
                using (SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    await connection.OpenAsync();

                    string checkQuery = @"
                        SELECT COUNT(*) 
                        FROM Certificates 
                        WHERE ApplicationID = @ApplicationID";

                    using (SqlCommand checkCommand = new SqlCommand(checkQuery, connection))
                    {
                        checkCommand.Parameters.AddWithValue("@ApplicationID", schoolId);
                        int existingCount = (int)await checkCommand.ExecuteScalarAsync();

                        if (existingCount > 0)
                        {
                            TempData["ErrorMessage"] = "A certificate has already been generated for this school.";
                            return RedirectToPage();
                        }
                    }

                    // Generate new certificate
                    string certificateNumber = GenerateCertificateNumber();
                    string insertQuery = @"
                        INSERT INTO Certificates (ApplicationID, CertificateNumber, IssueDate, IssuedBy, Status)
                        VALUES (@ApplicationID, @CertificateNumber, @IssueDate, @IssuedBy, @Status)";

                    using (SqlCommand insertCommand = new SqlCommand(insertQuery, connection))
                    {
                        insertCommand.Parameters.AddWithValue("@ApplicationID", schoolId);
                        insertCommand.Parameters.AddWithValue("@CertificateNumber", certificateNumber);
                        insertCommand.Parameters.AddWithValue("@IssueDate", DateTime.Now);
                        insertCommand.Parameters.AddWithValue("@IssuedBy", HttpContext.Session.GetString("UserName") ?? "Accreditation Officer");
                        insertCommand.Parameters.AddWithValue("@Status", "Active");

                        await insertCommand.ExecuteNonQueryAsync();
                        TempData["SuccessMessage"] = $"Certificate {certificateNumber} generated successfully!";
                    }
                }

                return RedirectToPage();
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error generating certificate: {ex.Message}";
                return RedirectToPage();
            }
        }

        private string GenerateCertificateNumber()
        {
            // Generate a unique certificate number in format: CERT-YYYY-NNNNNN
            string year = DateTime.Now.Year.ToString();
            string randomNumber = new Random().Next(100000, 999999).ToString();
            return $"CERT-{year}-{randomNumber}";
        }

        public class ApprovedSchool
        {
            public int ID { get; set; }
            public string SchoolEmail { get; set; }
            public string InstitutionName { get; set; }
            public string Email { get; set; }
            public string Trade { get; set; }
            public string Status { get; set; }
            public DateTime ApplicationDate { get; set; }
            public string ContactPerson { get; set; }
            public DateTime? ApprovalDate { get; set; }
            public string ReviewNotes { get; set; }
            public string Address { get; set; }
            public string PhoneNumber { get; set; }
        }
    }
}
