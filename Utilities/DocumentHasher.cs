using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace AccreditationSystem.Utilities
{
    public static class DocumentHasher
    {
        /// <summary>
        /// Generates a SHA256 hash for the provided file
        /// </summary>
        /// <param name="file">The uploaded file to hash</param>
        /// <returns>A string representation of the file hash</returns>
        public static async Task<string> GenerateFileHashAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                throw new ArgumentException("Cannot generate hash for null or empty file", nameof(file));
            }

            using (var memoryStream = new MemoryStream())
            {
                // Copy the file data to a memory stream
                await file.CopyToAsync(memoryStream);
                memoryStream.Position = 0;

                // Create a SHA256 hash of the file content
                using (var sha256 = SHA256.Create())
                {
                    byte[] hashBytes = sha256.ComputeHash(memoryStream);

                    // Convert the byte array to a hexadecimal string
                    StringBuilder builder = new StringBuilder();
                    for (int i = 0; i < hashBytes.Length; i++)
                    {
                        builder.Append(hashBytes[i].ToString("x2"));
                    }

                    return builder.ToString();
                }
            }
        }

        /// <summary>
        /// Validates if a file matches a previously stored hash
        /// </summary>
        /// <param name="file">The file to validate</param>
        /// <param name="storedHash">The previously stored hash</param>
        /// <returns>True if the file matches the hash, false otherwise</returns>
        public static async Task<bool> ValidateFileHashAsync(IFormFile file, string storedHash)
        {
            if (string.IsNullOrEmpty(storedHash))
            {
                return false;
            }

            try
            {
                string fileHash = await GenerateFileHashAsync(file);
                return string.Equals(fileHash, storedHash, StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }
    }
}