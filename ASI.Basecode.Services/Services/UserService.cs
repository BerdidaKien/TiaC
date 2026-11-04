using ASI.Basecode.Data.Interfaces;
using ASI.Basecode.Data.Models;
using ASI.Basecode.Services.Interfaces;
using ASI.Basecode.Services.Manager;
using ASI.Basecode.Services.ServiceModels;
using AutoMapper;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static ASI.Basecode.Resources.Constants.Enums;

namespace ASI.Basecode.Services.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _repository;
        private readonly IReservationRepository _reservationRepository;
        private readonly IMapper _mapper;

        public UserService(IUserRepository repository, IReservationRepository reservationRepository, IMapper mapper)
        {
            _mapper = mapper;
            _repository = repository;
            _reservationRepository = reservationRepository;
        }

        public LoginResult AuthenticateUser(string userId, string password, ref User user)
        {
            user = new User();

            var passwordKey = PasswordManager.EncryptPassword(password);

            user = _repository.GetUsers()
                .FirstOrDefault(x => x.UserId == userId && x.PasswordHash == passwordKey);

            return user != null ? LoginResult.Success : LoginResult.Failed;
        }

        public void AddUser(UserViewModel model)
        {
            var user = new User();

            if (!_repository.UserExists(model.UserId))
            {
                _mapper.Map(model, user);

                user.UserId = model.UserId;
                user.FirstName = model.FirstName;
                user.MiddleName = model.MiddleName;
                user.LastName = model.LastName;
                user.ContactNumber = model.ContactNumber;
                user.Email = model.Email;
                user.Role = string.IsNullOrWhiteSpace(model.Role) ? "Custodian" : model.Role;
                user.PasswordHash = PasswordManager.EncryptPassword(model.Password);

                user.BorrowerStatus = "eligible";
                user.IsActive = true;

                user.CreatedTime = DateTime.Now;
                user.UpdatedTime = DateTime.Now;
                user.CreatedBy = Environment.UserName;
                user.UpdatedBy = Environment.UserName;

                _repository.AddUser(user);
            }
            else
            {
                throw new InvalidDataException(Resources.Messages.Errors.UserExists);
            }
        }

        public bool ChangePassword(string userId, string currentPassword, string newPassword)
        {
            var user = _repository.GetUsers()
                .FirstOrDefault(x => x.UserId == userId);

            if (user == null)
                return false;

            if (user.PasswordHash != PasswordManager.EncryptPassword(currentPassword))
                return false;

            user.PasswordHash = PasswordManager.EncryptPassword(newPassword);
            user.UpdatedTime = DateTime.Now;
            user.UpdatedBy = Environment.UserName;

            _repository.UpdateUser(user);

            return true;
        }

        public UserManagementViewModel GetUserManagementData(string search, string role, string status)
        {
            var query = _repository.GetUsers();

            var totalUsers = query.Count();
            var totalBorrowers = query.Count(u => u.Role == "User" || u.Role == "Student");
            var totalCustodians = query.Count(u => u.Role == "Custodian");
            var totalAdmins = query.Count(u => u.Role == "Admin");

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim().ToLower();

                query = query.Where(u =>
                    (u.FirstName ?? "").ToLower().Contains(search) ||
                    (u.LastName ?? "").ToLower().Contains(search) ||
                    (u.Email ?? "").ToLower().Contains(search) ||
                    (u.UserId ?? "").ToLower().Contains(search));
            }

            if (!string.IsNullOrWhiteSpace(role))
            {
                query = query.Where(u => u.Role == role);
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                bool isActive = status.Equals("Active", StringComparison.OrdinalIgnoreCase);
                query = query.Where(u => u.IsActive == isActive);
            }

            var userList = query
                .OrderByDescending(u => u.CreatedTime)
                .Select(u => new UserListItemModel
                {
                    Id = u.Id,
                    UserId = u.UserId,
                    FullName = (u.FirstName + " " + u.LastName).Trim(),
                    Email = u.Email,
                    Role = u.Role,
                    IsActive = u.IsActive,
                    DateCreated = u.CreatedTime,
                    Initials =
                        ((!string.IsNullOrEmpty(u.FirstName) ? u.FirstName[0].ToString() : "") +
                         (!string.IsNullOrEmpty(u.LastName) ? u.LastName[0].ToString() : "")).ToUpper()
                })
                .ToList();

            return new UserManagementViewModel
            {
                TotalUsers = totalUsers,
                TotalBorrowers = totalBorrowers,
                TotalCustodians = totalCustodians,
                TotalAdministrators = totalAdmins,
                SearchTerm = search,
                SelectedRole = role,
                SelectedStatus = status,
                Users = userList
            };
        }

        // Get logged-in user's profile
        public ProfileViewModel GetProfile(string userId)
        {
            var user = _repository.GetUsers()
                .FirstOrDefault(u => u.UserId == userId);

            if (user == null)
            {
                return null;
            }

            return new ProfileViewModel
            {
                UserId = user.UserId,
                FirstName = user.FirstName,
                MiddleName = user.MiddleName,
                LastName = user.LastName,
                FullName = $"{user.FirstName} {user.MiddleName} {user.LastName}".Trim(),
                ContactNumber = user.ContactNumber,
                Email = user.Email,
                Role = user.Role,
                BorrowerStatus = user.BorrowerStatus,
                IsActive = user.IsActive,
                ProfileImagePath = user.ProfileImagePath
            };
        }

        // Current photo path (used to delete the old file after a new one is saved)
        public string GetProfileImagePath(string userId)
        {
            return _repository.GetUsers()
                .Where(u => u.UserId == userId)
                .Select(u => u.ProfileImagePath)
                .FirstOrDefault();
        }

        // Saves the editable profile fields and, when given, the new photo path
        public ServiceResult UpdateProfile(string userId, string contactNumber, string email, string newImagePath)
        {
            var user = _repository.GetUsers().FirstOrDefault(u => u.UserId == userId);
            if (user == null)
                return ServiceResult.Fail("Your account was not found. Please log in again.");

            if (!string.IsNullOrWhiteSpace(email))
            {
                var cleanEmail = email.Trim();
                try
                {
                    _ = new System.Net.Mail.MailAddress(cleanEmail);
                }
                catch (FormatException)
                {
                    return ServiceResult.Fail("Please enter a valid email address.");
                }
                user.Email = cleanEmail;
            }

            if (!string.IsNullOrWhiteSpace(contactNumber))
                user.ContactNumber = contactNumber.Trim();

            if (!string.IsNullOrEmpty(newImagePath))
                user.ProfileImagePath = newImagePath;

            user.UpdatedTime = DateTime.Now;
            user.UpdatedBy = userId;

            try
            {
                _repository.UpdateUser(user);
            }
            catch (Exception)
            {
                return ServiceResult.Fail("Could not save your profile. Please try again.");
            }

            return ServiceResult.Ok("Your profile was updated.");
        }
    }
}