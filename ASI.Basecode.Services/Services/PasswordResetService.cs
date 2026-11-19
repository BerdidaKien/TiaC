using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ASI.Basecode.Data.Interfaces;
using ASI.Basecode.Data.Models;
using ASI.Basecode.Services.Interfaces;
using ASI.Basecode.Services.Manager;
using System.Security.Cryptography;
using System.Net;


namespace ASI.Basecode.Services.Services
{
    public class PasswordResetService : IPasswordResetService
    {
        private const int ExpiryMinutes = 30;

        private readonly IUserRepository _userRepository;
        private readonly IPasswordResetRepository _passwordResetRepository;
        private readonly IEmailService _emailService;

        public PasswordResetService(IUserRepository userRepository, IPasswordResetRepository passwordResetRepository, IEmailService emailService)
        {
            _userRepository = userRepository;
            _passwordResetRepository = passwordResetRepository;
            _emailService = emailService;
        }

        public void RequestReset(string userCode, Func<string, string> buildLink)
        {
            if (string.IsNullOrWhiteSpace(userCode)) return;

            var user = _userRepository.GetUsers().FirstOrDefault(u => u.UserId == userCode.Trim());
            if (user == null || string.IsNullOrWhiteSpace(user.Email)) return;

            // Cancel any older unused links for this user
            var now = DateTime.Now;
            var old = _passwordResetRepository.GetTokens().Where(t => t.UserId == user.UserId && t.UsedAt == null).ToList();
            foreach (var t in old)
            {
                t.UsedAt = now;
                _passwordResetRepository.UpdateToken(t);
            }

            // Random token; only its hash is stored
            var tokenBytes = RandomNumberGenerator.GetBytes(32);
            var token = Convert.ToBase64String(tokenBytes)
                .Replace('+', '-').Replace('/', '_').TrimEnd('=');

            _passwordResetRepository.AddToken(new PasswordResetToken
            {
                UserId = user.UserId,
                TokenHash = Hash(token),
                ExpiresAt = now.AddMinutes(ExpiryMinutes),
                CreatedTime = now
            });

            var link = buildLink(token);
            var name = WebUtility.HtmlEncode(user.FirstName ?? user.UserId);

            var body = $@"
                <p>Hi {name},</p>
                <p>We received a request to reset your TiaC password.</p>
                <p><a href=""{link}"">Click here to set a new password</a></p>
                <p>This link expires in {ExpiryMinutes} minutes. If you didn't ask for this, you can ignore this email.</p>";

            _emailService.Send(user.Email, "Reset your TiaC password", body);
        }

        public bool ResetPassword(string token, string newPassword)
        {
            if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(newPassword)) return false;

            var hash = Hash(token);
            var now = DateTime.Now;

            var record = _passwordResetRepository.GetTokens()
                .FirstOrDefault(t => t.TokenHash == hash && t.UsedAt == null && t.ExpiresAt > now);
            if (record == null) return false;

            var user = _userRepository.GetUsers().FirstOrDefault(u => u.UserId == record.UserId);
            if (user == null) return false;

            user.PasswordHash = PasswordManager.EncryptPassword(newPassword);
            user.MustCompleteSetup = false;   // they chose their own password, so first-login setup is done
            user.UpdatedTime = now;
            user.UpdatedBy = user.UserId;
            _userRepository.UpdateUser(user);

            record.UsedAt = now;
            _passwordResetRepository.UpdateToken(record);

            return true;
        }

        private static string Hash(string token)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
            return Convert.ToHexString(bytes).ToLowerInvariant(); // 64 chars
        }
    }
}