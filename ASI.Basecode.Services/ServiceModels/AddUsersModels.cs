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
    }

    public class AddUsersPostModel
    {
        public string Role { get; set; } = "User";
        public bool AutoGeneratePassword { get; set; } = true;
        public bool IsActive { get; set; } = true;
        public List<AddUserRow> UserRows { get; set; } = new List<AddUserRow> { new AddUserRow() };
    }

    public class CreatedUserInfo
    {
        public string UserId { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Role { get; set; }
        public string TempPassword { get; set; }
    }

    public class FailedUserInfo
    {
        public int RowNumber { get; set; }
        public string UserId { get; set; }
        public string Reason { get; set; }
    }

    public class AddUsersResult
    {
        public string Role { get; set; }
        public bool IsActive { get; set; }
        public string GeneralError { get; set; }
        public List<CreatedUserInfo> Created { get; set; } = new List<CreatedUserInfo>();
        public List<FailedUserInfo> Failed { get; set; } = new List<FailedUserInfo>();
    }
}