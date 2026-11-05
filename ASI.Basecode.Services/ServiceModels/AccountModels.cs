using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Mail;
using System.Text.RegularExpressions;
using ASI.Basecode.Data.Models;

namespace ASI.Basecode.Services.ServiceModels
{
    public static class AccountStatuses
    {
        public const string Active = "active";
        public const string Inactive = "inactive";
        public const string Disabled = "disabled";

        public static readonly string[] All = { Active, Inactive, Disabled };

        public static string Label(string s) => s switch
        {
            Active => "Active",
            Inactive => "Inactive",
            Disabled => "Disabled",
            _ => s ?? "-"
        };
    }

    // The fields a user can ask the admin to correct
    public static class InfoFields
    {
        public static readonly (string Key, string Label, int Max)[] All =
        {
            ("first_name", "First name", 50),
            ("middle_name", "Middle name", 50),
            ("last_name", "Last name", 50),
            ("user_code", "Student / Employee ID", 50),
            ("email", "Email address", 100)
        };

        public static string Label(string key)
        {
            foreach (var f in All) if (f.Key == key) return f.Label;
            return key;
        }

        public static int Max(string key)
        {
            foreach (var f in All) if (f.Key == key) return f.Max;
            return 100;
        }

        public static string Read(User u, string key)
        {
            switch (key)
            {
                case "first_name": return u.FirstName;
                case "middle_name": return u.MiddleName;
                case "last_name": return u.LastName;
                case "user_code": return u.UserId;
                case "email": return u.Email;
                default: return null;
            }
        }

        public static void Apply(User u, string key, string value)
        {
            switch (key)
            {
                case "first_name": u.FirstName = value; break;
                case "middle_name": u.MiddleName = value.Length == 0 ? null : value; break;
                case "last_name": u.LastName = value; break;
                case "user_code": u.UserId = value; break;
                case "email": u.Email = value; break;
            }
        }

        public static string Normalize(string key, string raw)
        {
            raw = raw ?? "";
            return key == "first_name" || key == "middle_name" || key == "last_name"
                ? Regex.Replace(raw.Trim(), @"\s+", " ")
                : raw.Trim();
        }

        // Returns an error message, or null when the value is fine
        public static string Validate(string key, string value)
        {
            var label = Label(key);
            value = value ?? "";

            if (value.Length == 0)
                return key == "middle_name" ? null : label + " is required.";

            if (value.Length > Max(key))
                return $"{label} must be {Max(key)} characters or fewer.";

            switch (key)
            {
                case "first_name":
                case "middle_name":
                case "last_name":
                    if (!Regex.IsMatch(value, @"^[\p{L}][\p{L} .'\-]*$"))
                        return label + " can only contain letters, spaces, apostrophes, hyphens and periods.";
                    break;

                case "user_code":
                    if (!Regex.IsMatch(value, @"^[A-Za-z0-9._\-]+$"))
                        return label + " can only contain letters, numbers, dots, hyphens and underscores.";
                    break;

                case "email":
                    try
                    {
                        var addr = new MailAddress(value);
                        if (addr.Address != value) return "Email address is not valid.";
                    }
                    catch (FormatException)
                    {
                        return "Email address is not valid.";
                    }
                    break;
            }

            return null;
        }
    }

    // A proof photo that was saved on disk
    public class StoredProof
    {
        public string StoredName { get; set; }
        public string OriginalName { get; set; }
        public string ContentType { get; set; }
        public long SizeBytes { get; set; }
    }

    public class InfoRequestItemRow
    {
        public string Key { get; set; }
        public string Label { get; set; }
        public string OldValue { get; set; }
        public string NewValue { get; set; }
        public string ApprovedValue { get; set; }
        public int MaxLength { get; set; }
        public bool Required { get; set; }
    }

    public class InfoRequestFileRow
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public long Size { get; set; }
    }

    public class InfoRequestRow
    {
        public int Id { get; set; }
        public string Status { get; set; }          // pending | approved | rejected | cancelled
        public string Reason { get; set; }
        public string ReviewNote { get; set; }
        public string ReviewedBy { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public DateTime CreatedTime { get; set; }

        public int UserDbId { get; set; }
        public string UserCode { get; set; }
        public string UserName { get; set; }
        public string UserRole { get; set; }
        public string UserEmail { get; set; }

        public List<InfoRequestItemRow> Items { get; set; } = new List<InfoRequestItemRow>();
        public List<InfoRequestFileRow> Files { get; set; } = new List<InfoRequestFileRow>();

        public string Code => "REQ-" + CreatedTime.Year + "-" + Id.ToString("D4");
    }

    // What the user posts on the request form
    public class SubmitInfoRequest
    {
        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public string LastName { get; set; }
        public string UserCode { get; set; }
        public string Email { get; set; }
        public string Reason { get; set; }
    }

    public class InfoRequestFormViewModel
    {
        public string UserCode { get; set; }
        public string Role { get; set; }
        public string IdLabel { get; set; }

        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }

        public InfoRequestRow Pending { get; set; }
        public List<InfoRequestRow> History { get; set; } = new List<InfoRequestRow>();

        // The values the user just typed, when the form is shown again after an error
        public SubmitInfoRequest Posted { get; set; }
    }

    public class AdminInfoRequestsViewModel
    {
        public string Status { get; set; }          // pending | approved | rejected | all
        public string Search { get; set; }

        public int PendingCount { get; set; }
        public int ApprovedCount { get; set; }
        public int RejectedCount { get; set; }
        public int TotalCount { get; set; }

        public List<InfoRequestRow> Rows { get; set; } = new List<InfoRequestRow>();

        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalRows { get; set; }
        public int TotalPages { get; set; } = 1;

        public int FirstShown => TotalRows == 0 ? 0 : (Page - 1) * PageSize + 1;
        public int LastShown => Math.Min(Page * PageSize, TotalRows);

        // Page numbers to show; 0 means "..."
        public List<int> PageWindow()
        {
            var pages = new SortedSet<int> { 1, TotalPages, Page - 1, Page, Page + 1 };
            var result = new List<int>();
            int prev = 0;
            foreach (var p in pages.Where(p => p >= 1 && p <= TotalPages))
            {
                if (prev != 0 && p - prev > 1) result.Add(0);
                result.Add(p);
                prev = p;
            }
            return result;
        }
    }

    public class UserEditViewModel
    {
        public int Id { get; set; }
        public string UserCode { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string ContactNumber { get; set; }
        public string Role { get; set; }
        public string Status { get; set; }
        public DateTime CreatedTime { get; set; }

        public int ItemsOut { get; set; }
        public int OpenRequests { get; set; }
        public bool IsSelf { get; set; }
        public bool IsLastActiveAdmin { get; set; }

        public List<InfoRequestRow> Requests { get; set; } = new List<InfoRequestRow>();
    }
}