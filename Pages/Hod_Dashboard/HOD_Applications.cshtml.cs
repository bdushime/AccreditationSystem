using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;

namespace AccreditationSystem.Pages.Hod_Dashboard
{
    

    public class HOD_ApplicationsModel : PageModel
    {
        private readonly IConfiguration _configuration;
        public List<Application> Applications { get; set; }

        public HOD_ApplicationsModel(IConfiguration configuration)
        {
            _configuration = configuration;
            Applications = new List<Application>();
        }

        public async Task OnGetAsync()
        {
            await LoadApplicationsAsync();
        }

        public async Task<IActionResult> OnPostUpdateStatusAsync(int applicationId, string status)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    await connection.OpenAsync();
                    string query = "UPDATE Applications SET Status = @Status, ReviewDate = GETDATE() WHERE ID = @ID";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@ID", applicationId);
                        command.Parameters.AddWithValue("@Status", status);

                        int rowsAffected = await command.ExecuteNonQueryAsync();

                        if (rowsAffected > 0)
                        {
                            TempData["SuccessMessage"] = $"Application status updated to '{status}' successfully.";
                        }
                        else
                        {
                            TempData["ErrorMessage"] = "No records were updated.";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error updating application status: {ex.Message}";
            }

            return RedirectToPage();
        }

        private async Task LoadApplicationsAsync()
        {
            try
            {
                Applications.Clear();

                using (SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    await connection.OpenAsync();
                    string query = @"SELECT ID, SchoolEmail, InstitutionName, Email, Trade, 
                                    Status, ApplicationDate, ContactPerson 
                                    FROM Applications 
                                    ORDER BY ApplicationDate DESC";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
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
                TempData["ErrorMessage"] = $"Error retrieving applications: {ex.Message}";
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
        }
    }
}
