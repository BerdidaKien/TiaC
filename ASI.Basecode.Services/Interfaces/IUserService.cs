using ASI.Basecode.Data.Models;
using ASI.Basecode.Services.ServiceModels;
using System.Collections.Generic;
using static ASI.Basecode.Resources.Constants.Enums;

namespace ASI.Basecode.Services.Interfaces
{
    public interface IUserService
    {
        LoginResult AuthenticateUser(string userId, string password, ref User user);
        void AddUser(UserViewModel model);
        bool ChangePassword(string userId, string currentPassword, string newPassword);

        // Admin User Management Signatures (NO 'public' keyword inside interface)
        UserManagementViewModel GetUserManagementData(string search, string role, string status);
    }
}