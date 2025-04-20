using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AccreditationSystem.Models;

namespace AccreditationSystem.Pages.Hod_Dashboard
{
    public class SchoolsModel : PageModel
    {
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _environment;

        public List<Schools> Schools { get; set; }

        [BindProperty(SupportsGet = true)]
        public int PageNumber { get; set; } = 1;

        public int PageSize { get; set; } = 10;
        public int TotalSchools { get; set; }
        public int TotalPages => (int)Math.Ceiling(TotalSchools / (double)PageSize);

        public SchoolsModel(IConfiguration configuration, IWebHostEnvironment environment)
        {
            _configuration = configuration;
            _environment = environment;
            Schools = new List<Schools>();
        }

        public async Task OnGetAsync()
        {
            try
            {
                // Ensure page number is valid
                if (PageNumber < 1)
                {
                    PageNumber = 1;
                }

                using (SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    await connection.OpenAsync();

                    // Get total count for pagination
                    string countQuery = "SELECT COUNT(*) FROM school";
                    using (SqlCommand countCommand = new SqlCommand(countQuery, connection))
                    {
                        TotalSchools = (int)await countCommand.ExecuteScalarAsync();
                    }

                    // Calculate pagination values
                    int offset = (PageNumber - 1) * PageSize;

                    // Get paginated schools - using a more compatible approach
                    // First, try a simpler query without OFFSET/FETCH
                    string query = "SELECT * FROM school ORDER BY school_name";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            int counter = 0;

                            // Skip records until we reach our offset
                            while (counter < offset && await reader.ReadAsync())
                            {
                                counter++;
                            }

                            // Read only PageSize records
                            counter = 0;
                            while (counter < PageSize && await reader.ReadAsync())
                            {
                                Schools.Add(new Schools
                                {
                                    id = reader.GetInt32(reader.GetOrdinal("id")),
                                    school_name = reader.GetString(reader.GetOrdinal("school_name")),
                                    school_email = reader.GetString(reader.GetOrdinal("school_email")),
                                    school_phone = reader.GetString(reader.GetOrdinal("school_phone")),
                                    school_website = reader.IsDBNull(reader.GetOrdinal("school_website")) ? null : reader.GetString(reader.GetOrdinal("school_website")),
                                    year_established = reader.IsDBNull(reader.GetOrdinal("year_established")) ? null : (int?)reader.GetInt32(reader.GetOrdinal("year_established")),
                                    school_type = reader.GetString(reader.GetOrdinal("school_type")),
                                    education_levels = reader.GetString(reader.GetOrdinal("education_levels")),
                                    number_of_classrooms = reader.GetInt32(reader.GetOrdinal("number_of_classrooms")),
                                    average_classroom_size = reader.GetDecimal(reader.GetOrdinal("average_classroom_size")),
                                    average_students_per_classroom = reader.GetInt32(reader.GetOrdinal("average_students_per_classroom")),
                                    classroom_equipment_level = reader.GetString(reader.GetOrdinal("classroom_equipment_level")),
                                    specialized_facilities = reader.GetString(reader.GetOrdinal("specialized_facilities")),
                                    total_student_enrollment = reader.GetInt32(reader.GetOrdinal("total_student_enrollment")),
                                    number_of_teaching_staff = reader.GetInt32(reader.GetOrdinal("number_of_teaching_staff")),
                                    address_line_1 = reader.GetString(reader.GetOrdinal("address_line_1")),
                                    address_line_2 = reader.IsDBNull(reader.GetOrdinal("address_line_2")) ? null : reader.GetString(reader.GetOrdinal("address_line_2")),
                                    city = reader.GetString(reader.GetOrdinal("city")),
                                    state_province = reader.GetString(reader.GetOrdinal("state_province")),
                                    status = reader.GetString(reader.GetOrdinal("status")),
                                    createdAt = reader.GetDateTime(reader.GetOrdinal("createdAt")),
                                    updatedAt = reader.GetDateTime(reader.GetOrdinal("updatedAt"))
                                });
                                counter++;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Log the exception or display an error message
                TempData["ErrorMessage"] = $"Error retrieving schools: {ex.Message}";
            }
        }
    }
}