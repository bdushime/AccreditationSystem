using AccreditationSystem.Pages.Services;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;

namespace AccreditationSystem.Pages.Auth
{

    [BindProperties]
    public class RegisterModel : PageModel
    {
        private readonly IEmailService _emailService;
        private readonly IConfiguration _configuration;

        public RegisterModel(IEmailService emailService, IConfiguration configuration)
        {
            _emailService = emailService;
            _configuration = configuration;
        }

        [Required(ErrorMessage = "The First Name is required")]
        public string FirstName { get; set; } = "";

        [Required(ErrorMessage = "The Last Name is required")]
        public string LastName { get; set; } = "";

        [Required(ErrorMessage = "The Email is required"), EmailAddress]
        public string Email { get; set; } = "";

        public string? Phone { get; set; } = "";

        [Required(ErrorMessage = "The Address is required")]
        public string Address { get; set; } = "";

        [Required(ErrorMessage = "The Password is required")]
        [StringLength(50, ErrorMessage = "Password must be between 5 and 50 characters", MinimumLength = 5)]
        public string Password { get; set; } = "";

        [Required(ErrorMessage = "Confirm password is required")]
        [Compare("Password", ErrorMessage = "Password and Confirm Password do not match")]
        public string ComfirmPassword { get; set; } = "";

        [Required(ErrorMessage = "Role is required")]
        public string Role { get; set; } = "client"; // Default role

        public string errorMessage = "";
        public string successMessage = "";

        public void OnGet()
        {
            // Just display the form
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                errorMessage = "Data validation failed";
                return Page();
            }

            if (Phone == null) Phone = "";

            try
            {
                // Validate role is one of the allowed values
                if (Role != "client" && Role != "admin" && Role != "hod")
                {
                    errorMessage = "Invalid role selected";
                    return Page();
                }

                string connectionString = _configuration.GetConnectionString("DefaultConnection");
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    // Check if email already exists
                    string checkQuery = "SELECT COUNT(*) FROM users WHERE email = @Email";
                    using (Microsoft.Data.SqlClient.SqlCommand checkCommand = new SqlCommand(checkQuery, connection))
                    {
                        checkCommand.Parameters.AddWithValue("@Email", Email);
                        int existingCount = Convert.ToInt32(checkCommand.ExecuteScalar());

                        if (existingCount > 0)
                        {
                            errorMessage = "Email already registered. Please use a different email.";
                            return Page();
                        }
                    }

                    // Insert new user
                    string insertQuery = @"INSERT INTO users (firstname, lastname, email, phone, address, password, role, created_at) 
                                        VALUES (@FirstName, @LastName, @Email, @Phone, @Address, @Password, @Role, GETDATE())";

                    using (SqlCommand command = new SqlCommand(insertQuery, connection))
                    {
                        command.Parameters.AddWithValue("@FirstName", FirstName);
                        command.Parameters.AddWithValue("@LastName", LastName);
                        command.Parameters.AddWithValue("@Email", Email);
                        command.Parameters.AddWithValue("@Phone", Phone);
                        command.Parameters.AddWithValue("@Address", Address);
                        command.Parameters.AddWithValue("@Password", HashPassword(Password));
                        command.Parameters.AddWithValue("@Role", Role.ToLower()); // Ensure role is lowercase for consistency

                        command.ExecuteNonQuery();
                    }

                    // For HOD and Admin roles, we might want to add default permissions
                    if (Role.ToLower() == "hod" || Role.ToLower() == "admin")
                    {
                        await AddDefaultPermissions(connection, Email, Role.ToLower());
                    }
                }

                // Generate verification token
                string verificationToken = GenerateVerificationToken();

                // Generate verification link
                string verificationLink = Url.Page(
                    "/Auth/VerifyEmail",
                    pageHandler: null,
                    values: new { email = Email, token = verificationToken },
                    protocol: Request.Scheme);

                // Prepare role-specific welcome message
                string roleSpecificMessage = GetRoleSpecificMessage(Role);

                // Prepare email body
                string emailBody = $@"
                    <h2>Welcome to the Accreditation System, {FirstName}!</h2>
                    <p>Thank you for registering with us. Your account has been created with the role of <strong>{GetRoleName(Role)}</strong>.</p>
                    
                    {roleSpecificMessage}
                    
                    <p>Please verify your email address by clicking the link below:</p>
                    <p><a href='{verificationLink}' style='display: inline-block; background-color: #1550a6; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px;'>Verify Email Address</a></p>
                    
                    <p>If you didn't create this account, please ignore this email.</p>
                    <p>Best regards,<br>The Accreditation System Team</p>";

                // Send verification email
                await _emailService.SendEmailAsync(
                    Email,
                    "Accreditation System - Verify Your Email Address",
                    emailBody);

                successMessage = $"Account created successfully as {GetRoleName(Role)}! Please check your email to verify your account.";
                return Page();
            }
            catch (Exception ex)
            {
                errorMessage = $"Error creating account: {ex.Message}";
                return Page();
            }
        }

        private async Task AddDefaultPermissions(SqlConnection connection, string email, string role)
        {
            try
            {
                // First get the user ID
                string getUserIdQuery = "SELECT id FROM users WHERE email = @Email";
                int userId;

                using (SqlCommand getUserIdCommand = new SqlCommand(getUserIdQuery, connection))
                {
                    getUserIdCommand.Parameters.AddWithValue("@Email", email);
                    userId = Convert.ToInt32(await getUserIdCommand.ExecuteScalarAsync());
                }

                // Get default permissions based on role
                List<string> permissions = GetDefaultPermissions(role);

                foreach (var permission in permissions)
                {
                    // Check if permission exists, if not, create it
                    int permissionId = await EnsurePermissionExists(connection, permission);

                    // Add the permission to the user
                    string addPermissionQuery = @"
                        IF NOT EXISTS (SELECT 1 FROM user_permissions WHERE user_id = @UserId AND permission_id = @PermissionId)
                        BEGIN
                            INSERT INTO user_permissions (user_id, permission_id)
                            VALUES (@UserId, @PermissionId)
                        END";

                    using (SqlCommand addPermissionCommand = new SqlCommand(addPermissionQuery, connection))
                    {
                        addPermissionCommand.Parameters.AddWithValue("@UserId", userId);
                        addPermissionCommand.Parameters.AddWithValue("@PermissionId", permissionId);
                        await addPermissionCommand.ExecuteNonQueryAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                // Log the error but don't throw - we don't want to fail registration if permission setup fails
                System.Diagnostics.Debug.WriteLine($"Error setting up default permissions: {ex.Message}");
            }
        }

        private async Task<int> EnsurePermissionExists(SqlConnection connection, string permissionName)
        {
            // Check if permission exists
            string checkPermissionQuery = "SELECT id FROM permissions WHERE permission_name = @PermissionName";
            using (SqlCommand checkCommand = new SqlCommand(checkPermissionQuery, connection))
            {
                checkCommand.Parameters.AddWithValue("@PermissionName", permissionName);
                var result = await checkCommand.ExecuteScalarAsync();

                if (result != null)
                {
                    return Convert.ToInt32(result);
                }
            }

            // If not exists, create it with appropriate category
            string category = DetermineCategory(permissionName);

            string createPermissionQuery = @"
        INSERT INTO permissions (permission_name, description, category)
        VALUES (@PermissionName, @Description, @Category);
        SELECT SCOPE_IDENTITY();";

            using (SqlCommand createCommand = new SqlCommand(createPermissionQuery, connection))
            {
                createCommand.Parameters.AddWithValue("@PermissionName", permissionName);
                createCommand.Parameters.AddWithValue("@Description", $"Permission to {permissionName.Replace('.', ' ')}");
                createCommand.Parameters.AddWithValue("@Category", category);

                return Convert.ToInt32(await createCommand.ExecuteScalarAsync());
            }
        }

        private string DetermineCategory(string permissionName)
        {
            if (permissionName.StartsWith("user."))
                return "User Management";
            if (permissionName.StartsWith("system."))
                return "System Administration";
            if (permissionName.StartsWith("accreditation."))
                return "Accreditation";
            if (permissionName.StartsWith("departments."))
                return "Departments";
            if (permissionName.StartsWith("reports."))
                return "Reports";
            if (permissionName.StartsWith("schools."))
                return "Schools";
            if (permissionName.StartsWith("inspections."))
                return "Inspections";
            if (permissionName.StartsWith("claims."))
                return "Claims";

            // Default category
            return "General";
        }

        private List<string> GetDefaultPermissions(string role)
        {
            List<string> permissions = new List<string>();

            // Basic permissions that all users get
            permissions.Add("user.view_profile");
            permissions.Add("user.edit_profile");

            if (role == "admin")
            {
                // Admin gets all permissions
                permissions.Add("system.manage_settings");
                permissions.Add("users.manage");
                permissions.Add("accreditation.view");
                permissions.Add("accreditation.create");
                permissions.Add("accreditation.edit");
                permissions.Add("accreditation.approve");
                permissions.Add("accreditation.reject");
                permissions.Add("departments.view");
                permissions.Add("departments.edit");
                permissions.Add("reports.view");
                permissions.Add("reports.create");
                permissions.Add("reports.export");
                permissions.Add("schools.view");
                permissions.Add("schools.create");
                permissions.Add("schools.edit");
                permissions.Add("schools.delete");
                permissions.Add("inspections.view");
                permissions.Add("inspections.create");
                permissions.Add("claims.view");
                permissions.Add("claims.process");
            }
            else if (role == "hod")
            {
                // HOD gets department-specific permissions
                permissions.Add("accreditation.view");
                permissions.Add("accreditation.approve");
                permissions.Add("accreditation.reject");
                permissions.Add("departments.view");
                permissions.Add("reports.view");
                permissions.Add("reports.create");
                permissions.Add("schools.view");
                permissions.Add("inspections.view");
                permissions.Add("claims.view");
                permissions.Add("claims.process");
            }
            else if (role == "client")
            {
                // Client gets client-specific permissions
                permissions.Add("accreditation.view");
                permissions.Add("accreditation.create");
                permissions.Add("schools.view");
            }

            return permissions;
        }

        private string GetRoleSpecificMessage(string role)
        {
            switch (role.ToLower())
            {
                case "admin":
                    return "<p>As an administrator, you have full access to the system and can manage all aspects of the accreditation process.</p>";
                case "hod":
                    return "<p>As a Head of Department (HOD), you can review and process accreditation claims, access department information, and generate reports.</p>";
                case "client":
                    return "<p>As a client, you can submit accreditation claims for your school and track their status through our system.</p>";
                default:
                    return "";
            }
        }

        private string GetRoleName(string role)
        {
            switch (role.ToLower())
            {
                case "admin":
                    return "Administrator";
                case "hod":
                    return "Head of Department (HOD)";
                case "client":
                    return "Client";
                default:
                    return role;
            }
        }

        private string GenerateVerificationToken()
        {
            using (var rng = RandomNumberGenerator.Create())
            {
                var tokenData = new byte[32];
                rng.GetBytes(tokenData);
                return Convert.ToBase64String(tokenData)
                    .Replace("/", "_")
                    .Replace("+", "-")
                    .Replace("=", "");
            }
        }

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
