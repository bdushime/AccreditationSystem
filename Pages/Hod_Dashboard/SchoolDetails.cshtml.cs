using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading.Tasks;
using AccreditationSystem.Models;

namespace AccreditationSystem.Pages.Hod_Dashboard
{
    public class SchoolDetailsModel : PageModel
    {
        private readonly IConfiguration _configuration;

        [BindProperty(SupportsGet = true)]
        public int Id { get; set; }

        public Schools School { get; set; }
        public string DebugMessage { get; set; }

        public SchoolDetailsModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            // Add debugging information
            DebugMessage = $"Attempting to retrieve school with ID: {Id}";

            if (Id <= 0)
            {
                DebugMessage += " | ID is invalid (less than or equal to 0)";
                TempData["ErrorMessage"] = "Invalid school ID.";
                // Fix: Redirect to the correct page
                return Page();
                //return RedirectToPage("/Index");
            }

            try
            {
                string connectionString = _configuration.GetConnectionString("DefaultConnection");
                DebugMessage += $" | Connection string: {connectionString}";

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    string query = "SELECT * FROM school WHERE id = @Id";
                    DebugMessage += $" | Executing query: {query} with parameter ID = {Id}";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Id", Id);

                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                DebugMessage += " | Record found in database";
                                School = new Schools
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
                                };
                            }
                            else
                            {
                                DebugMessage += " | No record found with this ID";
                                TempData["ErrorMessage"] = $"School with ID {Id} not found.";
                                // Fix: Stay on the page and display the error message instead of redirecting
                                return Page();
                            }
                        }
                    }
                }

                return Page();
            }
            catch (Exception ex)
            {
                DebugMessage += $" | Exception occurred: {ex.Message}";
                TempData["ErrorMessage"] = $"Error retrieving school details: {ex.Message}";
                Console.Write(ex.ToString() );
                // Fix: Redirect to the home page instead
                return Page();
                //return RedirectToPage("/Index");
            }
        }
    }
}