using ASI.Basecode.Data.Models;
using ASI.Basecode.Services.ServiceModels;
using static ASI.Basecode.Resources.Constants.Enums;

namespace ASI.Basecode.Services.Interfaces
{
    public interface IUserService
    {
        LoginResult AuthenticateUser(string userId, string password, ref User user);

        bool ChangePassword(string userId, string currentPassword, string newPassword);

        // First login of an account an admin created
        bool MustCompleteSetup(string userId);
        ServiceResult CompleteFirstLogin(string userId, string newPassword);

        // Admin User Management
        UserManagementViewModel GetUserManagementData(string search, string role, string status);

        // Custodian Profile Methods
        CustodianProfileViewModel GetCustodianProfile(string userIdStr);

        // Shared / Student Profile Methods
        ProfileViewModel GetProfile(string userId);
        string GetProfileImagePath(string userId);
        ServiceResult UpdateProfile(string userId, string contactNumber, string email, string newImagePath);
    }
}
