using AccreditationSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;

namespace AccreditationSystem.Pages.Admin
{

    public class SchoolsModel : PageModel
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<SchoolsModel> _logger;

        public SchoolsModel(IConfiguration configuration, ILogger<SchoolsModel> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public IList<Schools> Schools { get; set; } = new List<Schools>();

        [BindProperty(SupportsGet = true)]
        public string SearchString { get; set; }

        [BindProperty(SupportsGet = true)]
        public string SchoolTypeFilter { get; set; }

        [BindProperty(SupportsGet = true)]
        public string LocationFilter { get; set; }

        [BindProperty(SupportsGet = true)]
        public string CurrentSort { get; set; }

        [BindProperty(SupportsGet = true)]
        public int CurrentPage { get; set; } = 1;

        public int PageSize { get; set; } = 10;
        public int TotalPages { get; set; }
        public string CurrentFilter { get; set; }

        public string NameSort { get; set; }
        public string LocationSort { get; set; }

        public SelectList SchoolTypes { get; set; }
        public SelectList Locations { get; set; }

        [TempData]
        public string SuccessMessage { get; set; }

        [TempData]
        public string ErrorMessage { get; set; }

        public async Task OnGetAsync(string sortOrder, int? pageIndex)
        {
            // Store current filter for pagination
            CurrentFilter = $"SearchString={SearchString}&SchoolTypeFilter={SchoolTypeFilter}&LocationFilter={LocationFilter}";

            // Handle sorting
            NameSort = sortOrder == "name_asc" ? "name_desc" : "name_asc";
            LocationSort = sortOrder == "location_asc" ? "location_desc" : "location_asc";
            CurrentSort = sortOrder ?? "name_asc";

            // Handle pagination
            if (pageIndex.HasValue)
            {
                CurrentPage = pageIndex.Value;
            }

            try
            {
                string connectionString = "Data Source=DB7beni\\SQLEXPRESS;Initial Catalog=AccreditationFinal;Integrated Security=True;TrustServerCertificate=True";

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    // Get school types for filter dropdown
                    string typesQuery = "SELECT DISTINCT school_type FROM school";
                    using (SqlCommand typesCommand = new SqlCommand(typesQuery, connection))
                    {
                        using (SqlDataReader typesReader = await typesCommand.ExecuteReaderAsync())
                        {
                            List<string> typesList = new List<string>();
                            while (await typesReader.ReadAsync())
                            {
                                if (!typesReader.IsDBNull(0))
                                {
                                    typesList.Add(typesReader.GetString(0));
                                }
                            }
                            SchoolTypes = new SelectList(typesList);
                        }
                    }

                    // Get locations for filter dropdown
                    string locationsQuery = "SELECT DISTINCT city FROM school";
                    using (SqlCommand locationsCommand = new SqlCommand(locationsQuery, connection))
                    {
                        using (SqlDataReader locationsReader = await locationsCommand.ExecuteReaderAsync())
                        {
                            List<string> locationsList = new List<string>();
                            while (await locationsReader.ReadAsync())
                            {
                                if (!locationsReader.IsDBNull(0))
                                {
                                    locationsList.Add(locationsReader.GetString(0));
                                }
                            }
                            Locations = new SelectList(locationsList);
                        }
                    }

                    // Construct base query
                    string query = "SELECT * FROM school WHERE 1=1";
                    List<SqlParameter> parameters = new List<SqlParameter>();

                    // Apply search filter
                    if (!string.IsNullOrEmpty(SearchString))
                    {
                        query += " AND (school_name LIKE @SearchString OR city LIKE @SearchString)";
                        parameters.Add(new SqlParameter("@SearchString", $"%{SearchString}%"));
                    }

                    // Apply type filter
                    if (!string.IsNullOrEmpty(SchoolTypeFilter))
                    {
                        query += " AND school_type = @SchoolType";
                        parameters.Add(new SqlParameter("@SchoolType", SchoolTypeFilter));
                    }

                    // Apply location filter
                    if (!string.IsNullOrEmpty(LocationFilter))
                    {
                        query += " AND city = @Location";
                        parameters.Add(new SqlParameter("@Location", LocationFilter));
                    }

                    // Count total for pagination
                    string countQuery = query.Replace("SELECT *", "SELECT COUNT(*)");
                    using (SqlCommand countCommand = new SqlCommand(countQuery, connection))
                    {
                        foreach (var param in parameters)
                        {
                            countCommand.Parameters.Add(param);
                        }

                        int totalItems = (int)await countCommand.ExecuteScalarAsync();
                        TotalPages = (int)Math.Ceiling(totalItems / (double)PageSize);
                    }

                    // Apply sorting
                    query += CurrentSort switch
                    {
                        "name_asc" => " ORDER BY school_name ASC",
                        "name_desc" => " ORDER BY school_name DESC",
                        "location_asc" => " ORDER BY city ASC",
                        "location_desc" => " ORDER BY city DESC",
                        _ => " ORDER BY school_name ASC"
                    };

                    // Apply pagination
                    query += " OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY";
                    parameters.Add(new SqlParameter("@Skip", (CurrentPage - 1) * PageSize));
                    parameters.Add(new SqlParameter("@Take", PageSize));

                    // Execute final query
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        foreach (var param in parameters)
                        {
                            command.Parameters.Add(param);
                        }

                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                Schools.Add(new Schools
                                {
                                    id = reader.GetInt32(reader.GetOrdinal("id")),
                                    school_name = reader.GetString(reader.GetOrdinal("school_name")),
                                    school_email = reader.GetString(reader.GetOrdinal("school_email")),
                                    school_phone = reader.IsDBNull(reader.GetOrdinal("school_phone")) ? null : reader.GetString(reader.GetOrdinal("school_phone")),
                                    school_website = reader.IsDBNull(reader.GetOrdinal("school_website")) ? null : reader.GetString(reader.GetOrdinal("school_website")),
                                    year_established = reader.IsDBNull(reader.GetOrdinal("year_established")) ? null : (int?)reader.GetInt32(reader.GetOrdinal("year_established")),
                                    school_type = reader.IsDBNull(reader.GetOrdinal("school_type")) ? null : reader.GetString(reader.GetOrdinal("school_type")),
                                    education_levels = reader.IsDBNull(reader.GetOrdinal("education_levels")) ? null : reader.GetString(reader.GetOrdinal("education_levels")),
                                    number_of_classrooms = reader.IsDBNull(reader.GetOrdinal("number_of_classrooms")) ? 0 : reader.GetInt32(reader.GetOrdinal("number_of_classrooms")),
                                    average_classroom_size = reader.IsDBNull(reader.GetOrdinal("average_classroom_size")) ? 0 : reader.GetDecimal(reader.GetOrdinal("average_classroom_size")),
                                    average_students_per_classroom = reader.IsDBNull(reader.GetOrdinal("average_students_per_classroom")) ? 0 : reader.GetInt32(reader.GetOrdinal("average_students_per_classroom")),
                                    classroom_equipment_level = reader.IsDBNull(reader.GetOrdinal("classroom_equipment_level")) ? null : reader.GetString(reader.GetOrdinal("classroom_equipment_level")),
                                    specialized_facilities = reader.IsDBNull(reader.GetOrdinal("specialized_facilities")) ? null : reader.GetString(reader.GetOrdinal("specialized_facilities")),
                                    total_student_enrollment = reader.IsDBNull(reader.GetOrdinal("total_student_enrollment")) ? 0 : reader.GetInt32(reader.GetOrdinal("total_student_enrollment")),
                                    number_of_teaching_staff = reader.IsDBNull(reader.GetOrdinal("number_of_teaching_staff")) ? 0 : reader.GetInt32(reader.GetOrdinal("number_of_teaching_staff")),
                                    address_line_1 = reader.IsDBNull(reader.GetOrdinal("address_line_1")) ? null : reader.GetString(reader.GetOrdinal("address_line_1")),
                                    address_line_2 = reader.IsDBNull(reader.GetOrdinal("address_line_2")) ? null : reader.GetString(reader.GetOrdinal("address_line_2")),
                                    city = reader.IsDBNull(reader.GetOrdinal("city")) ? null : reader.GetString(reader.GetOrdinal("city")),
                                    state_province = reader.IsDBNull(reader.GetOrdinal("state_province")) ? null : reader.GetString(reader.GetOrdinal("state_province")),
                                    status = reader.IsDBNull(reader.GetOrdinal("status")) ? null : reader.GetString(reader.GetOrdinal("status")),
                                    createdAt = reader.IsDBNull(reader.GetOrdinal("createdAt")) ? DateTime.MinValue : reader.GetDateTime(reader.GetOrdinal("createdAt")),
                                    updatedAt = reader.IsDBNull(reader.GetOrdinal("updatedAt")) ? DateTime.MinValue : reader.GetDateTime(reader.GetOrdinal("updatedAt"))
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving schools: {Message}", ex.Message);
                ErrorMessage = "An error occurred while retrieving the schools. Please try again later.";
            }
        }

        // Add a handler for delete functionality
        public async Task<IActionResult> OnPostAsync(int id)
        {
            try
            {
                string connectionString = "Data Source=DB7beni\\SQLEXPRESS;Initial Catalog=AccreditationFinal;Integrated Security=True;TrustServerCertificate=True";

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    string query = "DELETE FROM school WHERE id = @Id";
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Id", id);
                        int result = await command.ExecuteNonQueryAsync();

                        if (result > 0)
                        {
                            SuccessMessage = "School deleted successfully.";
                        }
                        else
                        {
                            ErrorMessage = "School not found or could not be deleted.";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting school: {Message}", ex.Message);
                ErrorMessage = "An error occurred while deleting the school. Please try again later.";
            }

            return RedirectToPage();
        }
    }
}
