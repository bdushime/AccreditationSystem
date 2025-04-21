using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;

namespace AccreditationSystem.Pages.Admin
{
    public class AccreditedSchoolsModel : PageModel
    {
        private readonly IConfiguration _configuration;

        [BindProperty(SupportsGet = true)]
        public string SearchTerm { get; set; } = string.Empty;

        [BindProperty(SupportsGet = true)]
        public string TradeFilter { get; set; } = string.Empty;

        [BindProperty(SupportsGet = true)]
        public int CurrentPage { get; set; } = 1;

        public int PageSize { get; set; } = 9;
        public int TotalPages { get; set; }
        public int TotalSchools { get; set; }
        public int GeneralSchools { get; set; }
        public int SpecializedSchools { get; set; }
        public int NewlyAccreditedSchools { get; set; }

        public List<AccreditedSchool> Schools { get; set; }
        public List<string> Trades { get; set; }

        public AccreditedSchoolsModel(IConfiguration configuration)
        {
            _configuration = configuration;
            Schools = new List<AccreditedSchool>();
            Trades = new List<string>();
        }

        public async Task OnGetAsync()
        {
            if (CurrentPage < 1)
            {
                CurrentPage = 1;
            }

            await LoadSchoolsAsync();
            await LoadFiltersAsync();
        }

        private async Task LoadSchoolsAsync()
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    await connection.OpenAsync();

                    // Base query for approved applications only
                    string baseQuery = "FROM Applications WHERE Status = 'Approved'";

                    // Add search filters if provided
                    if (!string.IsNullOrEmpty(SearchTerm))
                    {
                        baseQuery += " AND (InstitutionName LIKE @SearchTerm OR Trade LIKE @SearchTerm OR Comments LIKE @SearchTerm)";
                    }

                    if (!string.IsNullOrEmpty(TradeFilter))
                    {
                        baseQuery += " AND Trade = @TradeFilter";
                    }

                    // Get total count for pagination
                    string countQuery = "SELECT COUNT(*) " + baseQuery;
                    using (SqlCommand countCommand = new SqlCommand(countQuery, connection))
                    {
                        if (!string.IsNullOrEmpty(SearchTerm))
                        {
                            countCommand.Parameters.AddWithValue("@SearchTerm", "%" + SearchTerm + "%");
                        }

                        if (!string.IsNullOrEmpty(TradeFilter))
                        {
                            countCommand.Parameters.AddWithValue("@TradeFilter", TradeFilter);
                        }

                        var result = await countCommand.ExecuteScalarAsync();
                        TotalSchools = result != null ? Convert.ToInt32(result) : 0;
                    }

                    // Get count by school type (general vs specialized)
                    string generalQuery = "SELECT COUNT(*) FROM Applications WHERE Status = 'Approved' AND Trade = 'General'";
                    using (SqlCommand genCommand = new SqlCommand(generalQuery, connection))
                    {
                        var result = await genCommand.ExecuteScalarAsync();
                        GeneralSchools = result != null ? Convert.ToInt32(result) : 0;
                    }

                    string specializedQuery = "SELECT COUNT(*) FROM Applications WHERE Status = 'Approved' AND Trade != 'General'";
                    using (SqlCommand specCommand = new SqlCommand(specializedQuery, connection))
                    {
                        var result = await specCommand.ExecuteScalarAsync();
                        SpecializedSchools = result != null ? Convert.ToInt32(result) : 0;
                    }

                    // Get newly accredited count (last 30 days)
                    string newlyQuery = "SELECT COUNT(*) FROM Applications WHERE Status = 'Approved' AND DATEDIFF(day, ApprovalDate, GETDATE()) <= 30";
                    using (SqlCommand newlyCommand = new SqlCommand(newlyQuery, connection))
                    {
                        var result = await newlyCommand.ExecuteScalarAsync();
                        NewlyAccreditedSchools = result != null ? Convert.ToInt32(result) : 0;
                    }

                    // Calculate pagination
                    TotalPages = (int)Math.Ceiling(TotalSchools / (double)PageSize);
                    if (CurrentPage > TotalPages && TotalPages > 0)
                    {
                        CurrentPage = TotalPages;
                    }

                    // Get paginated data
                    string dataQuery = @"SELECT ID, SchoolEmail, InstitutionName, Email, Trade, 
                                       AssessmentScore, ApplicationDate, Status, ReviewDate, 
                                       ApprovalDate, ExpirationDate, Comments, ContactPerson " +
                                       baseQuery +
                                       " ORDER BY ApprovalDate DESC OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY";

                    using (SqlCommand command = new SqlCommand(dataQuery, connection))
                    {
                        if (!string.IsNullOrEmpty(SearchTerm))
                        {
                            command.Parameters.AddWithValue("@SearchTerm", "%" + SearchTerm + "%");
                        }

                        if (!string.IsNullOrEmpty(TradeFilter))
                        {
                            command.Parameters.AddWithValue("@TradeFilter", TradeFilter);
                        }

                        command.Parameters.AddWithValue("@Skip", (CurrentPage - 1) * PageSize);
                        command.Parameters.AddWithValue("@Take", PageSize);

                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            int imageIndex = 1;
                            while (await reader.ReadAsync())
                            {
                                // Get a placeholder image based on ID
                                string imagePath = GetImagePathForSchool(imageIndex++);

                                Schools.Add(new AccreditedSchool
                                {
                                    Id = reader.GetInt32(reader.GetOrdinal("ID")),
                                    SchoolEmail = reader.GetString(reader.GetOrdinal("SchoolEmail")),
                                    InstitutionName = reader.GetString(reader.GetOrdinal("InstitutionName")),
                                    Email = reader.GetString(reader.GetOrdinal("Email")),
                                    Trade = reader.GetString(reader.GetOrdinal("Trade")),
                                    AssessmentScore = reader.GetInt32(reader.GetOrdinal("AssessmentScore")),
                                    ApplicationDate = reader.GetDateTime(reader.GetOrdinal("ApplicationDate")),
                                    Status = reader.GetString(reader.GetOrdinal("Status")),
                                    ReviewDate = reader.IsDBNull(reader.GetOrdinal("ReviewDate")) ?
                                        null : (DateTime?)reader.GetDateTime(reader.GetOrdinal("ReviewDate")),
                                    ApprovalDate = reader.IsDBNull(reader.GetOrdinal("ApprovalDate")) ?
                                        null : (DateTime?)reader.GetDateTime(reader.GetOrdinal("ApprovalDate")),
                                    ExpirationDate = reader.IsDBNull(reader.GetOrdinal("ExpirationDate")) ?
                                        DateTime.Now.AddYears(3) : (DateTime?)reader.GetDateTime(reader.GetOrdinal("ExpirationDate")),
                                    Comments = reader.IsDBNull(reader.GetOrdinal("Comments")) ?
                                        "An accredited educational institution meeting standards for quality education." : reader.GetString(reader.GetOrdinal("Comments")),
                                    ContactPerson = reader.IsDBNull(reader.GetOrdinal("ContactPerson")) ?
                                        "N/A" : reader.GetString(reader.GetOrdinal("ContactPerson")),
                                    ImageUrl = imagePath
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error retrieving accredited schools: {ex.Message}";
            }
        }

        private async Task LoadFiltersAsync()
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    await connection.OpenAsync();

                    // Get unique trades for filter dropdown
                    string tradesQuery = "SELECT DISTINCT Trade FROM Applications WHERE Status = 'Approved' ORDER BY Trade";
                    using (SqlCommand command = new SqlCommand(tradesQuery, connection))
                    {
                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                Trades.Add(reader.GetString(0));
                            }
                        }
                    }
                }
            }
            catch
            {
                // Fallback if query fails
                Trades = new List<string> { "General", "Culinary Arts", "PCM" };
            }
        }

        private string GetImagePathForSchool(int index)
        {
            // Array of placeholder images - adjust paths to match your project structure
            string[] images = new string[]
            {
                "/images/GlobalEducationCharter.jpg",
                "/images/school2.jpg",
                "/images/school3.jpg",
                "/images/school4.jpg",
                "/images/school5.jpg",
                "/images/school6.jpg"
            };

            // Cycle through images using modulo
            return images[(index - 1) % images.Length];
        }
    }

    public class AccreditedSchool
    {
        public int Id { get; set; }
        public required string SchoolEmail { get; set; }
        public required string InstitutionName { get; set; }
        public required string Email { get; set; }
        public required string Trade { get; set; }
        public int AssessmentScore { get; set; }
        public DateTime ApplicationDate { get; set; }
        public required string Status { get; set; }
        public DateTime? ReviewDate { get; set; } // Nullable datetime
        public DateTime? ApprovalDate { get; set; } // Nullable datetime
        public DateTime? ExpirationDate { get; set; }
        public string Comments { get; set; } = string.Empty; // Default empty string
        public string ContactPerson { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;

        // Helper properties
        public string ShortDescription => Comments?.Length > 150 ?
            Comments.Substring(0, 150) + "..." :
            Comments ?? string.Empty;
    }
}
