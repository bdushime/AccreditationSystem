using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AccreditationSystem.Utilities;

namespace AccreditationSystem.Services
{
    public class DuplicateDocumentDetector
    {
        private readonly IConfiguration _configuration;

        public DuplicateDocumentDetector(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        /// <summary>
        /// Check if a document already exists in the system by comparing its hash
        /// </summary>
        /// <param name="file">The file to check</param>
        /// <returns>A tuple containing a boolean indicating if the file is a duplicate and document info if found</returns>
        public async Task<(bool isDuplicate, DocumentInfo documentInfo)> IsDuplicateAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return (false, null);
            }

            try
            {
                // Generate hash for the file
                string fileHash = await DocumentHasher.GenerateFileHashAsync(file);

                // Check if hash exists in the database
                return await CheckHashExistsAsync(fileHash);
            }
            catch (Exception)
            {
                return (false, null);
            }
        }

        /// <summary>
        /// Checks if multiple files are duplicates
        /// </summary>
        /// <param name="files">The collection of files to check</param>
        /// <returns>A dictionary with file names as keys and duplicate info as values</returns>
        public async Task<Dictionary<string, (bool isDuplicate, DocumentInfo documentInfo)>> CheckMultipleFilesAsync(IEnumerable<IFormFile> files)
        {
            var results = new Dictionary<string, (bool isDuplicate, DocumentInfo documentInfo)>();

            if (files == null)
            {
                return results;
            }

            foreach (var file in files)
            {
                if (file != null && file.Length > 0)
                {
                    var result = await IsDuplicateAsync(file);
                    results.Add(file.FileName, result);
                }
            }

            return results;
        }

        private async Task<(bool isDuplicate, DocumentInfo documentInfo)> CheckHashExistsAsync(string fileHash)
        {
            using (SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                await connection.OpenAsync();

                string sql = @"
                    SELECT 
                        h.HashId, 
                        h.FileName, 
                        h.FilePath, 
                        h.FileType, 
                        h.UploadDate, 
                        h.UploadedBy, 
                        h.RelatedEntityType, 
                        h.RelatedEntityId
                    FROM DocumentHashes h
                    WHERE h.DocumentHash = @DocumentHash";

                using (SqlCommand command = new SqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@DocumentHash", fileHash);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            // Document with the same hash exists
                            var documentInfo = new DocumentInfo
                            {
                                HashId = reader.GetInt32(0),
                                FileName = reader.GetString(1),
                                FilePath = reader.GetString(2),
                                FileType = reader.GetString(3),
                                UploadDate = reader.GetDateTime(4),
                                UploadedBy = reader.IsDBNull(5) ? null : reader.GetString(5),
                                RelatedEntityType = reader.IsDBNull(6) ? null : reader.GetString(6),
                                RelatedEntityId = reader.IsDBNull(7) ? null : (int?)reader.GetInt32(7)
                            };

                            return (true, documentInfo);
                        }
                    }
                }
            }

            // No duplicate found
            return (false, null);
        }
    }

    public class DocumentInfo
    {
        public int HashId { get; set; }
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public string FileType { get; set; }
        public DateTime UploadDate { get; set; }
        public string UploadedBy { get; set; }
        public string RelatedEntityType { get; set; }
        public int? RelatedEntityId { get; set; }
    }
}