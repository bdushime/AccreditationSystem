using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.IO;

namespace AccreditationSystem.Pages.Hod_Dashboard
{
    public class DownloadDocumentModel : PageModel
    {
        private readonly IWebHostEnvironment _environment;

        public DownloadDocumentModel(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        public IActionResult OnGet(string path)
        {
            // Check if user is authenticated
            if (HttpContext.Session.GetInt32("UserId") == null)
            {
                return RedirectToPage("/Auth/Login");
            }

            // Check if user has HOD role
            string userRole = HttpContext.Session.GetString("UserRole")?.ToLower();
            if (userRole != "hod" && userRole != "admin")
            {
                return RedirectToPage("/AccessDenied");
            }

            if (string.IsNullOrEmpty(path))
            {
                return BadRequest("Document path is required");
            }

            try
            {
                // Build the file path
                string uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads");
                string filePath = Path.Combine(uploadsFolder, path);

                // Security check - make sure the path is within the uploads directory
                if (!filePath.StartsWith(uploadsFolder))
                {
                    return BadRequest("Invalid document path");
                }

                // Check if file exists
                if (!System.IO.File.Exists(filePath))
                {
                    return NotFound("File not found");
                }

                // Get the file name
                string fileName = Path.GetFileName(filePath);

                // Determine content type based on file extension
                string contentType = GetContentType(Path.GetExtension(fileName).ToLowerInvariant());

                // Return the file
                return PhysicalFile(filePath, contentType, fileName);
            }
            catch (Exception ex)
            {
                // Log error
                return StatusCode(500, $"Error downloading document: {ex.Message}");
            }
        }

        private string GetContentType(string fileExtension)
        {
            switch (fileExtension)
            {
                case ".pdf":
                    return "application/pdf";
                case ".doc":
                    return "application/msword";
                case ".docx":
                    return "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
                case ".xls":
                    return "application/vnd.ms-excel";
                case ".xlsx":
                    return "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                case ".zip":
                    return "application/zip";
                case ".jpg":
                case ".jpeg":
                    return "image/jpeg";
                case ".png":
                    return "image/png";
                default:
                    return "application/octet-stream";
            }
        }
    }
}