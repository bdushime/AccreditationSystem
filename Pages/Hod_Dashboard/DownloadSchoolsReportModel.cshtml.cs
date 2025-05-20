using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using CsvHelper;
using System.Globalization;

namespace AccreditationSystem.Pages.Hod_Dashboard
{
    public class DownloadSchoolsReportModel : PageModel
    {
        private readonly IConfiguration _configuration;

        public DownloadSchoolsReportModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            List<SchoolReportDTO> schoolsReport = new List<SchoolReportDTO>();

            try
            {
                using (SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    await connection.OpenAsync();

                    // Query to get all schools
                    string query = "SELECT id, school_name, school_email, school_type, school_phone, " +
                                  "address_line_1, address_line_2, city, state_province, status, createdAt " +
                                  "FROM school ORDER BY school_name";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                // Get address components, handling potential null values
                                string addressLine1 = reader.GetString(reader.GetOrdinal("address_line_1"));
                                string addressLine2 = reader.IsDBNull(reader.GetOrdinal("address_line_2")) ? "" : reader.GetString(reader.GetOrdinal("address_line_2"));
                                string city = reader.GetString(reader.GetOrdinal("city"));
                                string stateProvince = reader.GetString(reader.GetOrdinal("state_province"));

                                // Format the full address
                                string fullAddress = addressLine1;
                                if (!string.IsNullOrEmpty(addressLine2))
                                    fullAddress += ", " + addressLine2;
                                fullAddress += ", " + city + ", " + stateProvince;

                                // Add school to the report list
                                schoolsReport.Add(new SchoolReportDTO
                                {
                                    Id = reader.GetInt32(reader.GetOrdinal("id")),
                                    Name = reader.GetString(reader.GetOrdinal("school_name")),
                                    Email = reader.GetString(reader.GetOrdinal("school_email")),
                                    Type = reader.GetString(reader.GetOrdinal("school_type")),
                                    Phone = reader.GetString(reader.GetOrdinal("school_phone")),
                                    Address = fullAddress,
                                    Status = reader.GetString(reader.GetOrdinal("status")),
                                    RegistrationDate = reader.GetDateTime(reader.GetOrdinal("createdAt"))
                                });
                            }
                        }
                    }
                }

                // Generate CSV file
                var fileName = $"Schools_Report_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
                var stream = new MemoryStream();

                using (var writer = new StreamWriter(stream, leaveOpen: true))
                using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
                {
                    // Write header
                    csv.WriteHeader<SchoolReportDTO>();
                    csv.NextRecord();

                    // Write records
                    foreach (var school in schoolsReport)
                    {
                        csv.WriteRecord(school);
                        csv.NextRecord();
                    }
                }

                stream.Position = 0;

                // Return file for download
                return File(stream, "text/csv", fileName);
            }
            catch (Exception ex)
            {
                // Log the error and return to the schools page with an error message
                TempData["ErrorMessage"] = $"Error generating report: {ex.Message}";
                return RedirectToPage("./HOD_Schools");
            }
        }
    }

    // DTO for CSV export
    public class SchoolReportDTO
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Type { get; set; }
        public string Phone { get; set; }
        public string Address { get; set; }
        public string Status { get; set; }
        public DateTime RegistrationDate { get; set; }
    }
}