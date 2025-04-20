using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AccreditationSystem.Pages.Hod_Dashboard
{
    public class HOD_HomeModel : PageModel
    {
        private readonly IConfiguration _configuration;

        [BindProperty(SupportsGet = true)]
        public int CurrentPage { get; set; } = 1;

        public int PageSize { get; set; } = 5;
        public int TotalRecords { get; set; }
        public int TotalPages => (int)Math.Ceiling(TotalRecords / (double)PageSize);

        public List<Application> Applications { get; set; }

        public HOD_HomeModel(IConfiguration configuration)
        {
            _configuration = configuration;
            Applications = new List<Application>();
        }

        public async Task OnGetAsync()
        {
            if (CurrentPage < 1)
            {
                CurrentPage = 1;
            }

            await LoadApplicationsAsync();
            await GetDashboardMetricsAsync();
        }

        private async Task LoadApplicationsAsync()
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    await connection.OpenAsync();

                    // Get total count for pagination
                    string countQuery = "SELECT COUNT(*) FROM Applications";
                    using (SqlCommand countCommand = new SqlCommand(countQuery, connection))
                    {
                        TotalRecords = (int)await countCommand.ExecuteScalarAsync();
                    }

                    // Get paginated data
                    string query = @"SELECT ID, SchoolEmail, InstitutionName, Email, Trade, 
                                    Status, ApplicationDate, ContactPerson 
                                    FROM Applications 
                                    ORDER BY ApplicationDate DESC
                                    OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        int skip = (CurrentPage - 1) * PageSize;
                        command.Parameters.AddWithValue("@Skip", skip);
                        command.Parameters.AddWithValue("@Take", PageSize);

                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                Applications.Add(new Application
                                {
                                    ID = reader.GetInt32(reader.GetOrdinal("ID")),
                                    SchoolEmail = reader.GetString(reader.GetOrdinal("SchoolEmail")),
                                    InstitutionName = reader.GetString(reader.GetOrdinal("InstitutionName")),
                                    Email = reader.GetString(reader.GetOrdinal("Email")),
                                    Trade = reader.GetString(reader.GetOrdinal("Trade")),
                                    Status = reader.GetString(reader.GetOrdinal("Status")),
                                    ApplicationDate = reader.GetDateTime(reader.GetOrdinal("ApplicationDate")),
                                    ContactPerson = reader.IsDBNull(reader.GetOrdinal("ContactPerson")) ?
                                        null : reader.GetString(reader.GetOrdinal("ContactPerson"))
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Log the exception or display an error message
                TempData["ErrorMessage"] = $"Error retrieving applications: {ex.Message}";
            }
        }

        private async Task GetDashboardMetricsAsync()
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    await connection.OpenAsync();

                    // Get current month's requests count
                    string requestsQuery = @"SELECT COUNT(*) FROM Applications 
                                            WHERE MONTH(ApplicationDate) = MONTH(GETDATE()) 
                                            AND YEAR(ApplicationDate) = YEAR(GETDATE())";
                    using (SqlCommand command = new SqlCommand(requestsQuery, connection))
                    {
                        RequestsThisMonth = (int)await command.ExecuteScalarAsync();
                    }

                    // Get total claims count
                    string claimsQuery = "SELECT COUNT(*) FROM Applications WHERE Status = 'Approved'";
                    using (SqlCommand command = new SqlCommand(claimsQuery, connection))
                    {
                        TotalClaims = (int)await command.ExecuteScalarAsync();
                    }
                }
            }
            catch (Exception)
            {
                // Fallback to default values if query fails
                RequestsThisMonth = 0;
                TotalClaims = 0;
            }
        }

        public int RequestsThisMonth { get; private set; }
        public int TotalClaims { get; private set; }

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
        }
    }
}