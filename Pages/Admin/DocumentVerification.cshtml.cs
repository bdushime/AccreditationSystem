using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using AccreditationSystem.Services;
using AccreditationSystem.Utilities;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace AccreditationSystem.Pages.Admin
{
    public class DocumentVerificationModel : PageModel
    {
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _environment;
        private readonly DocumentValidationService _validationService;

        [TempData]
        public string SuccessMessage { get; set; }

        [TempData]
        public string ErrorMessage { get; set; }

        public string GeneratedHash { get; set; }

        public DocumentVerificationModel(
            IConfiguration configuration,
            IWebHostEnvironment environment)
        {
            _configuration = configuration;
            _environment = environment;
            _validationService = new DocumentValidationService(configuration, environment);
        }

        public void OnGet()
        {
            // Initialize the page
        }

        public async Task<IActionResult> OnPostVerifyByIdAsync(int documentId, IFormFile documentFile)
        {
            if (documentFile == null || documentFile.Length == 0)
            {
                ErrorMessage = "Please select a file to verify.";
                return Page();
            }

            bool isValid = await _validationService.ValidateDocumentAsync(documentId, documentFile);

            if (isValid)
            {
                SuccessMessage = "Document verification successful! The uploaded file matches the stored hash.";
            }
            else
            {
                ErrorMessage = "Document verification failed! The uploaded file does not match the stored hash.";
            }

            return Page();
        }

        public async Task<IActionResult> OnPostVerifyClaimAsync(int claimId, string documentType, IFormFile claimFile)
        {
            if (claimFile == null || claimFile.Length == 0)
            {
                ErrorMessage = "Please select a file to verify.";
                return Page();
            }

            if (string.IsNullOrEmpty(documentType))
            {
                ErrorMessage = "Please select a document type.";
                return Page();
            }

            bool isValid = await _validationService.ValidateClaimDocumentAsync(claimId, documentType, claimFile);

            if (isValid)
            {
                SuccessMessage = $"Claim document verification successful! The uploaded {documentType} file matches the stored hash.";
            }
            else
            {
                ErrorMessage = $"Claim document verification failed! The uploaded {documentType} file does not match the stored hash.";
            }

            return Page();
        }

        public async Task<IActionResult> OnPostGenerateHashAsync(IFormFile hashFile)
        {
            if (hashFile == null || hashFile.Length == 0)
            {
                ErrorMessage = "Please select a file to hash.";
                return Page();
            }

            try
            {
                GeneratedHash = await DocumentHasher.GenerateFileHashAsync(hashFile);
                SuccessMessage = "Hash generated successfully.";
            }
            catch (Exception ex)
            {
                ErrorMessage = $"An error occurred while generating the hash: {ex.Message}";
            }

            return Page();
        }
    }
}