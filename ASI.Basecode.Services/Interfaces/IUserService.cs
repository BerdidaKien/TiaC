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

        // Admin User Management
        UserManagementViewModel GetUserManagementData(string search, string role, string status);

        // Get logged-in user's profile
        ProfileViewModel GetProfile(string userId);

        // Profile photo / editable profile fields
        string GetProfileImagePath(string userId);
        ServiceResult UpdateProfile(string userId, string contactNumber, string email, string newImagePath);
    }
}