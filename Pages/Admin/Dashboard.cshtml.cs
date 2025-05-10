using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Threading.Tasks;
using AccreditationSystem.Pages.Services;

namespace AccreditationSystem.Pages.Admin
{
    public class DashboardModel : PageModel
    {
        private readonly IConfiguration _configuration;
        private readonly IAuditLogService _auditLogService;

        public DashboardModel(IConfiguration configuration, IAuditLogService auditLogService)
        {
            _configuration = configuration;
            _auditLogService = auditLogService;
        }

        public List<UserViewModel> Users { get; set; } = new List<UserViewModel>();
        public List<PermissionViewModel> AvailablePermissions { get; set; } = new List<PermissionViewModel>();
        public Dictionary<string, int> UserRoleDistribution { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> SystemActivityByDay { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> MostActiveUsers { get; set; } = new Dictionary<string, int>();
        public List<AuditLogEntry> RecentAuditLogs { get; set; } = new List<AuditLogEntry>();

        public string ErrorMessage { get; set; } = "";
        public string SuccessMessage { get; set; } = "";

        public async Task<IActionResult> OnGetAsync()
        {
            // Check if user is authenticated and has admin permissions
            if (HttpContext.Session.GetInt32("UserId") == null ||
                HttpContext.Session.GetString("UserRole") != "admin")
            {
                return RedirectToPage("/Auth/Login", new { returnUrl = "/Admin/Dashboard" });
            }

            await LoadUsers();
            await LoadAvailablePermissions();
            await LoadDashboardStatistics();
            await LoadAuditLogs();

            return Page();
        }

        private async Task LoadUsers()
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                string query = @"
                    SELECT 
                        u.id, u.firstname, u.lastname, u.email, u.role, u.is_active, u.last_login,
                        STRING_AGG(p.permission_name, ', ') as permissions
                    FROM 
                        users u
                    LEFT JOIN 
                        user_permissions up ON u.id = up.user_id
                    LEFT JOIN 
                        permissions p ON up.permission_id = p.id
                    GROUP BY 
                        u.id, u.firstname, u.lastname, u.email, u.role, u.is_active, u.last_login
                    ORDER BY 
                        u.lastname, u.firstname";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            Users.Add(new UserViewModel
                            {
                                Id = Convert.ToInt32(reader["id"]),
                                FirstName = reader["firstname"].ToString(),
                                LastName = reader["lastname"].ToString(),
                                Email = reader["email"].ToString(),
                                Role = reader["role"].ToString(),
                                IsActive = reader["is_active"] != DBNull.Value ? Convert.ToBoolean(reader["is_active"]) : true,
                                LastLogin = reader["last_login"] != DBNull.Value ? Convert.ToDateTime(reader["last_login"]) : null,
                                Permissions = reader["permissions"] != DBNull.Value ? reader["permissions"].ToString() : ""
                            });
                        }
                    }
                }
            }
        }

        private async Task LoadAvailablePermissions()
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                string query = "SELECT id, permission_name, description, category FROM permissions ORDER BY category, permission_name";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            AvailablePermissions.Add(new PermissionViewModel
                            {
                                Id = Convert.ToInt32(reader["id"]),
                                Name = reader["permission_name"].ToString(),
                                Description = reader["description"].ToString(),
                                Category = reader["category"].ToString()
                            });
                        }
                    }
                }
            }
        }

        private async Task LoadDashboardStatistics()
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                // User role distribution
                string roleQuery = "SELECT role, COUNT(*) as count FROM users GROUP BY role";
                using (SqlCommand command = new SqlCommand(roleQuery, connection))
                {
                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            UserRoleDistribution.Add(
                                reader["role"].ToString(),
                                Convert.ToInt32(reader["count"])
                            );
                        }
                    }
                }

                // System activity by day (last 7 days)
                string activityQuery = @"
                    SELECT 
                        CONVERT(date, timestamp) as day, 
                        COUNT(*) as count 
                    FROM 
                        audit_logs 
                    WHERE 
                        timestamp >= DATEADD(day, -7, GETDATE()) 
                    GROUP BY 
                        CONVERT(date, timestamp) 
                    ORDER BY 
                        day";

                using (SqlCommand command = new SqlCommand(activityQuery, connection))
                {
                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            SystemActivityByDay.Add(
                                ((DateTime)reader["day"]).ToString("yyyy-MM-dd"),
                                Convert.ToInt32(reader["count"])
                            );
                        }
                    }
                }

                // Most active users (top 5)
                string activeUsersQuery = @"
                    SELECT TOP 5
                        u.firstname + ' ' + u.lastname as user_name,
                        COUNT(al.id) as activity_count
                    FROM 
                        audit_logs al
                    JOIN 
                        users u ON al.user_id = u.id
                    WHERE 
                        al.timestamp >= DATEADD(day, -30, GETDATE())
                    GROUP BY 
                        u.firstname, u.lastname
                    ORDER BY 
                        activity_count DESC";

                using (SqlCommand command = new SqlCommand(activeUsersQuery, connection))
                {
                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            MostActiveUsers.Add(
                                reader["user_name"].ToString(),
                                Convert.ToInt32(reader["activity_count"])
                            );
                        }
                    }
                }
            }
        }

        private async Task LoadAuditLogs()
        {
            // Get most recent 10 audit logs
            RecentAuditLogs = await _auditLogService.GetRecentAuditLogsAsync(10);
        }

        public async Task<IActionResult> OnPostUpdateUserStatusAsync(int userId, bool isActive)
        {
            // Check if user is authenticated and has admin permissions
            if (HttpContext.Session.GetInt32("UserId") == null ||
                HttpContext.Session.GetString("UserRole") != "admin")
            {
                return RedirectToPage("/Auth/Login", new { returnUrl = "/Admin/Dashboard" });
            }

            try
            {
                string connectionString = _configuration.GetConnectionString("DefaultConnection");
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    string query = "UPDATE users SET is_active = @IsActive WHERE id = @UserId";
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@UserId", userId);
                        command.Parameters.AddWithValue("@IsActive", isActive);
                        await command.ExecuteNonQueryAsync();
                    }
                }

                // Log the action
                await _auditLogService.LogActionAsync(
                    HttpContext.Session.GetInt32("UserId").Value,
                    "UserStatusUpdate",
                    $"Updated user ID {userId} status to {(isActive ? "active" : "inactive")}"
                );

                SuccessMessage = "User status updated successfully";
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Failed to update user status: {ex.Message}";
            }

            // Reload the page data
            await LoadUsers();
            await LoadAvailablePermissions();
            await LoadDashboardStatistics();
            await LoadAuditLogs();

            return Page();
        }

        public async Task<IActionResult> OnPostUpdateUserPermissionsAsync(int userId, List<int> permissionIds)
        {
            // Check if user is authenticated and has admin permissions
            if (HttpContext.Session.GetInt32("UserId") == null ||
                HttpContext.Session.GetString("UserRole") != "admin")
            {
                return RedirectToPage("/Auth/Login", new { returnUrl = "/Admin/Dashboard" });
            }

            try
            {
                string connectionString = _configuration.GetConnectionString("DefaultConnection");
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    // First, remove all existing permissions for the user
                    string deleteQuery = "DELETE FROM user_permissions WHERE user_id = @UserId";
                    using (SqlCommand command = new SqlCommand(deleteQuery, connection))
                    {
                        command.Parameters.AddWithValue("@UserId", userId);
                        await command.ExecuteNonQueryAsync();
                    }

                    // Then, add the new permissions
                    if (permissionIds != null && permissionIds.Count > 0)
                    {
                        foreach (var permissionId in permissionIds)
                        {
                            string insertQuery = "INSERT INTO user_permissions (user_id, permission_id) VALUES (@UserId, @PermissionId)";
                            using (SqlCommand command = new SqlCommand(insertQuery, connection))
                            {
                                command.Parameters.AddWithValue("@UserId", userId);
                                command.Parameters.AddWithValue("@PermissionId", permissionId);
                                await command.ExecuteNonQueryAsync();
                            }
                        }
                    }
                }

                // Log the action
                await _auditLogService.LogActionAsync(
                    HttpContext.Session.GetInt32("UserId").Value,
                    "UserPermissionsUpdate",
                    $"Updated permissions for user ID {userId}"
                );

                SuccessMessage = "User permissions updated successfully";
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Failed to update user permissions: {ex.Message}";
            }

            // Reload the page data
            await LoadUsers();
            await LoadAvailablePermissions();
            await LoadDashboardStatistics();
            await LoadAuditLogs();

            return Page();
        }

        public async Task<IActionResult> OnPostAddPermissionAsync(string name, string description, string category)
        {
            // Check if user is authenticated and has admin permissions
            if (HttpContext.Session.GetInt32("UserId") == null ||
                HttpContext.Session.GetString("UserRole") != "admin")
            {
                return RedirectToPage("/Auth/Login", new { returnUrl = "/Admin/Dashboard" });
            }

            try
            {
                string connectionString = _configuration.GetConnectionString("DefaultConnection");
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    // Check if the permission already exists
                    string checkQuery = "SELECT COUNT(*) FROM permissions WHERE permission_name = @Name";
                    using (SqlCommand command = new SqlCommand(checkQuery, connection))
                    {
                        command.Parameters.AddWithValue("@Name", name);
                        int count = Convert.ToInt32(await command.ExecuteScalarAsync());

                        if (count > 0)
                        {
                            ErrorMessage = $"Permission '{name}' already exists";
                            return await OnGetAsync();
                        }
                    }

                    // Create the new permission
                    string insertQuery = @"
                        INSERT INTO permissions (permission_name, description, category) 
                        VALUES (@Name, @Description, @Category)";

                    using (SqlCommand command = new SqlCommand(insertQuery, connection))
                    {
                        command.Parameters.AddWithValue("@Name", name);
                        command.Parameters.AddWithValue("@Description", description);
                        command.Parameters.AddWithValue("@Category", category);

                        await command.ExecuteNonQueryAsync();
                    }
                }

                // Log the action
                await _auditLogService.LogActionAsync(
                    HttpContext.Session.GetInt32("UserId").Value,
                    "PermissionCreate",
                    $"Created new permission '{name}' in category '{category}'"
                );

                SuccessMessage = "Permission added successfully";
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Failed to add permission: {ex.Message}";
            }

            return await OnGetAsync();
        }

        public async Task<IActionResult> OnPostEditPermissionAsync(int id, string name, string description, string category)
        {
            // Check if user is authenticated and has admin permissions
            if (HttpContext.Session.GetInt32("UserId") == null ||
                HttpContext.Session.GetString("UserRole") != "admin")
            {
                return RedirectToPage("/Auth/Login", new { returnUrl = "/Admin/Dashboard" });
            }

            try
            {
                string connectionString = _configuration.GetConnectionString("DefaultConnection");
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    // Check if the permission name already exists for a different ID
                    string checkQuery = "SELECT COUNT(*) FROM permissions WHERE permission_name = @Name AND id != @Id";
                    using (SqlCommand command = new SqlCommand(checkQuery, connection))
                    {
                        command.Parameters.AddWithValue("@Name", name);
                        command.Parameters.AddWithValue("@Id", id);
                        int count = Convert.ToInt32(await command.ExecuteScalarAsync());

                        if (count > 0)
                        {
                            ErrorMessage = $"Permission '{name}' already exists";
                            return await OnGetAsync();
                        }
                    }

                    // Update the permission
                    string updateQuery = @"
                        UPDATE permissions 
                        SET permission_name = @Name, description = @Description, category = @Category
                        WHERE id = @Id";

                    using (SqlCommand command = new SqlCommand(updateQuery, connection))
                    {
                        command.Parameters.AddWithValue("@Id", id);
                        command.Parameters.AddWithValue("@Name", name);
                        command.Parameters.AddWithValue("@Description", description);
                        command.Parameters.AddWithValue("@Category", category);

                        await command.ExecuteNonQueryAsync();
                    }
                }

                // Log the action
                await _auditLogService.LogActionAsync(
                    HttpContext.Session.GetInt32("UserId").Value,
                    "PermissionEdit",
                    $"Updated permission ID {id} to '{name}' in category '{category}'"
                );

                SuccessMessage = "Permission updated successfully";
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Failed to update permission: {ex.Message}";
            }

            return await OnGetAsync();
        }
    }

    public class UserViewModel
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Role { get; set; }
        public bool IsActive { get; set; }
        public DateTime? LastLogin { get; set; }
        public string Permissions { get; set; }

        public string FullName => $"{FirstName} {LastName}";
    }

    public class PermissionViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Category { get; set; }
    }
}