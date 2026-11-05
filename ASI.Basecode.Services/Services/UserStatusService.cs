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
        private readonly IAccountAdminRepository _repo;

        public UserStatusService(IAccountAdminRepository repo)
        {
            _repo = repo;
        }

        public UserEditViewModel GetEditPage(int id, string adminCode)
        {
            var u = _repo.GetUserById(id);
            if (u == null) return null;

            var requests = _repo.GetRequests()
                .Where(r => r.UserId == id)
                .OrderByDescending(r => r.CreatedTime)
                .Take(20)
                .ToList();

            var isLastAdmin = u.Role == "Admin" &&
                              u.AccountStatus == AccountStatuses.Active &&
                              !_repo.GetUsers().Any(o => o.Id != u.Id && o.Role == "Admin" &&
                                                         o.IsActive && o.AccountStatus == "active");

            return new UserEditViewModel
            {
                Id = u.Id,
                UserCode = u.UserId,
                FullName = InfoRequestMapper.FullName(u),
                Email = u.Email,
                ContactNumber = u.ContactNumber,
                Role = u.Role,
                Status = u.AccountStatus,
                CreatedTime = u.CreatedTime,
                ItemsOut = _repo.GetTransactions().Count(t => t.BorrowerId == id && t.ActualReturnTime == null),
                OpenRequests = OpenReservations(id).Count(),
                IsSelf = u.UserId == adminCode,
                IsLastActiveAdmin = isLastAdmin,
                Requests = requests.Select(InfoRequestMapper.ToRow).ToList()
            };
        }

        public ServiceResult ChangeStatus(int id, string newStatus, string adminCode)
        {
            newStatus = (newStatus ?? "").Trim().ToLowerInvariant();
            if (!AccountStatuses.All.Contains(newStatus))
                return ServiceResult.Fail("Please choose a valid status.");

            var user = _repo.GetUserById(id);
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
                    !_repo.GetUsers().Any(o => o.Id != user.Id && o.Role == "Admin" &&
                                               o.IsActive && o.AccountStatus == "active"))
                    return ServiceResult.Fail("This is the only active administrator. Create or activate another admin first.");

                var itemsOut = _repo.GetTransactions().Count(t => t.BorrowerId == id && t.ActualReturnTime == null);
                if (itemsOut > 0)
                    return ServiceResult.Fail($"{name} still has {itemsOut} item(s) that haven't been returned. Process the return first.");

                // Requests that can no longer be picked up are cancelled
                var open = OpenReservations(id).ToList();
                foreach (var r in open)
                {
                    r.Status = "cancelled";
                    r.UpdatedTime = now;
                    if (string.IsNullOrWhiteSpace(r.ReviewNotes))
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
                _repo.SaveChanges();
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

        // Pending requests, plus approved ones that haven't been picked up yet
        private IQueryable<Data.Models.Reservation> OpenReservations(int userId)
        {
            return _repo.GetReservations().Where(r =>
                r.BorrowerId == userId &&
                (r.Status == "pending" || (r.Status == "approved" && r.BorrowingTransaction == null)));
        }
    }
}