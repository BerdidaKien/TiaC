using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASI.Basecode.Services.Interfaces
{
    public interface IPasswordResetService
    {
        /// Creates a token and emails a link. Does nothing (silently) if the user or email doesn't exist.
        void RequestReset(string userId, Func<string, string> buildLink);

        /// Returns true if the token was valid and the password was changed.
        bool ResetPassword(string token, string newPassword);
    }
}