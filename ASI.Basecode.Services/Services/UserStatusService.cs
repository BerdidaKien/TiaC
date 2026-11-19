using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ASI.Basecode.Data.Interfaces;
using ASI.Basecode.Services.Interfaces;
using ASI.Basecode.Services.ServiceModels;
using Microsoft.EntityFrameworkCore;

namespace ASI.Basecode.Services.Services
{
    public class UserStatusService : IUserStatusService
    {
        private readonly IAccountAdminRepository _accountAdminRepository;

        public UserStatusService(IAccountAdminRepository accountAdminRepository)
        {
            _accountAdminRepository = accountAdminRepository;
        }

        public UserEditViewModel GetEditPage(int id, string adminCode)
        {
            var user = _accountAdminRepository.GetUserById(id);
            if (user == null) return null;

            var requests = _accountAdminRepository.GetRequests()
                .Where(r => r.UserId == id)
                .OrderByDescending(r => r.CreatedTime)
                .Take(20)
                .ToList();

            var isLastAdmin = user.Role == "Admin" &&
                              user.AccountStatus == AccountStatuses.Active &&
                              !_accountAdminRepository.GetUsers().Any(o => o.Id != user.Id && o.Role == "Admin" &&
                                                         o.IsActive && o.AccountStatus == "active");

            return new UserEditViewModel
            {
                Id = user.Id,
                UserCode = user.UserId,
                FullName = InfoRequestMapper.FullName(user),
                Email = user.Email,
                ContactNumber = user.ContactNumber,
                Role = user.Role,
                Status = user.AccountStatus,
                CreatedTime = user.CreatedTime,
                ItemsOut = OpenLoans(user).Count(),
                OpenRequests = OpenReservations(user).Count(),
                IsSelf = user.UserId == adminCode,
                IsLastActiveAdmin = isLastAdmin,
                Requests = requests.Select(InfoRequestMapper.ToRow).ToList()
            };
        }

        public ServiceResult ChangeStatus(int id, string newStatus, string adminCode)
        {
            newStatus = (newStatus ?? "").Trim().ToLowerInvariant();
            if (!AccountStatuses.All.Contains(newStatus))
                return ServiceResult.Fail("Please choose a valid status.");

            var user = _accountAdminRepository.GetUserById(id);
            if (user == null) return ServiceResult.Fail("User not found.");

            var name = InfoRequestMapper.FullName(user);

            if (user.AccountStatus == newStatus && user.IsActive == (newStatus == AccountStatuses.Active))
                return ServiceResult.Fail($"{name}'s account is already {AccountStatuses.Label(newStatus).ToLower()}.");

            var leaving = newStatus != AccountStatuses.Active;
            var cancelled = 0;
            var now = DateTime.Now;

            if (leaving)
            {
                if (user.UserId == adminCode)
                    return ServiceResult.Fail("You can't deactivate or disable your own account.");

                if (user.Role == "Admin" && user.AccountStatus == AccountStatuses.Active &&
                    !_accountAdminRepository.GetUsers().Any(o => o.Id != user.Id && o.Role == "Admin" &&
                                               o.IsActive && o.AccountStatus == "active"))
                    return ServiceResult.Fail("This is the only active administrator. Create or activate another admin first.");

                var isCustodian = user.Role == "Custodian";

                // A borrower can't leave with items out; a custodian can't leave while their equipment is on loan,
                // because only the owning custodian can receive the return
                var itemsOut = OpenLoans(user).Count();
                if (itemsOut > 0)
                    return ServiceResult.Fail(isCustodian
                        ? $"{name} still has {itemsOut} item(s) out on loan. Process the return first."
                        : $"{name} still has {itemsOut} item(s) that haven't been returned. Process the return first.");

                // Requests that can no longer be picked up (or approved) are cancelled
                var open = OpenReservations(user).ToList();
                foreach (var r in open)
                {
                    r.Status = "cancelled";
                    r.UpdatedTime = now;
                    if (isCustodian)
                        r.ReviewNotes = "Cancelled because the equipment's custodian account was " + newStatus + ".";
                    else if (string.IsNullOrWhiteSpace(r.ReviewNotes))
                        r.ReviewNotes = "Cancelled because the borrower's account was " + newStatus + ".";
                }
                cancelled = open.Count;
            }

            user.AccountStatus = newStatus;
            user.IsActive = newStatus == AccountStatuses.Active;
            user.UpdatedBy = adminCode;
            user.UpdatedTime = now;

            try
            {
                _accountAdminRepository.SaveChanges();
            }
            catch (DbUpdateException)
            {
                return ServiceResult.Fail("Could not change the status. Please try again.");
            }

            string message;
            switch (newStatus)
            {
                case AccountStatuses.Active:
                    message = $"{name} is active again and can log in.";
                    break;
                case AccountStatuses.Inactive:
                    message = $"{name} is now inactive and can't log in until reactivated.";
                    break;
                default:
                    message = $"{name} is now disabled. They can't log in, and their records are kept.";
                    break;
            }

            if (cancelled > 0) message += $" {cancelled} open reservation(s) were cancelled.";
            return ServiceResult.Ok(message);
        }

        // Pending requests, plus approved ones that haven't been picked up yet.
        // For a custodian these are the requests for the equipment they own; for everyone else, their own requests.
        private IQueryable<Data.Models.Reservation> OpenReservations(Data.Models.User user)
        {
            var open = _accountAdminRepository.GetReservations().Where(r =>
                r.Status == "pending" || (r.Status == "approved" && r.BorrowingTransaction == null));

            return user.Role == "Custodian"
                ? open.Where(r => r.EquipmentItem.CustodianId == user.Id)
                : open.Where(r => r.BorrowerId == user.Id);
        }

        // Loans not returned yet: items the user holds, or (for a custodian) their equipment that is out on loan
        private IQueryable<Data.Models.BorrowingTransaction> OpenLoans(Data.Models.User user)
        {
            var loans = _accountAdminRepository.GetTransactions().Where(t => t.ActualReturnTime == null);

            return user.Role == "Custodian"
                ? loans.Where(t => t.EquipmentItem.CustodianId == user.Id)
                : loans.Where(t => t.BorrowerId == user.Id);
        }
    }
}