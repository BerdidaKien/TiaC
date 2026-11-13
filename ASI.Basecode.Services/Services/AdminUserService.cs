using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ASI.Basecode.Data.Interfaces;
using ASI.Basecode.Data.Models;
using ASI.Basecode.Services.Interfaces;
using ASI.Basecode.Services.Manager;
using ASI.Basecode.Services.ServiceModels;
using Microsoft.EntityFrameworkCore;

namespace ASI.Basecode.Services.Services
{
    public class AdminUserService : IAdminUserService
    {
        // Admin accounts are not created from this screen
        private static readonly string[] AllowedRoles = { "User", "Custodian" };
        private const int MaxRows = 100;
        private const string PasswordChars = "ABCDEFGHJKMNPQRSTUVWXYZabcdefghjkmnpqrstuvwxyz23456789";

        private readonly IUserAdminRepository _repo;

        public AdminUserService(IUserAdminRepository repo)
        {
            _repo = repo;
        }

        // The whole batch is checked first; nothing is saved unless every row is valid,
        // so the admin can fix the flagged rows and resubmit without retyping the rest.
        public AddUsersResult CreateUsers(AddUsersPostModel model, string adminCode)
        {
            var result = new AddUsersResult();

            var role = AllowedRoles.FirstOrDefault(r => string.Equals(r, model.Role, StringComparison.OrdinalIgnoreCase));
            if (role == null)
            {
                result.GeneralError = "Please choose a role: Student or Custodian.";
                return result;
            }
            result.Role = role;

            var rows = model.UserRows ?? new List<AddUserRow>();
            if (rows.Count == 0)
            {
                result.GeneralError = "Add at least one user.";
                return result;
            }
            if (rows.Count > MaxRows)
            {
                result.GeneralError = $"You can create at most {MaxRows} users at a time.";
                return result;
            }

            var ids = rows.Select(r => Clean(r.StudentEmployeeId)).Where(s => s.Length > 0).ToList();
            var emails = rows.Select(r => Clean(r.Email)).Where(s => s.Length > 0).ToList();

            // Existing accounts that clash with this batch
            var existing = _repo.GetUsers()
                .Where(u => ids.Contains(u.UserId) || emails.Contains(u.Email))
                .Select(u => new { u.UserId, u.Email })
                .ToList();

            var takenIds = new HashSet<string>(existing.Select(e => e.UserId), StringComparer.OrdinalIgnoreCase);
            var takenEmails = new HashSet<string>(existing.Select(e => e.Email), StringComparer.OrdinalIgnoreCase);
            var batchIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var batchEmails = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var now = DateTime.Now;
            var pending = new List<(User User, CreatedUserInfo Info)>();

            for (int i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                var id = Clean(row.StudentEmployeeId);
                var first = Clean(row.FirstName);
                var middle = Clean(row.MiddleName);
                var last = Clean(row.LastName);
                var email = Clean(row.Email);
                var contact = UserFieldRules.NormalizeContact(row.ContactNumber);

                var errors = new Dictionary<string, string>();
                void Check(string key, string message)
                {
                    if (message != null) errors[key] = message;
                }

                Check("id", UserFieldRules.CheckId(id));
                Check("first", UserFieldRules.CheckName(first, "First name", required: true));
                Check("middle", UserFieldRules.CheckName(middle, "Middle name", required: false));
                Check("last", UserFieldRules.CheckName(last, "Last name", required: true));
                Check("email", UserFieldRules.CheckEmail(email));
                Check("contact", UserFieldRules.CheckContact(contact));

                if (!errors.ContainsKey("id"))
                {
                    if (takenIds.Contains(id)) errors["id"] = "This ID already has an account.";
                    else if (!batchIds.Add(id)) errors["id"] = "This ID appears more than once in this list.";
                }
                if (!errors.ContainsKey("email"))
                {
                    if (takenEmails.Contains(email)) errors["email"] = "This email already has an account.";
                    else if (!batchEmails.Add(email)) errors["email"] = "This email appears more than once in this list.";
                }

                var password = model.AutoGeneratePassword ? GeneratePassword() : (row.Password ?? "");
                if (!model.AutoGeneratePassword)
                    Check("password", UserFieldRules.CheckPassword(password));

                if (errors.Count > 0)
                {
                    result.RowErrors[i] = errors;
                    continue;
                }

                var user = new User
                {
                    UserId = id,
                    FirstName = first,
                    MiddleName = middle.Length == 0 ? null : middle,
                    LastName = last,
                    Email = email,
                    ContactNumber = contact.Length == 0 ? null : contact,
                    PasswordHash = PasswordManager.EncryptPassword(password),
                    Role = role,
                    IsActive = true,
                    AccountStatus = "active",
                    MustCompleteSetup = true,
                    BorrowerStatus = "Active",
                    CreatedBy = adminCode,
                    UpdatedBy = adminCode,
                    CreatedTime = now,
                    UpdatedTime = now
                };

                var info = new CreatedUserInfo
                {
                    UserId = id,
                    FullName = (first + " " + (middle.Length == 0 ? "" : middle + " ") + last).Trim(),
                    Email = email,
                    Role = role,
                    TempPassword = password
                };

                pending.Add((user, info));
            }

            if (result.RowErrors.Count > 0) return result;

            try
            {
                _repo.AddUsers(pending.Select(p => p.User));
                _repo.SaveChanges();
                result.Created.AddRange(pending.Select(p => p.Info));
            }
            catch (DbUpdateException)
            {
                // e.g. someone else created the same ID or email at the same moment
                result.GeneralError = "The accounts could not be saved. An ID or email may have just been taken. Please submit again.";
            }

            return result;
        }

        private static string Clean(string value) => (value ?? "").Trim();

        private static string GeneratePassword()
        {
            var chars = new char[12];
            for (int i = 0; i < chars.Length; i++)
            {
                chars[i] = PasswordChars[RandomNumberGenerator.GetInt32(PasswordChars.Length)];
            }
            return new string(chars);
        }
    }

    // Field rules for new accounts. The Add Users page applies the same rules in the browser.
    public static class UserFieldRules
    {
        private static readonly Regex IdPattern = new Regex(@"^[A-Za-z0-9]+(-[A-Za-z0-9]+)*$");
        private static readonly Regex NamePattern = new Regex(@"^\p{L}[\p{L}\p{M} .'\-]*$");
        private static readonly Regex EmailPattern = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
        private static readonly Regex ContactPattern = new Regex(@"^(09\d{9}|\+639\d{9})$");

        public static string CheckId(string id)
        {
            if (id.Length == 0) return "ID is required.";
            if (id.Length > 50) return "ID must be 50 characters or fewer.";
            if (!IdPattern.IsMatch(id)) return "Use letters and numbers only, separated by single hyphens (e.g. 2023-1001).";
            return null;
        }

        public static string CheckName(string name, string label, bool required)
        {
            if (name.Length == 0) return required ? label + " is required." : null;
            if (name.Length > 50) return label + " must be 50 characters or fewer.";
            if (!NamePattern.IsMatch(name)) return label + " can only contain letters, spaces, periods, hyphens and apostrophes.";
            return null;
        }

        public static string CheckEmail(string email)
        {
            if (email.Length == 0) return "Email is required.";
            if (email.Length > 100) return "Email must be 100 characters or fewer.";
            if (!EmailPattern.IsMatch(email) || !IsValidMailAddress(email)) return "Enter a valid email address (e.g. name@school.edu.ph).";
            return null;
        }

        // Optional. Philippine mobile number: 09XXXXXXXXX or +639XXXXXXXXX
        public static string CheckContact(string contact)
        {
            if (contact.Length == 0) return null;
            if (!ContactPattern.IsMatch(contact)) return "Use 11 digits starting with 09 (e.g. 09171234567) or +639 followed by 9 digits.";
            return null;
        }

        public static string CheckPassword(string password)
        {
            if (password.Length < 8 || password.Length > 64) return "Password must be 8 to 64 characters.";
            if (!password.Any(char.IsLetter) || !password.Any(char.IsDigit)) return "Password must contain at least one letter and one number.";
            return null;
        }

        // Spaces and hyphens people type for readability are dropped before checking
        public static string NormalizeContact(string value) =>
            Regex.Replace(value ?? "", @"[\s\-]", "");

        private static bool IsValidMailAddress(string email)
        {
            try
            {
                return new MailAddress(email).Address == email;
            }
            catch (FormatException)
            {
                return false;
            }
        }
    }
}
