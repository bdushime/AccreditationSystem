using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace AccreditationSystem.Pages.Services
{
    public interface IAuditLogService
    {
        Task LogActionAsync(int userId, string actionType, string actionDetails);
        Task<List<AuditLogEntry>> GetRecentAuditLogsAsync(int count);
        Task<List<AuditLogEntry>> GetUserAuditLogsAsync(int userId, int count);
        Task<List<AuditLogEntry>> GetAuditLogsByTypeAsync(string actionType, int count);
        Task<List<AuditLogEntry>> GetAuditLogsByDateRangeAsync(DateTime startDate, DateTime endDate, int maxCount = 1000);
    }

    public class AuditLogService : IAuditLogService
    {
        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuditLogService(IConfiguration configuration, IHttpContextAccessor httpContextAccessor)
        {
            _configuration = configuration;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task LogActionAsync(int userId, string actionType, string actionDetails)
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                string query = @"
                    INSERT INTO audit_logs 
                        (user_id, action_type, action_details, ip_address, timestamp) 
                    VALUES 
                        (@UserId, @ActionType, @ActionDetails, @IpAddress, GETDATE())";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@UserId", userId);
                    command.Parameters.AddWithValue("@ActionType", actionType);
                    command.Parameters.AddWithValue("@ActionDetails", actionDetails);

                    // Try to get user IP address
                    string ipAddress = "0.0.0.0";
                    if (_httpContextAccessor?.HttpContext != null)
                    {
                        ipAddress = _httpContextAccessor.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "0.0.0.0";
                    }
                    command.Parameters.AddWithValue("@IpAddress", ipAddress);

                    await command.ExecuteNonQueryAsync();
                }
            }
        }

        public async Task<List<AuditLogEntry>> GetRecentAuditLogsAsync(int count)
        {
            List<AuditLogEntry> logs = new List<AuditLogEntry>();
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                string query = @"
                    SELECT TOP (@Count)
                        al.id, al.user_id, al.action_type, al.action_details, 
                        al.ip_address, al.timestamp, u.firstname, u.lastname
                    FROM 
                        audit_logs al
                    JOIN 
                        users u ON al.user_id = u.id
                    ORDER BY 
                        al.timestamp DESC";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Count", count);

                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            logs.Add(new AuditLogEntry
                            {
                                Id = Convert.ToInt32(reader["id"]),
                                UserId = Convert.ToInt32(reader["user_id"]),
                                UserName = $"{reader["firstname"]} {reader["lastname"]}",
                                ActionType = reader["action_type"].ToString(),
                                ActionDetails = reader["action_details"].ToString(),
                                IpAddress = reader["ip_address"].ToString(),
                                Timestamp = Convert.ToDateTime(reader["timestamp"])
                            });
                        }
                    }
                }
            }

            return logs;
        }

        public async Task<List<AuditLogEntry>> GetUserAuditLogsAsync(int userId, int count)
        {
            List<AuditLogEntry> logs = new List<AuditLogEntry>();
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                string query = @"
                    SELECT TOP (@Count)
                        al.id, al.user_id, al.action_type, al.action_details, 
                        al.ip_address, al.timestamp, u.firstname, u.lastname
                    FROM 
                        audit_logs al
                    JOIN 
                        users u ON al.user_id = u.id
                    WHERE
                        al.user_id = @UserId
                    ORDER BY 
                        al.timestamp DESC";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Count", count);
                    command.Parameters.AddWithValue("@UserId", userId);

                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            logs.Add(new AuditLogEntry
                            {
                                Id = Convert.ToInt32(reader["id"]),
                                UserId = Convert.ToInt32(reader["user_id"]),
                                UserName = $"{reader["firstname"]} {reader["lastname"]}",
                                ActionType = reader["action_type"].ToString(),
                                ActionDetails = reader["action_details"].ToString(),
                                IpAddress = reader["ip_address"].ToString(),
                                Timestamp = Convert.ToDateTime(reader["timestamp"])
                            });
                        }
                    }
                }
            }

            return logs;
        }

        public async Task<List<AuditLogEntry>> GetAuditLogsByTypeAsync(string actionType, int count)
        {
            List<AuditLogEntry> logs = new List<AuditLogEntry>();
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                string query = @"
                    SELECT TOP (@Count)
                        al.id, al.user_id, al.action_type, al.action_details, 
                        al.ip_address, al.timestamp, u.firstname, u.lastname
                    FROM 
                        audit_logs al
                    JOIN 
                        users u ON al.user_id = u.id
                    WHERE
                        al.action_type = @ActionType
                    ORDER BY 
                        al.timestamp DESC";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Count", count);
                    command.Parameters.AddWithValue("@ActionType", actionType);

                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            logs.Add(new AuditLogEntry
                            {
                                Id = Convert.ToInt32(reader["id"]),
                                UserId = Convert.ToInt32(reader["user_id"]),
                                UserName = $"{reader["firstname"]} {reader["lastname"]}",
                                ActionType = reader["action_type"].ToString(),
                                ActionDetails = reader["action_details"].ToString(),
                                IpAddress = reader["ip_address"].ToString(),
                                Timestamp = Convert.ToDateTime(reader["timestamp"])
                            });
                        }
                    }
                }
            }

            return logs;
        }

        public async Task<List<AuditLogEntry>> GetAuditLogsByDateRangeAsync(DateTime startDate, DateTime endDate, int maxCount = 1000)
        {
            List<AuditLogEntry> logs = new List<AuditLogEntry>();
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                string query = @"
                    SELECT TOP (@MaxCount)
                        al.id, al.user_id, al.action_type, al.action_details, 
                        al.ip_address, al.timestamp, u.firstname, u.lastname
                    FROM 
                        audit_logs al
                    JOIN 
                        users u ON al.user_id = u.id
                    WHERE
                        al.timestamp BETWEEN @StartDate AND @EndDate
                    ORDER BY 
                        al.timestamp DESC";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@MaxCount", maxCount);
                    command.Parameters.AddWithValue("@StartDate", startDate);
                    command.Parameters.AddWithValue("@EndDate", endDate);

                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            logs.Add(new AuditLogEntry
                            {
                                Id = Convert.ToInt32(reader["id"]),
                                UserId = Convert.ToInt32(reader["user_id"]),
                                UserName = $"{reader["firstname"]} {reader["lastname"]}",
                                ActionType = reader["action_type"].ToString(),
                                ActionDetails = reader["action_details"].ToString(),
                                IpAddress = reader["ip_address"].ToString(),
                                Timestamp = Convert.ToDateTime(reader["timestamp"])
                            });
                        }
                    }
                }
            }

            return logs;
        }
    }

    public class AuditLogEntry
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; }
        public string ActionType { get; set; }
        public string ActionDetails { get; set; }
        public string IpAddress { get; set; }
        public DateTime Timestamp { get; set; }
    }
}