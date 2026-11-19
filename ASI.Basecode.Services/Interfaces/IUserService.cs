using ASI.Basecode.Data.Models;
using ASI.Basecode.Services.ServiceModels;
using static ASI.Basecode.Resources.Constants.Enums;

namespace ASI.Basecode.Services.Interfaces
{
    public interface IUserService
    {
        LoginResult AuthenticateUser(string userCode, string password, ref User user);

        bool ChangePassword(string userCode, string currentPassword, string newPassword);

        // First login of an account an admin created
        bool MustCompleteSetup(string userCode);

        // Checked on every request: an account that is disabled or made inactive is signed out
        AccountAccess GetAccountAccess(string userCode);
        ServiceResult CompleteFirstLogin(string userCode, string newPassword);

        // Admin User Management
        UserManagementViewModel GetUserManagementData(string search, string role, string status);

        // Custodian Profile Methods
        CustodianProfileViewModel GetCustodianProfile(string userCode);

        // Shared / Student Profile Methods
        ProfileViewModel GetProfile(string userCode);
        string GetProfileImagePath(string userCode);
        ServiceResult UpdateProfile(string userCode, string contactNumber, string email, string newImagePath);
    }
}
