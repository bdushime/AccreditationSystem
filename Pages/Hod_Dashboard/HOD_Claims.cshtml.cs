using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AccreditationSystem.Pages.Hod_Dashboard
{
    public class ClaimsModel : PageModel
    {
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _environment;

        // Property to hold the list of accreditation claims
        public List<AccreditationClaim> Applications { get; set; }

        // Pagination properties
        public int PageSize { get; set; } = 10;
        public int CurrentPage { get; set; } = 1;
        public int TotalRecords { get; set; } = 0;
        public int TotalPages => (int)Math.Ceiling(TotalRecords / (double)PageSize);

        // Filter property
        [BindProperty(SupportsGet = true)]
        public string AccreditationType { get; set; }

        public ClaimsModel(IConfiguration configuration, IWebHostEnvironment environment)
        {
            _configuration = configuration;
            _environment = environment;
            Applications = new List<AccreditationClaim>();
        }

        public async Task OnGetAsync(int? pageNumber)
        {
            try
            {
                // Set current page from query parameter if provided
                if (pageNumber.HasValue && pageNumber.Value > 0)
                {
                    CurrentPage = pageNumber.Value;
                }

                using (SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    await connection.OpenAsync();

                    // First get total count for pagination
                    string countQuery = "SELECT COUNT(*) FROM AccreditationClaims";
                    if (!string.IsNullOrEmpty(AccreditationType))
                    {
                        countQuery += " WHERE AccreditationType = @AccreditationType";
                    }

                    using (SqlCommand countCommand = new SqlCommand(countQuery, connection))
                    {
                        if (!string.IsNullOrEmpty(AccreditationType))
                        {
                            countCommand.Parameters.AddWithValue("@AccreditationType", AccreditationType);
                        }

                        TotalRecords = (int)await countCommand.ExecuteScalarAsync();
                    }

                    // Build main query with filtering and pagination
                    string query = "SELECT * FROM AccreditationClaims";
                    if (!string.IsNullOrEmpty(AccreditationType))
                    {
                        query += " WHERE AccreditationType = @AccreditationType";
                    }

                    // Add ORDER BY and OFFSET/FETCH for pagination
                    query += " ORDER BY SubmissionDate DESC";
                    query += " OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        // Add parameters
                        if (!string.IsNullOrEmpty(AccreditationType))
                        {
                            command.Parameters.AddWithValue("@AccreditationType", AccreditationType);
                        }

                        // Calculate offset based on current page and page size
                        int offset = (CurrentPage - 1) * PageSize;
                        command.Parameters.AddWithValue("@Offset", offset);
                        command.Parameters.AddWithValue("@PageSize", PageSize);

                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                Applications.Add(new AccreditationClaim
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
                                    AdditionalComments = reader.IsDBNull(reader.GetOrdinal("AdditionalComments")) ? null : reader.GetString(reader.GetOrdinal("AdditionalComments")),
                                    ContactPhone = reader.IsDBNull(reader.GetOrdinal("ContactPhone")) ? null : reader.GetString(reader.GetOrdinal("ContactPhone"))
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Log the exception or display an error message
                TempData["ErrorMessage"] = $"Error retrieving claims: {ex.Message}";
            }
        }
    }

    // Model class for AccreditationClaim
    //public class AccreditationClaim
    //{
    //    public int ClaimID { get; set; }
    //    public string SchoolEmail { get; set; }
    //    public string AccreditationType { get; set; }
    //    public string AccreditationLevel { get; set; }
    //    public string PreviousStatus { get; set; }
    //    public DateTime StartDate { get; set; }
    //    public DateTime EndDate { get; set; }
    //    public string Status { get; set; }
    //    public DateTime SubmissionDate { get; set; }
    //    public string AdditionalComments { get; set; }
    //    public string ContactPhone { get; set; }
    //    public string ContactName { get; set; }
    //    public string ContactPosition { get; set; }
    //    public string ContactEmail { get; set; }
    //    // Additional properties can be added as needed
    //}
}