using ASI.Basecode.Data.Models;
using ASI.Basecode.Services.ServiceModels;
using static ASI.Basecode.Resources.Constants.Enums;

namespace ASI.Basecode.Services.Interfaces
{
    public interface IUserService
    {
        LoginResult AuthenticateUser(string userId, string password, ref User user);
        void AddUser(UserViewModel model);
        bool ChangePassword(string userId, string currentPassword, string newPassword);
        UserManagementViewModel GetUserManagementData(string search, string role, string status);

        CustodianProfileViewModel GetCustodianProfile(string userIdStr);
        bool UpdateCustodianProfile(int id, string firstName, string lastName, string contactNumber);
    }
}