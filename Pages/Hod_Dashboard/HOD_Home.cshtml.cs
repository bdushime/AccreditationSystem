using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using System.Security.Claims;

namespace AccreditationSystem.Pages.Hod_Dashboard
{
    public class HOD_HomeModel : PageModel
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<HOD_HomeModel> _logger;

        [BindProperty(SupportsGet = true)]
        public int CurrentPage { get; set; } = 1;

        public int PageSize { get; set; } = 5;
        public int TotalRecords { get; set; }
        public int TotalPages => (int)Math.Ceiling(TotalRecords / (double)PageSize);

        public List<Application> Applications { get; set; }

        // Add properties for dashboard metrics
        public int RequestsThisMonth { get; private set; }
        public int TotalAnalystSpecialists { get; private set; }
        public int ScheduledInspections { get; private set; }
        public int TotalSchools { get; private set; }
        public int TotalClaims { get; private set; }
        public string UserFirstName { get; private set; } = "User"; // Default value

        public HOD_HomeModel(IConfiguration configuration, ILogger<HOD_HomeModel> logger)
        {
            _configuration = configuration;
            _logger = logger;
            Applications = new List<Application>();
        }

        public async Task OnGetAsync()
        {
            if (CurrentPage < 1)
            {
                CurrentPage = 1;
            }

            // Get the current user's email (assuming you're using authentication)
            string userEmail = User.Identity.IsAuthenticated
                ? User.FindFirstValue(ClaimTypes.Email)
                : "admin@example.com"; // Fallback for testing

            // Load data
            await GetUserName(userEmail);
            await LoadApplicationsAsync();
            await GetDashboardMetricsAsync();
        }

        private async Task GetUserName(string email)
        {
            try
            {
                string connectionString = _configuration.GetConnectionString("DefaultConnection");
                _logger.LogInformation($"Connection string: {connectionString}");

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    // Try to get the user's first name
                    string query = "SELECT FirstName FROM Users WHERE Email = @Email";
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Email", email);

                        var result = await command.ExecuteScalarAsync();
                        if (result != null && result != DBNull.Value)
                        {
                            UserFirstName = result.ToString();
                            _logger.LogInformation($"Found user: {UserFirstName}");
                        }
                        else
                        {
                            // If no user is found, try to get default HOD
                            query = "SELECT TOP 1 FirstName FROM users WHERE Role = 'Client'";
                            command.CommandText = query;
                            command.Parameters.Clear();

                            result = await command.ExecuteScalarAsync();
                            if (result != null && result != DBNull.Value)
                            {
                                UserFirstName = result.ToString();
                                _logger.LogInformation($"Using default HOD: {UserFirstName}");
                            }
                            else
                            {
                                UserFirstName = "Joshua"; // Hardcoded fallback
                                _logger.LogWarning("No user found, using fallback name");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user name");
                UserFirstName = "Joshua"; // Fallback in case of error
            }
        }

        private async Task LoadApplicationsAsync()
        {
            try
            {
                string connectionString = _configuration.GetConnectionString("DefaultConnection");
                _logger.LogInformation($"Loading applications using connection: {connectionString}");

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    // Get total count for pagination
                    string countQuery = "SELECT COUNT(*) FROM Applications";
                    using (SqlCommand countCommand = new SqlCommand(countQuery, connection))
                    {
                        TotalRecords = (int)await countCommand.ExecuteScalarAsync();
                        _logger.LogInformation($"Total applications: {TotalRecords}");
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
                        _logger.LogInformation($"Loaded {Applications.Count} applications for page {CurrentPage}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving applications");
                TempData["ErrorMessage"] = $"Error retrieving applications: {ex.Message}";
            }
        }

        private async Task GetDashboardMetricsAsync()
        {
            try
            {
                string connectionString = _configuration.GetConnectionString("DefaultConnection");
                _logger.LogInformation($"Loading metrics using connection: {connectionString}");

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    try
                    {
                        // Get current month's requests count
                        string requestsQuery = @"SELECT COUNT(*) FROM Applications 
                                                WHERE MONTH(ApplicationDate) = MONTH(GETDATE()) 
                                                AND YEAR(ApplicationDate) = YEAR(GETDATE())";

                        using (SqlCommand command = new SqlCommand(requestsQuery, connection))
                        {
                            var result = await command.ExecuteScalarAsync();
                            RequestsThisMonth = Convert.ToInt32(result);
                            _logger.LogInformation($"Requests this month: {RequestsThisMonth}");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error getting requests count");
                        RequestsThisMonth = 0;
                    }

                    try
                    {
                        // Get total schools count - using the school table you mentioned
                        string schoolsQuery = "SELECT COUNT(*) FROM school";
                        using (SqlCommand command = new SqlCommand(schoolsQuery, connection))
                        {
                            var result = await command.ExecuteScalarAsync();
                            TotalSchools = Convert.ToInt32(result);
                            _logger.LogInformation($"Total schools: {TotalSchools}");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error getting schools count");
                        TotalSchools = 0;
                    }

                    try
                    {
                        // Get total claims count - using the AccreditationClaims table
                        string claimsQuery = "SELECT COUNT(*) FROM AccreditationClaims";
                        using (SqlCommand command = new SqlCommand(claimsQuery, connection))
                        {
                            var result = await command.ExecuteScalarAsync();
                            TotalClaims = Convert.ToInt32(result);
                            _logger.LogInformation($"Total claims: {TotalClaims}");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error getting claims count");
                        TotalClaims = 0;
                    }

                    try
                    {
                        // Get scheduled inspections count
                        string inspectionsQuery = "SELECT COUNT(*) FROM Inspections WHERE Status = 'Scheduled'";
                        using (SqlCommand command = new SqlCommand(inspectionsQuery, connection))
                        {
                            var result = await command.ExecuteScalarAsync();
                            ScheduledInspections = Convert.ToInt32(result);
                            _logger.LogInformation($"Scheduled inspections: {ScheduledInspections}");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error getting inspections count - table may not exist");
                        ScheduledInspections = 0;
                    }

                    try
                    {
                        // Get total analyst specialists count
                        string specialistsQuery = "SELECT COUNT(*) FROM Users WHERE Role = 'Analyst'";
                        using (SqlCommand command = new SqlCommand(specialistsQuery, connection))
                        {
                            var result = await command.ExecuteScalarAsync();
                            TotalAnalystSpecialists = Convert.ToInt32(result);
                            _logger.LogInformation($"Total analyst specialists: {TotalAnalystSpecialists}");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error getting analyst count - table may not exist");
                        TotalAnalystSpecialists = 0;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving dashboard metrics");

                // Fallback to default values if query fails
                RequestsThisMonth = 0;
                TotalSchools = 0;
                TotalClaims = 0;
                ScheduledInspections = 0;
                TotalAnalystSpecialists = 0;
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