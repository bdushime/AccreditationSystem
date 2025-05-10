using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace AccreditationSystem.Pages.Services
{
    public interface IPermissionService
    {
        Task<bool> UserHasPermissionAsync(int userId, string permissionName);
        Task<List<string>> GetUserPermissionsAsync(int userId);
        Task<bool> GrantPermissionAsync(int userId, string permissionName);
        Task<bool> RevokePermissionAsync(int userId, string permissionName);
        Task<bool> UpdateUserPermissionsAsync(int userId, List<string> permissionNames);
        Task<List<Permission>> GetAllPermissionsAsync();
        Task<List<PermissionCategory>> GetPermissionCategoriesAsync();
        Task<bool> CreatePermissionAsync(string name, string description, string category);
    }

    public class PermissionService : IPermissionService
    {
        private readonly IConfiguration _configuration;
        private readonly IAuditLogService _auditLogService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public PermissionService(
            IConfiguration configuration,
            IAuditLogService auditLogService,
            IHttpContextAccessor httpContextAccessor)
        {
            _configuration = configuration;
            _auditLogService = auditLogService;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<bool> UserHasPermissionAsync(int userId, string permissionName)
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                // Check if user has the admin role, which bypasses permission checks
                string roleQuery = "SELECT role FROM users WHERE id = @UserId";
                using (SqlCommand roleCommand = new SqlCommand(roleQuery, connection))
                {
                    roleCommand.Parameters.AddWithValue("@UserId", userId);
                    string role = (string)await roleCommand.ExecuteScalarAsync();

                    if (role?.ToLower() == "admin")
                    {
                        return true;
                    }
                }

                string query = @"
                    SELECT COUNT(*) 
                    FROM user_permissions up
                    JOIN permissions p ON up.permission_id = p.id
                    WHERE up.user_id = @UserId AND p.permission_name = @PermissionName";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@UserId", userId);
                    command.Parameters.AddWithValue("@PermissionName", permissionName);

                    int count = Convert.ToInt32(await command.ExecuteScalarAsync());
                    return count > 0;
                }
            }
        }

        public async Task<List<string>> GetUserPermissionsAsync(int userId)
        {
            List<string> permissions = new List<string>();
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                // Check if user has the admin role
                string roleQuery = "SELECT role FROM users WHERE id = @UserId";
                using (SqlCommand roleCommand = new SqlCommand(roleQuery, connection))
                {
                    roleCommand.Parameters.AddWithValue("@UserId", userId);
                    string role = (string)await roleCommand.ExecuteScalarAsync();

                    if (role?.ToLower() == "admin")
                    {
                        // Admin has all permissions - get all permission names
                        string allPermissionsQuery = "SELECT permission_name FROM permissions";
                        using (SqlCommand allPermissionsCommand = new SqlCommand(allPermissionsQuery, connection))
                        {
                            using (SqlDataReader reader = await allPermissionsCommand.ExecuteReaderAsync())
                            {
                                while (await reader.ReadAsync())
                                {
                                    permissions.Add(reader["permission_name"].ToString());
                                }
                            }
                        }

                        return permissions;
                    }
                }

                string query = @"
                    SELECT p.permission_name
                    FROM user_permissions up
                    JOIN permissions p ON up.permission_id = p.id
                    WHERE up.user_id = @UserId
                    ORDER BY p.permission_name";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@UserId", userId);

                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            permissions.Add(reader["permission_name"].ToString());
                        }
                    }
                }
            }

            return permissions;
        }

        public async Task<bool> GrantPermissionAsync(int userId, string permissionName)
        {
            try
            {
                string connectionString = _configuration.GetConnectionString("DefaultConnection");
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    // First, get the permission ID
                    int permissionId;
                    string getPermissionQuery = "SELECT id FROM permissions WHERE permission_name = @PermissionName";
                    using (SqlCommand command = new SqlCommand(getPermissionQuery, connection))
                    {
                        command.Parameters.AddWithValue("@PermissionName", permissionName);
                        object result = await command.ExecuteScalarAsync();

                        if (result == null)
                        {
                            return false; // Permission doesn't exist
                        }

                        permissionId = Convert.ToInt32(result);
                    }

                    // Check if the user already has this permission
                    string checkQuery = @"
                        SELECT COUNT(*) 
                        FROM user_permissions 
                        WHERE user_id = @UserId AND permission_id = @PermissionId";

                    using (SqlCommand command = new SqlCommand(checkQuery, connection))
                    {
                        command.Parameters.AddWithValue("@UserId", userId);
                        command.Parameters.AddWithValue("@PermissionId", permissionId);

                        int count = Convert.ToInt32(await command.ExecuteScalarAsync());

                        if (count > 0)
                        {
                            return true; // User already has this permission
                        }
                    }

                    // Grant the permission
                    string grantQuery = @"
                        INSERT INTO user_permissions (user_id, permission_id) 
                        VALUES (@UserId, @PermissionId)";

                    using (SqlCommand command = new SqlCommand(grantQuery, connection))
                    {
                        command.Parameters.AddWithValue("@UserId", userId);
                        command.Parameters.AddWithValue("@PermissionId", permissionId);

                        await command.ExecuteNonQueryAsync();
                    }

                    // Log the action
                    int currentUserId = _httpContextAccessor.HttpContext.Session.GetInt32("UserId") ?? 0;
                    await _auditLogService.LogActionAsync(
                        currentUserId,
                        "PermissionGrant",
                        $"Granted permission '{permissionName}' to user ID {userId}"
                    );

                    return true;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> RevokePermissionAsync(int userId, string permissionName)
        {
            try
            {
                string connectionString = _configuration.GetConnectionString("DefaultConnection");
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    // Get the permission ID
                    int permissionId;
                    string getPermissionQuery = "SELECT id FROM permissions WHERE permission_name = @PermissionName";
                    using (SqlCommand command = new SqlCommand(getPermissionQuery, connection))
                    {
                        command.Parameters.AddWithValue("@PermissionName", permissionName);
                        object result = await command.ExecuteScalarAsync();

                        if (result == null)
                        {
                            return false; // Permission doesn't exist
                        }

                        permissionId = Convert.ToInt32(result);
                    }

                    // Revoke the permission
                    string revokeQuery = @"
                        DELETE FROM user_permissions 
                        WHERE user_id = @UserId AND permission_id = @PermissionId";

                    using (SqlCommand command = new SqlCommand(revokeQuery, connection))
                    {
                        command.Parameters.AddWithValue("@UserId", userId);
                        command.Parameters.AddWithValue("@PermissionId", permissionId);

                        await command.ExecuteNonQueryAsync();
                    }

                    // Log the action
                    int currentUserId = _httpContextAccessor.HttpContext.Session.GetInt32("UserId") ?? 0;
                    await _auditLogService.LogActionAsync(
                        currentUserId,
                        "PermissionRevoke",
                        $"Revoked permission '{permissionName}' from user ID {userId}"
                    );

                    return true;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> UpdateUserPermissionsAsync(int userId, List<string> permissionNames)
        {
            try
            {
                string connectionString = _configuration.GetConnectionString("DefaultConnection");
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    using (SqlTransaction transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            // First, remove all existing permissions for the user
                            string deleteQuery = "DELETE FROM user_permissions WHERE user_id = @UserId";
                            using (SqlCommand command = new SqlCommand(deleteQuery, connection, transaction))
                            {
                                command.Parameters.AddWithValue("@UserId", userId);
                                await command.ExecuteNonQueryAsync();
                            }

                            // Then, add the new permissions
                            if (permissionNames != null && permissionNames.Count > 0)
                            {
                                foreach (var permissionName in permissionNames)
                                {
                                    // Get the permission ID
                                    int permissionId;
                                    string getPermissionQuery = "SELECT id FROM permissions WHERE permission_name = @PermissionName";
                                    using (SqlCommand command = new SqlCommand(getPermissionQuery, connection, transaction))
                                    {
                                        command.Parameters.AddWithValue("@PermissionName", permissionName);
                                        object result = await command.ExecuteScalarAsync();

                                        if (result == null)
                                        {
                                            continue; // Skip permissions that don't exist
                                        }

                                        permissionId = Convert.ToInt32(result);
                                    }

                                    // Add the permission
                                    string insertQuery = "INSERT INTO user_permissions (user_id, permission_id) VALUES (@UserId, @PermissionId)";
                                    using (SqlCommand command = new SqlCommand(insertQuery, connection, transaction))
                                    {
                                        command.Parameters.AddWithValue("@UserId", userId);
                                        command.Parameters.AddWithValue("@PermissionId", permissionId);
                                        await command.ExecuteNonQueryAsync();
                                    }
                                }
                            }

                            // Commit the transaction
                            transaction.Commit();

                            // Log the action
                            int currentUserId = _httpContextAccessor.HttpContext.Session.GetInt32("UserId") ?? 0;
                            await _auditLogService.LogActionAsync(
                                currentUserId,
                                "PermissionsUpdate",
                                $"Updated permissions for user ID {userId}"
                            );

                            return true;
                        }
                        catch (Exception)
                        {
                            // Rollback the transaction if an error occurs
                            transaction.Rollback();
                            throw;
                        }
                    }
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<List<Permission>> GetAllPermissionsAsync()
        {
            List<Permission> permissions = new List<Permission>();
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                string query = @"
                    SELECT id, permission_name, description, category
                    FROM permissions
                    ORDER BY category, permission_name";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            permissions.Add(new Permission
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

            return permissions;
        }

        public async Task<List<PermissionCategory>> GetPermissionCategoriesAsync()
        {
            List<PermissionCategory> categories = new List<PermissionCategory>();
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                // First, get all distinct categories
                string categoryQuery = @"
                    SELECT DISTINCT category
                    FROM permissions
                    ORDER BY category";

                using (SqlCommand command = new SqlCommand(categoryQuery, connection))
                {
                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            categories.Add(new PermissionCategory
                            {
                                Name = reader["category"].ToString(),
                                Permissions = new List<Permission>()
                            });
                        }
                    }
                }

                // Then, get permissions for each category
                foreach (var category in categories)
                {
                    string permissionsQuery = @"
                        SELECT id, permission_name, description
                        FROM permissions
                        WHERE category = @Category
                        ORDER BY permission_name";

                    using (SqlCommand command = new SqlCommand(permissionsQuery, connection))
                    {
                        command.Parameters.AddWithValue("@Category", category.Name);

                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                category.Permissions.Add(new Permission
                                {
                                    Id = Convert.ToInt32(reader["id"]),
                                    Name = reader["permission_name"].ToString(),
                                    Description = reader["description"].ToString(),
                                    Category = category.Name
                                });
                            }
                        }
                    }
                }
            }

            return categories;
        }

        public async Task<bool> CreatePermissionAsync(string name, string description, string category)
        {
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
                            return false; // Permission already exists
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

                    // Log the action
                    int currentUserId = _httpContextAccessor.HttpContext.Session.GetInt32("UserId") ?? 0;
                    await _auditLogService.LogActionAsync(
                        currentUserId,
                        "PermissionCreate",
                        $"Created new permission '{name}' in category '{category}'"
                    );

                    return true;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }
    }

    public class Permission
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Category { get; set; }
    }

    public class PermissionCategory
    {
        public string Name { get; set; }
        public List<Permission> Permissions { get; set; }
    }
}