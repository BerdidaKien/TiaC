using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
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
        private static readonly string[] AllowedRoles = { "User", "Custodian", "Admin" };
        private const int MaxRows = 100;
        private const string PasswordChars = "ABCDEFGHJKMNPQRSTUVWXYZabcdefghjkmnpqrstuvwxyz23456789";

        private readonly IUserAdminRepository _repo;

        public AdminUserService(IUserAdminRepository repo)
        {
            _repo = repo;
        }

        public AddUsersResult CreateUsers(AddUsersPostModel model, string adminCode)
        {
            var result = new AddUsersResult { IsActive = model.IsActive };

            var role = AllowedRoles.FirstOrDefault(r => string.Equals(r, model.Role, StringComparison.OrdinalIgnoreCase));
            if (role == null)
            {
                result.GeneralError = "Invalid role selected.";
                return result;
            }
            result.Role = role;

            if (model.UserRows == null || model.UserRows.Count == 0)
            {
                result.GeneralError = "Add at least one user.";
                return result;
            }
            if (model.UserRows.Count > MaxRows)
            {
                result.GeneralError = $"You can create at most {MaxRows} users at a time.";
                return result;
            }

            var ids = model.UserRows.Select(r => Clean(r.StudentEmployeeId)).Where(s => s.Length > 0).ToList();
            var emails = model.UserRows.Select(r => Clean(r.Email)).Where(s => s.Length > 0).ToList();

            // Existing accounts that clash with this batch
            var existing = _repo.GetUsers()
                .Where(u => ids.Contains(u.UserId) || emails.Contains(u.Email))
                .Select(u => new { u.UserId, u.Email })
                .ToList();

            var takenIds = new HashSet<string>(existing.Select(e => e.UserId), StringComparer.OrdinalIgnoreCase);
            var takenEmails = new HashSet<string>(existing.Select(e => e.Email), StringComparer.OrdinalIgnoreCase);

            var now = DateTime.Now;
            var pending = new List<(User User, CreatedUserInfo Info, int Row)>();

            for (int i = 0; i < model.UserRows.Count; i++)
            {
                var row = model.UserRows[i];
                var rowNumber = i + 1;
                var id = Clean(row.StudentEmployeeId);
                var first = Clean(row.FirstName);
                var middle = Clean(row.MiddleName);
                var last = Clean(row.LastName);
                var email = Clean(row.Email);
                var contact = Clean(row.Contact());

                string error = null;

                if (id.Length == 0 || first.Length == 0 || last.Length == 0 || email.Length == 0)
                    error = "ID, first name, last name and email are required.";
                else if (id.Length > 50 || first.Length > 50 || middle.Length > 50 || last.Length > 50)
                    error = "ID and names must be 50 characters or fewer.";
                else if (email.Length > 100 || !IsValidEmail(email))
                    error = "Email address is not valid.";
                else if (contact.Length > 20)
                    error = "Contact number must be 20 characters or fewer.";
                else if (takenIds.Contains(id))
                    error = "This ID already exists or is repeated in this batch.";
                else if (takenEmails.Contains(email))
                    error = "This email already exists or is repeated in this batch.";

                string password = null;
                if (error == null)
                {
                    password = model.AutoGeneratePassword ? GeneratePassword() : (row.Password ?? "");
                    if (password.Length < 8)
                        error = "Password must be at least 8 characters.";
                }

                if (error != null)
                {
                    result.Failed.Add(new FailedUserInfo { RowNumber = rowNumber, UserId = id, Reason = error });
                    continue;
                }

                takenIds.Add(id);
                takenEmails.Add(email);

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
                    IsActive = model.IsActive,
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

                pending.Add((user, info, rowNumber));
            }

            if (pending.Count > 0)
            {
                try
                {
                    _repo.AddUsers(pending.Select(p => p.User));
                    _repo.SaveChanges();
                    result.Created.AddRange(pending.Select(p => p.Info));
                }
                catch (DbUpdateException)
                {
                    // e.g. someone else created the same ID or email at the same moment
                    foreach (var p in pending)
                    {
                        result.Failed.Add(new FailedUserInfo
                        {
                            RowNumber = p.Row,
                            UserId = p.Info.UserId,
                            Reason = "Could not be saved. Please try again."
                        });
                    }
                }
            }

            result.Failed = result.Failed.OrderBy(f => f.RowNumber).ToList();
            return result;
        }

        private static string Clean(string value) => (value ?? "").Trim();

        private static bool IsValidEmail(string email)
        {
            try
            {
                var addr = new MailAddress(email);
                return addr.Address == email;
            }
            catch (FormatException)
            {
                return false;
            }
        }

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

    internal static class AddUserRowExtensions
    {
        public static string Contact(this AddUserRow row) => row.ContactNumber;
    }
}
