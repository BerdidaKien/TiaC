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
using System.Text.RegularExpressions;
using static ASI.Basecode.Resources.Constants.Enums;

namespace ASI.Basecode.Services.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IReservationRepository _reservationRepository;
        private readonly IMapper _mapper;

        public UserService(IUserRepository userRepository, IReservationRepository reservationRepository, IMapper mapper)
        {
            _mapper = mapper;
            _userRepository = userRepository;
            _reservationRepository = reservationRepository;
        }

        public LoginResult AuthenticateUser(string userCode, string password, ref User user)
        {
            var passwordKey = PasswordManager.EncryptPassword(password);

            // Inactive and disabled accounts can't log in
            user = _userRepository.GetUsers()
                .FirstOrDefault(x => x.UserId == userCode &&
                                     x.PasswordHash == passwordKey &&
                                     x.IsActive &&
                                     x.AccountStatus == "active");

            return user != null ? LoginResult.Success : LoginResult.Failed;
        }

        public bool ChangePassword(string userCode, string currentPassword, string newPassword)
        {
            var user = _userRepository.GetUsers()
                .FirstOrDefault(x => x.UserId == userCode);

            if (user == null)
                return false;

            if (user.PasswordHash != PasswordManager.EncryptPassword(currentPassword))
                return false;

            user.PasswordHash = PasswordManager.EncryptPassword(newPassword);
            user.UpdatedTime = DateTime.Now;
            user.UpdatedBy = Environment.UserName;

            _userRepository.UpdateUser(user);

            return true;
        }

        public AccountAccess GetAccountAccess(string userCode)
        {
            var row = _userRepository.GetUsers()
                .Where(u => u.UserId == userCode)
                .Select(u => new { u.IsActive, u.AccountStatus, u.MustCompleteSetup })
                .FirstOrDefault();

            if (row == null || !row.IsActive || row.AccountStatus != AccountStatuses.Active)
                return AccountAccess.Blocked;

            return row.MustCompleteSetup ? AccountAccess.MustSetUp : AccountAccess.Allowed;
        }

        public bool MustCompleteSetup(string userCode)
        {
            return _userRepository.GetUsers().Any(u => u.UserId == userCode && u.MustCompleteSetup);
        }

        // Sets the user's own password, then lifts the setup requirement.
        // The rules are checked here, so they apply even if the browser checks are skipped.
        public ServiceResult CompleteFirstLogin(string userCode, string newPassword)
        {
            var user = _userRepository.GetUsers().FirstOrDefault(u => u.UserId == userCode);
            if (user == null)
                return ServiceResult.Fail("Your account was not found. Please log in again.");

            var passwordError = UserFieldRules.CheckPassword(newPassword ?? "");
            if (passwordError != null)
                return ServiceResult.Fail(passwordError);

            var newHash = PasswordManager.EncryptPassword(newPassword);
            if (newHash == user.PasswordHash)
                return ServiceResult.Fail("Choose a new password that is different from your temporary one.");

            user.PasswordHash = newHash;
            user.MustCompleteSetup = false;
            user.UpdatedBy = userCode;
            user.UpdatedTime = DateTime.Now;

            try
            {
                _userRepository.UpdateUser(user);
            }
            catch (Exception)
            {
                return ServiceResult.Fail("Could not save your new password. Please try again.");
            }

            return ServiceResult.Ok("Your password was updated. Welcome!");
        }

        public UserManagementViewModel GetUserManagementData(string search, string role, string status)
        {
            var query = _userRepository.GetUsers();

            var totalUsers = query.Count();
            var totalBorrowers = query.Count(u => u.Role == "User" || u.Role == "Student");
            var totalCustodians = query.Count(u => u.Role == "Custodian");
            var totalAdmins = query.Count(u => u.Role == "Admin");

            var searchTerm = search;   // shown back in the search box exactly as typed

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();

                query = query.Where(u =>
                    (u.FirstName ?? "").ToLower().Contains(term) ||
                    (u.LastName ?? "").ToLower().Contains(term) ||
                    (u.Email ?? "").ToLower().Contains(term) ||
                    (u.UserId ?? "").ToLower().Contains(term));
            }

            if (!string.IsNullOrWhiteSpace(role))
            {
                query = query.Where(u => u.Role == role);
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                var wanted = status.Trim().ToLowerInvariant();
                if (AccountStatuses.All.Contains(wanted))
                {
                    query = query.Where(u => u.AccountStatus == wanted);
                }
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
                    Status = u.AccountStatus,
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
                SearchTerm = searchTerm,
                SelectedRole = role,
                SelectedStatus = status,
                Users = userList
            };
        }

        // ---------- Custodian profile ----------
        public CustodianProfileViewModel GetCustodianProfile(string userCode)
        {
            var user = _userRepository.GetUsers().FirstOrDefault(u => u.UserId == userCode);
            if (user == null) return null;

            int approvedCount = 0;
            int completedCount = 0;

            if (_reservationRepository != null)
            {
                // The requests this custodian approved, and the returns this custodian received
                approvedCount = _reservationRepository.GetReservations()
                    .Count(r => r.ReviewedBy == user.Id && r.Status == "approved");

                completedCount = _reservationRepository.GetTransactions()
                    .Count(t => t.ReturnedTo == user.Id && t.ActualReturnTime != null);
            }

            return new CustodianProfileViewModel
            {
                Id = user.Id,
                FirstName = user.FirstName,
                MiddleName = user.MiddleName,
                LastName = user.LastName,
                EmployeeId = user.UserId,
                Email = user.Email,
                ContactNumber = user.ContactNumber,
                Role = user.Role == "Custodian" ? "Equipment Custodian" : user.Role,
                IsActive = user.IsActive,
                CreatedTime = user.CreatedTime,
                ProfileImagePath = user.ProfileImagePath,
                ApprovedRequestsCount = approvedCount,
                CompletedBorrowingsCount = completedCount
            };
        }

        // ---------- Profile (student and custodian) ----------
        public ProfileViewModel GetProfile(string userCode)
        {
            var user = _userRepository.GetUsers()
                .FirstOrDefault(u => u.UserId == userCode);

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
        public string GetProfileImagePath(string userCode)
        {
            return _userRepository.GetUsers()
                .Where(u => u.UserId == userCode)
                .Select(u => u.ProfileImagePath)
                .FirstOrDefault();
        }

        // Users can change only their contact number and photo here.
        // Name, ID and email go through an information change request that an admin approves.
        // (The email parameter is kept so existing callers still compile, but it is ignored.)
        public ServiceResult UpdateProfile(string userCode, string contactNumber, string email, string newImagePath)
        {
            var user = _userRepository.GetUsers().FirstOrDefault(u => u.UserId == userCode);
            if (user == null)
                return ServiceResult.Fail("Your account was not found. Please log in again.");

            // Same contact rule as Add Users. Only a changed number is checked, so saving a new photo
            // never fails because of a number saved before this rule existed.
            var contact = UserFieldRules.NormalizeContact(contactNumber);
            if (contact != UserFieldRules.NormalizeContact(user.ContactNumber))
            {
                if (contact.Length == 0)
                    return ServiceResult.Fail("Contact number can't be removed. Enter a new one to replace it.");

                var contactError = UserFieldRules.CheckContact(contact);
                if (contactError != null)
                    return ServiceResult.Fail(contactError);

                user.ContactNumber = contact;
            }

            if (!string.IsNullOrEmpty(newImagePath))
                user.ProfileImagePath = newImagePath;

            user.UpdatedTime = DateTime.Now;
            user.UpdatedBy = userCode;

            try
            {
                _userRepository.UpdateUser(user);
            }
            catch (Exception)
            {
                return ServiceResult.Fail("Could not save your profile. Please try again.");
            }

            return ServiceResult.Ok("Your profile was updated.");
        }
    }
}