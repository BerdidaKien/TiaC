using ASI.Basecode.Data.Interfaces;
using ASI.Basecode.Data.Models;
using ASI.Basecode.Services.Interfaces;
using ASI.Basecode.Services.Manager;
using ASI.Basecode.Services.ServiceModels;
using AutoMapper;
using System;
using System.IO;
using System.Linq;
using static ASI.Basecode.Resources.Constants.Enums;

namespace ASI.Basecode.Services.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _repository;
        private readonly IMapper _mapper;

        public UserService(IUserRepository repository, IMapper mapper)
        {
            _mapper = mapper;
            _repository = repository;
        }

        public LoginResult AuthenticateUser(string userId, string password, ref User user)
        {
            user = new User();
            var passwordKey = PasswordManager.EncryptPassword(password);
            user = _repository.GetUsers().Where(x => x.UserId == userId &&
                                                     x.PasswordHash == passwordKey).FirstOrDefault();

            return user != null ? LoginResult.Success : LoginResult.Failed;
        }

        public void AddUser(UserViewModel model)
        {
            var user = new User();
            if (!_repository.UserExists(model.UserId))
            {
                // Map properties from UserViewModel to User entity
                _mapper.Map(model, user);

                // Explicit field assignments matching the new schema
                user.UserId = model.UserId;
                user.FirstName = model.FirstName;
                user.MiddleName = model.MiddleName;
                user.LastName = model.LastName;
                user.ContactNumber = model.ContactNumber;
                user.Email = model.Email;
                user.Role = string.IsNullOrWhiteSpace(model.Role) ? "Custodian" : model.Role;
                user.PasswordHash = PasswordManager.EncryptPassword(model.Password);

                // Account Status Defaults
                user.BorrowerStatus = "Active";
                user.IsActive = true;

                // Audit Metadata
                user.CreatedTime = DateTime.Now;
                user.UpdatedTime = DateTime.Now;
                user.CreatedBy = System.Environment.UserName;
                user.UpdatedBy = System.Environment.UserName;

                _repository.AddUser(user);
            }
            else
            {
                throw new InvalidDataException(Resources.Messages.Errors.UserExists);
            }
        }

        public bool ChangePassword(string userId, string currentPassword, string newPassword)
        {
            var user = _repository.GetUsers().FirstOrDefault(x => x.UserId == userId);
            if (user == null) return false;

            // Wrong current password -> reject
            if (user.PasswordHash != PasswordManager.EncryptPassword(currentPassword)) return false;

            // Save the new password encrypted the same way login compares it
            user.PasswordHash = PasswordManager.EncryptPassword(newPassword);
            user.UpdatedTime = DateTime.Now;
            user.UpdatedBy = System.Environment.UserName;

            _repository.UpdateUser(user);
            return true;
        }
    }
}