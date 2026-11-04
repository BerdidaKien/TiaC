// Location: ASI.Basecode.WebApp/Mvc/ProfilePhotoHelper.cs
using System;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Http;

namespace ASI.Basecode.WebApp.Mvc
{
    /// <summary>
    /// Validates, saves and deletes profile photos (wwwroot/uploads/profiles).
    /// </summary>
    public static class ProfilePhotoHelper
    {
        private const long MaxBytes = 5 * 1024 * 1024; // 5 MB
        private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".webp" };

        // Returns an error message, or null when the photo is fine (or none was chosen)
        public static string Validate(IFormFile photo)
        {
            if (photo == null || photo.Length == 0) return null;

            var ext = Path.GetExtension(photo.FileName ?? "").ToLowerInvariant();
            if (!AllowedExtensions.Contains(ext) ||
                !(photo.ContentType ?? "").StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            {
                return "Please upload a JPG, PNG or WEBP image.";
            }

            if (photo.Length > MaxBytes)
            {
                return "The photo must be 5 MB or smaller.";
            }

            return null;
        }

        // Saves with a random file name and returns the web path (null when no photo was chosen)
        public static string Save(IFormFile photo, string webRootPath)
        {
            if (photo == null || photo.Length == 0) return null;

            var ext = Path.GetExtension(photo.FileName).ToLowerInvariant();
            var folder = Path.Combine(webRootPath, "uploads", "profiles");
            Directory.CreateDirectory(folder);

            var fileName = $"{Guid.NewGuid():N}{ext}";
            using (var stream = new FileStream(Path.Combine(folder, fileName), FileMode.Create))
            {
                photo.CopyTo(stream);
            }

            return "/uploads/profiles/" + fileName;
        }

        // Deletes an old photo (only inside wwwroot/uploads/profiles)
        public static void Delete(string imagePath, string webRootPath)
        {
            if (string.IsNullOrEmpty(imagePath) ||
                !imagePath.StartsWith("/uploads/profiles/", StringComparison.OrdinalIgnoreCase))
                return;

            var fullPath = Path.Combine(webRootPath, "uploads", "profiles", Path.GetFileName(imagePath));
            try
            {
                if (File.Exists(fullPath)) File.Delete(fullPath);
            }
            catch (IOException) { /* a leftover file is harmless */ }
        }
    }
}