using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Threading.Tasks;

namespace AccreditationSystem.Services
{
    public class DocumentValidationService
    {
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _environment;

        public DocumentValidationService(IConfiguration configuration, IWebHostEnvironment environment)
        {
            _configuration = configuration;
            _environment = environment;
        }

        /// <summary>
        /// Validates a document against its stored hash in the database
        /// </summary>
        /// <param name="documentId">The ID of the document in the DocumentHashes table</param>
        /// <param name="file">The file to validate</param>
        /// <returns>True if the document is valid, false otherwise</returns>
        public async Task<bool> ValidateDocumentAsync(int documentId, IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return false;
            }

            try
            {
                // Get the stored hash from the database
                string storedHash = await GetStoredHashAsync(documentId);
                if (string.IsNullOrEmpty(storedHash))
                {
                    return false;
                }

                // Generate hash for the current file
                string currentHash = await Utilities.DocumentHasher.GenerateFileHashAsync(file);

                // Compare the hashes
                return string.Equals(storedHash, currentHash, StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Validates a document from an accreditation claim
        /// </summary>
        /// <param name="claimId">The claim ID</param>
        /// <param name="documentType">The type of document (self-assessment, curriculum, etc.)</param>
        /// <param name="file">The file to validate</param>
        /// <returns>True if the document is valid, false otherwise</returns>
        public async Task<bool> ValidateClaimDocumentAsync(int claimId, string documentType, IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return false;
            }

            try
            {
                // Get the stored hash from the AccreditationClaims table
                string storedHash = await GetClaimDocumentHashAsync(claimId, documentType);
                if (string.IsNullOrEmpty(storedHash))
                {
                    return false;
                }

                // Generate hash for the current file
                string currentHash = await Utilities.DocumentHasher.GenerateFileHashAsync(file);

                // Compare the hashes
                return string.Equals(storedHash, currentHash, StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Stores a document hash in the DocumentHashes table
        /// </summary>
        /// <param name="hash">The document hash</param>
        /// <param name="fileName">Original file name</param>
        /// <param name="filePath">File path on the server</param>
        /// <param name="fileType">Type of document</param>
        /// <param name="fileSize">Size of the file in bytes</param>
        /// <param name="uploadedBy">User who uploaded the document</param>
        /// <param name="relatedEntityType">Type of related entity (e.g., "Claim", "School")</param>
        /// <param name="relatedEntityId">ID of the related entity</param>
        /// <returns>The ID of the newly created record</returns>
        public async Task<int> StoreDocumentHashAsync(
            string hash,
            string fileName,
            string filePath,
            string fileType,
            long fileSize,
            string uploadedBy = null,
            string relatedEntityType = null,
            int? relatedEntityId = null)
        {
            using (SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                await connection.OpenAsync();

                string sql = @"
                    INSERT INTO DocumentHashes (
                        DocumentHash, FileName, FilePath, FileType, FileSize, UploadDate, 
                        UploadedBy, RelatedEntityType, RelatedEntityId
                    ) VALUES (
                        @DocumentHash, @FileName, @FilePath, @FileType, @FileSize, GETDATE(), 
                        @UploadedBy, @RelatedEntityType, @RelatedEntityId
                    );
                    SELECT SCOPE_IDENTITY();";

                using (SqlCommand command = new SqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@DocumentHash", hash);
                    command.Parameters.AddWithValue("@FileName", fileName);
                    command.Parameters.AddWithValue("@FilePath", filePath);
                    command.Parameters.AddWithValue("@FileType", fileType);
                    command.Parameters.AddWithValue("@FileSize", fileSize);
                    command.Parameters.AddWithValue("@UploadedBy", uploadedBy ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@RelatedEntityType", relatedEntityType ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@RelatedEntityId", relatedEntityId ?? (object)DBNull.Value);

                    var result = await command.ExecuteScalarAsync();
                    return Convert.ToInt32(result);
                }
            }
        }

        private async Task<string> GetStoredHashAsync(int documentId)
        {
            using (SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                await connection.OpenAsync();

                string sql = "SELECT DocumentHash FROM DocumentHashes WHERE HashId = @HashId";
                using (SqlCommand command = new SqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@HashId", documentId);
                    var result = await command.ExecuteScalarAsync();
                    return result?.ToString();
                }
            }
        }

        private async Task<string> GetClaimDocumentHashAsync(int claimId, string documentType)
        {
            using (SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                await connection.OpenAsync();

                string columnName;
                switch (documentType.ToLower())
                {
                    case "self-assessment":
                        columnName = "SelfAssessmentHash";
                        break;
                    case "curriculum":
                        columnName = "CurriculumHash";
                        break;
                    case "faculty":
                        columnName = "FacultyCredentialsHash";
                        break;
                    default:
                        return null;
                }

                string sql = $"SELECT {columnName} FROM AccreditationClaims WHERE Id = @ClaimId";
                using (SqlCommand command = new SqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@ClaimId", claimId);
                    var result = await command.ExecuteScalarAsync();
                    return result?.ToString();
                }
            }
        }
    }
}