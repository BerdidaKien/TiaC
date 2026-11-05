using System;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace ASI.Basecode.WebApp
{
    public class ProfilePhotoStorage
    {
        public const long MaxBytes = 5 * 1024 * 1024;   // 5 MB

        private static readonly Regex WebPathPattern =
            new Regex(@"^/uploads/profiles/[0-9a-f]{32}\.(jpg|png|webp)$");

        private readonly string _folder;

        public ProfilePhotoStorage(IWebHostEnvironment env)
        {
            _folder = Path.Combine(env.WebRootPath, "uploads", "profiles");
        }

        // On success, webPath is something like /uploads/profiles/<random>.jpg
        public bool TrySave(IFormFile file, out string webPath, out string error)
        {
            webPath = null;
            error = null;

            if (file == null || file.Length == 0)
            {
                error = "The photo is empty.";
                return false;
            }

            if (file.Length > MaxBytes)
            {
                error = "The photo must be 5 MB or smaller.";
                return false;
            }

            string ext;
            using (var s = file.OpenReadStream())
            {
                ext = DetectExtension(s);
            }

            if (ext == null)
            {
                error = "Please upload a JPG, PNG or WEBP image.";
                return false;
            }

            Directory.CreateDirectory(_folder);
            var fileName = Guid.NewGuid().ToString("N") + ext;

            using (var fs = new FileStream(Path.Combine(_folder, fileName), FileMode.CreateNew))
            {
                file.CopyTo(fs);
            }

            webPath = "/uploads/profiles/" + fileName;
            return true;
        }

        // Deletes a photo saved by TrySave. Anything else is ignored.
        public void Delete(string webPath)
        {
            if (string.IsNullOrEmpty(webPath) || !WebPathPattern.IsMatch(webPath)) return;

            var path = Path.Combine(_folder, Path.GetFileName(webPath));
            try
            {
                if (File.Exists(path)) File.Delete(path);
            }
            catch (IOException)
            {
                // a leftover file is harmless
            }
        }

        private static string DetectExtension(Stream s)
        {
            var h = new byte[12];
            var read = s.Read(h, 0, h.Length);

            if (read >= 3 && h[0] == 0xFF && h[1] == 0xD8 && h[2] == 0xFF) return ".jpg";

            if (read >= 8 && h[0] == 0x89 && h[1] == 0x50 && h[2] == 0x4E && h[3] == 0x47 &&
                h[4] == 0x0D && h[5] == 0x0A && h[6] == 0x1A && h[7] == 0x0A) return ".png";

            if (read >= 12 && h[0] == 0x52 && h[1] == 0x49 && h[2] == 0x46 && h[3] == 0x46 &&
                h[8] == 0x57 && h[9] == 0x45 && h[10] == 0x42 && h[11] == 0x50) return ".webp";

            return null;
        }
    }
}