using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Collections.Generic;
using System.Threading.Tasks;
using AccreditationSystem.Pages.Services;

namespace AccreditationSystem.Controllers
{
    [ApiController]
    [Route("api/users")]
    public class UsersApiController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly IAuditLogService _auditLogService;

        public UsersApiController(
            IConfiguration configuration,
            IAuditLogService auditLogService)
        {
            _configuration = configuration;
            _auditLogService = auditLogService;
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<UserDto>> GetUser(int id)
        {
            // Check if the current user is authenticated and has admin permissions
            if (HttpContext.Session.GetInt32("UserId") == null ||
                HttpContext.Session.GetString("UserRole") != "admin")
            {
                return Unauthorized();
            }

            string connectionString = _configuration.GetConnectionString("DefaultConnection");
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                string query = @"
                    SELECT 
                        u.id, u.firstname, u.lastname, u.email, u.role, 
                        u.is_active, u.last_login, u.phone, u.address,
                        STRING_AGG(p.permission_name, ', ') as permissions
                    FROM 
                        users u
                    LEFT JOIN 
                        user_permissions up ON u.id = up.user_id
                    LEFT JOIN 
                        permissions p ON up.permission_id = p.id
                    WHERE 
                        u.id = @UserId
                    GROUP BY 
                        u.id, u.firstname, u.lastname, u.email, u.role, 
                        u.is_active, u.last_login, u.phone, u.address";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@UserId", id);

                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            var user = new UserDto
                            {
                                Id = id,
                                FirstName = reader["firstname"].ToString(),
                                LastName = reader["lastname"].ToString(),
                                Email = reader["email"].ToString(),
                                Role = reader["role"].ToString(),
                                IsActive = reader["is_active"] != System.DBNull.Value ? Convert.ToBoolean(reader["is_active"]) : true,
                                LastLogin = reader["last_login"] != System.DBNull.Value ? Convert.ToDateTime(reader["last_login"]) : null,
                                Phone = reader["phone"].ToString(),
                                Address = reader["address"].ToString(),
                                Permissions = reader["permissions"] != System.DBNull.Value ? reader["permissions"].ToString() : null
                            };

                            return user;
                        }
                    }
                }
            }

            return NotFound();
        }

        [HttpGet("{id}/permissions")]
        public async Task<ActionResult<List<int>>> GetUserPermissionIds(int id)
        {
            // Check if the current user is authenticated and has admin permissions
            if (HttpContext.Session.GetInt32("UserId") == null ||
                HttpContext.Session.GetString("UserRole") != "admin")
            {
                return Unauthorized();
            }

            List<int> permissionIds = new List<int>();
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                string query = @"
                    SELECT permission_id
                    FROM user_permissions
                    WHERE user_id = @UserId";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@UserId", id);

                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            permissionIds.Add(Convert.ToInt32(reader["permission_id"]));
                        }
                    }
                }
            }

            return permissionIds;
        }

        [HttpGet("{id}/activity")]
        public async Task<ActionResult<List<AuditLogEntry>>> GetUserActivity(int id)
        {
            // Check if the current user is authenticated and has admin permissions
            if (HttpContext.Session.GetInt32("UserId") == null ||
                HttpContext.Session.GetString("UserRole") != "admin")
            {
                return Unauthorized();
            }

            var logs = await _auditLogService.GetUserAuditLogsAsync(id, 20);
            return logs;
        }
    }

    public class UserDto
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Role { get; set; }
        public bool IsActive { get; set; }
        public DateTime? LastLogin { get; set; }
        public string Phone { get; set; }
        public string Address { get; set; }
        public string Permissions { get; set; }
    }
}