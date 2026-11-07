using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASI.Basecode.Services.ServiceModels
{
    public class AddUserRow
    {
        public string StudentEmployeeId { get; set; }
        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string ContactNumber { get; set; }
        public string Password { get; set; }

        public bool IsBlank() =>
            string.IsNullOrWhiteSpace(StudentEmployeeId) && string.IsNullOrWhiteSpace(FirstName) &&
            string.IsNullOrWhiteSpace(MiddleName) && string.IsNullOrWhiteSpace(LastName) &&
            string.IsNullOrWhiteSpace(Email) && string.IsNullOrWhiteSpace(ContactNumber) &&
            string.IsNullOrWhiteSpace(Password);
    }

    public class AddUsersPostModel
    {
        public string Role { get; set; } = "User";
        public bool AutoGeneratePassword { get; set; } = true;
        public List<AddUserRow> UserRows { get; set; } = new List<AddUserRow> { new AddUserRow() };

        // Filled by the controller when the batch is sent back to be corrected
        public string FormError { get; set; }
        public Dictionary<int, Dictionary<string, string>> RowErrors { get; set; } = new Dictionary<int, Dictionary<string, string>>();
    }

    public class CreatedUserInfo
    {
        public string UserId { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Role { get; set; }
        public string TempPassword { get; set; }
    }

    public class AddUsersResult
    {
        public string Role { get; set; }
        public string GeneralError { get; set; }
        public List<CreatedUserInfo> Created { get; set; } = new List<CreatedUserInfo>();

        // Row index (0-based) -> field key (id, first, middle, last, email, contact, password) -> message
        public Dictionary<int, Dictionary<string, string>> RowErrors { get; set; } = new Dictionary<int, Dictionary<string, string>>();

        public bool Success => GeneralError == null && RowErrors.Count == 0 && Created.Count > 0;
    }
}
