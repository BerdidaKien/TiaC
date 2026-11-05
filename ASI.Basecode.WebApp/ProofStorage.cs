using System;
using System.IO;
using System.Text.RegularExpressions;
using ASI.Basecode.Services.ServiceModels;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace ASI.Basecode.WebApp
{
    // Saves proof photos OUTSIDE wwwroot, so they are never served as public static files.
    // The only way to open one is the admin-only ProofFile action.
    public class ProofStorage
    {
        public const long MaxBytes = 5 * 1024 * 1024;   // 5 MB

        private static readonly Regex StoredNamePattern = new Regex(@"^[0-9a-f]{32}\.(jpg|png|webp)$");
        private readonly string _folder;

        public ProofStorage(IWebHostEnvironment env)
        {
            _folder = Path.Combine(env.ContentRootPath, "PrivateUploads", "info-requests");
        }

        public bool TrySave(IFormFile file, out StoredProof stored, out string error)
        {
            stored = null;
            error = null;

            if (file == null || file.Length == 0)
            {
                error = "One of the files is empty.";
                return false;
            }

            if (file.Length > MaxBytes)
            {
                error = $"\"{ShortName(file)}\" is larger than 5 MB.";
                return false;
            }

            // Check the real file signature, not just the name or the browser's content type
            string ext;
            using (var s = file.OpenReadStream())
            {
                ext = DetectExtension(s);
            }

            if (ext == null)
            {
                error = $"\"{ShortName(file)}\" is not a JPG, PNG or WEBP picture.";
                return false;
            }

            Directory.CreateDirectory(_folder);
            var storedName = Guid.NewGuid().ToString("N") + ext;

            using (var fs = new FileStream(Path.Combine(_folder, storedName), FileMode.CreateNew))
            {
                file.CopyTo(fs);
            }

            stored = new StoredProof
            {
                StoredName = storedName,
                OriginalName = ShortName(file),
                ContentType = ext == ".jpg" ? "image/jpeg" : ext == ".png" ? "image/png" : "image/webp",
                SizeBytes = file.Length
            };
            return true;
        }

        // Returns the full path for a stored name, or null if the name isn't one we created
        public string PhysicalPath(string storedName)
        {
            if (string.IsNullOrEmpty(storedName) || !StoredNamePattern.IsMatch(storedName)) return null;
            return Path.Combine(_folder, storedName);
        }

        public void Delete(string storedName)
        {
            var path = PhysicalPath(storedName);
            if (path == null) return;

            try
            {
                if (File.Exists(path)) File.Delete(path);
            }
            catch (IOException)
            {
                // a leftover file is harmless
            }
        }

        private static string ShortName(IFormFile file)
        {
            var name = Path.GetFileName(file.FileName ?? "file");
            return name.Length > 120 ? name.Substring(0, 120) : name;
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