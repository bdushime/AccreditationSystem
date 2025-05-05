using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
namespace AccreditationSystem.Pages.Auth
{
    [BindProperties]
    public class ResetPasswordModel : PageModel
    {
        private readonly IConfiguration _configuration;
        public ResetPasswordModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        [Required]
        public string Email { get; set; }
        [Required]
        public string Token { get; set; }
        [Required(ErrorMessage = "New password is required")]
        [StringLength(100, ErrorMessage = "Password must be at least {2} characters long", MinimumLength = 8)]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Required(ErrorMessage = "Confirm password is required")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Passwords do not match")]
        public string ConfirmPassword { get; set; }

        public bool IsValidToken { get; set; } = false;
        public string errorMessage = "";
        public string successMessage = "";

        public async Task<IActionResult> OnGetAsync(string email, string token)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(token))
            {
                errorMessage = "Invalid password reset link";
                return Page();
            }

            Email = email;
            Token = token;

            // Check if the token is valid
            IsValidToken = await ValidateTokenAsync(email, token);

            if (!IsValidToken)
            {
                errorMessage = "The password reset link is invalid or has expired";
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                IsValidToken = true; // We assume the token is valid since we're in POST
                return Page();
            }

            // Check if the token is valid
            IsValidToken = await ValidateTokenAsync(Email, Token);

            if (!IsValidToken)
            {
                errorMessage = "The password reset link is invalid or has expired";
                return Page();
            }

            try
            {
                string connectionString = _configuration.GetConnectionString("DefaultConnection");
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    // Update the user's password
                    string updateQuery = @"
                        UPDATE users 
                        SET password = @Password
                        WHERE email = @Email";

                    using (SqlCommand command = new SqlCommand(updateQuery, connection))
                    {
                        // Use the same hashing method as in Login
                        string hashedPassword = HashPassword(Password);

                        command.Parameters.AddWithValue("@Email", Email);
                        command.Parameters.AddWithValue("@Password", hashedPassword);

                        int rowsAffected = await command.ExecuteNonQueryAsync();

                        if (rowsAffected > 0)
                        {
                            // Delete used token
                            string deleteTokenQuery = "DELETE FROM password_reset_tokens WHERE email = @Email";
                            using (SqlCommand deleteCommand = new SqlCommand(deleteTokenQuery, connection))
                            {
                                deleteCommand.Parameters.AddWithValue("@Email", Email);
                                await deleteCommand.ExecuteNonQueryAsync();
                            }

                            successMessage = "Your password has been reset successfully. You can now login with your new password.";

                            // Redirect to login page
                            return RedirectToPage("/Auth/Login", new { passwordReset = true });
                        }
                        else
                        {
                            errorMessage = "Failed to reset password. Please try again.";
                            IsValidToken = true; // Keep the form visible
                            return Page();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                errorMessage = $"An error occurred: {ex.Message}";
                IsValidToken = true; // Keep the form visible
                return Page();
            }
        }

        private async Task<bool> ValidateTokenAsync(string email, string token)
        {
            try
            {
                string connectionString = _configuration.GetConnectionString("DefaultConnection");
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    string query = @"
                        SELECT COUNT(*) 
                        FROM password_reset_tokens 
                        WHERE email = @Email 
                        AND token = @Token 
                        AND expiry_date > GETDATE()";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Email", email);
                        command.Parameters.AddWithValue("@Token", token);

                        int count = Convert.ToInt32(await command.ExecuteScalarAsync());
                        return count > 0;
                    }
                }
            }
            catch
            {
                return false;
            }
        }

        // Using the exact same hashing method as in LoginModel
        private string HashPassword(string password)
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(password));

                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }
    }
}