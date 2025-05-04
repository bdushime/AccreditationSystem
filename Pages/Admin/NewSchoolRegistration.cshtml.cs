using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;

namespace AccreditationSystem.Pages.Admin
{
    public class NewSchoolRegistrationModel : PageModel
    {
        private readonly ILogger<NewSchoolRegistrationModel> _logger;

        public NewSchoolRegistrationModel(ILogger<NewSchoolRegistrationModel> logger)
        {
            _logger = logger;
        }

        [BindProperty]
        public SchoolRegistration Registration { get; set; }

        public SelectList SchoolTypes { get; set; }
        public SelectList Countries { get; set; }

        [TempData]
        public string SuccessMessage { get; set; }

        [TempData]
        public string ErrorMessage { get; set; }

        public void OnGet()
        {
            // Initialize the registration model if it's not already populated
            Registration = new SchoolRegistration();

            // Populate dropdown lists
            PopulateDropdownLists();

            _logger.LogInformation("School registration page visited at {time}", DateTime.UtcNow);
        }


        public async Task<IActionResult> OnPostAsync()
        {
            //if (!ModelState.IsValid)
            //{
            //    return Page();
            //}

            // Create a new application object
            var application = new AccreditationSystem.Models.Schools
            {
                // Basic Information
                school_name = Request.Form["schoolName"],
                school_email = Request.Form["schoolEmail"],
                school_phone = Request.Form["schoolPhone"],
                school_website = Request.Form["schoolWebsite"],
                year_established = int.TryParse(Request.Form["yearEstablished"], out var year) ? year : null,
                school_type = Request.Form["schoolType"],
                education_levels = string.Join(",", Request.Form["educationLevels[]"]),

                // Facilities Information
                number_of_classrooms = int.TryParse(Request.Form["classroomCount"], out var classrooms) ? classrooms : 0,
                average_classroom_size = decimal.TryParse(Request.Form["classroomSize"], out var size) ? size : 0,
                average_students_per_classroom = int.TryParse(Request.Form["studentsPerClass"], out var students) ? students : 0,
                classroom_equipment_level = Request.Form["equipmentLevel"],
                specialized_facilities = string.Join(",", Request.Form["specializedFacilities[]"]),
                total_student_enrollment = int.TryParse(Request.Form["totalEnrollment"], out var enrollment) ? enrollment : 0,
                number_of_teaching_staff = int.TryParse(Request.Form["teachingStaff"], out var staff) ? staff : 0,

                // School Address
                address_line_1 = Request.Form["addressLine1"],
                address_line_2 = Request.Form["addressLine2"],
                city = Request.Form["city"],
                state_province = Request.Form["state"],

                // Additional fields
                status = "Pending",
                createdAt = DateTime.UtcNow,
                updatedAt = DateTime.UtcNow
            };

            try
            {
                string connectionString = "Data Source=DB7beni\\SQLEXPRESS;Initial Catalog=AccreditationFinal;Integrated Security=True;TrustServerCertificate=True";

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();


                    string sql = @"INSERT INTO school 
                (school_name, school_email, school_phone, school_website, year_established, 
                 school_type, education_levels, number_of_classrooms, average_classroom_size, 
                 average_students_per_classroom, classroom_equipment_level, specialized_facilities, 
                 total_student_enrollment, number_of_teaching_staff, address_line_1, 
                 address_line_2, city, state_province, status, createdAt, updatedAt) 
                VALUES 
                (@school_name, @school_email, @school_phone, @school_website, @year_established, 
                 @school_type, @education_levels, @number_of_classrooms, @average_classroom_size, 
                 @average_students_per_classroom, @classroom_equipment_level, @specialized_facilities, 
                 @total_student_enrollment, @number_of_teaching_staff, @address_line_1, 
                 @address_line_2, @city, @state_province, @status, @createdAt, @updatedAt)";

                    using (SqlCommand command = new SqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@school_name", application.school_name);
                        command.Parameters.AddWithValue("@school_email", application.school_email);
                        command.Parameters.AddWithValue("@school_phone", application.school_phone);
                        command.Parameters.AddWithValue("@school_website", application.school_website ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@year_established", application.year_established ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@school_type", application.school_type);
                        command.Parameters.AddWithValue("@education_levels", application.education_levels);
                        command.Parameters.AddWithValue("@number_of_classrooms", application.number_of_classrooms);
                        command.Parameters.AddWithValue("@average_classroom_size", application.average_classroom_size);
                        command.Parameters.AddWithValue("@average_students_per_classroom", application.average_students_per_classroom);
                        command.Parameters.AddWithValue("@classroom_equipment_level", application.classroom_equipment_level);
                        command.Parameters.AddWithValue("@specialized_facilities", application.specialized_facilities ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@total_student_enrollment", application.total_student_enrollment);
                        command.Parameters.AddWithValue("@number_of_teaching_staff", application.number_of_teaching_staff);
                        command.Parameters.AddWithValue("@address_line_1", application.address_line_1);
                        command.Parameters.AddWithValue("@address_line_2", application.address_line_2 ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@city", application.city);
                        command.Parameters.AddWithValue("@state_province", application.state_province);
                        command.Parameters.AddWithValue("@status", application.status);
                        command.Parameters.AddWithValue("@createdAt", application.createdAt);
                        command.Parameters.AddWithValue("@updatedAt", application.updatedAt);

                        int result = await command.ExecuteNonQueryAsync();
                        Console.WriteLine("Result", result);
                        if (result > 0)
                        {
                            SuccessMessage = "Application submitted successfully!";
                            //return RedirectToPage("/ConfirmationModel");
                            return Page();
                        }
                        else
                        {
                            ErrorMessage = "Failed to submit application.";
                            return Page();
                        }

                    }
                }
                // Redirect to a confirmation page
                return RedirectToPage("./ConfirmationModel");
            }
            catch (Exception ex)
            {
                // Log the error
                ModelState.AddModelError("", "An error occurred while saving the application: " + ex.Message);
                return Page();
            }

            // Remove these lines since they're using Entity Framework, which doesn't match your approach
            // _context.Applications.Add(application);
            // await _context.SaveChangesAsync();
        }


        private void PopulateDropdownLists()
        {
            // Populate school types dropdown
            var schoolTypesList = new List<string>
            {
                "Public",
                "Private",
                "Charter",
                "International",
                "Vocational",
                "Technical",
                "Religious"
            };
            SchoolTypes = new SelectList(schoolTypesList);

            // Populate countries dropdown (abbreviated list for example)
            var countriesList = new List<string>
            {
                "United States",
                "Canada",
                "United Kingdom",
                "Australia",
                "Germany",
                "France",
                "Japan",
                "China",
                "India",
                "Brazil",
                "South Africa",
                "Nigeria",
                "Kenya",
                "Ghana"
                // Add more countries as needed
            };
            Countries = new SelectList(countriesList);
        }
    }
}
