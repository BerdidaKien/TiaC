using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using ASI.Basecode.Data.Interfaces;
using ASI.Basecode.Data.Models;
using ASI.Basecode.Services.Interfaces;
using ASI.Basecode.Services.ServiceModels;
using Microsoft.EntityFrameworkCore;

namespace ASI.Basecode.Services.Services
{
    public class InfoRequestService : IInfoRequestService
    {
        private const int PageSize = 10;
        private const int MaxFiles = 3;

        private readonly IAccountAdminRepository _repo;

        public InfoRequestService(IAccountAdminRepository repo)
        {
            _repo = repo;
        }

        // ---------- Student / custodian side ----------
        public InfoRequestFormViewModel GetForm(string userCode)
        {
            var u = _repo.GetUserByCode(userCode);
            if (u == null) return null;

            var rows = _repo.GetRequests()
                .Where(r => r.UserId == u.Id)
                .OrderByDescending(r => r.CreatedTime)
                .Take(10)
                .ToList()
                .Select(InfoRequestMapper.ToRow)
                .ToList();

            return new InfoRequestFormViewModel
            {
                UserCode = u.UserId,
                Role = u.Role,
                IdLabel = (u.Role == "Custodian" || u.Role == "Admin") ? "Employee ID" : "Student ID",
                FirstName = u.FirstName,
                MiddleName = u.MiddleName ?? "",
                LastName = u.LastName,
                Email = u.Email,
                Pending = rows.FirstOrDefault(r => r.Status == "pending"),
                History = rows
            };
        }

        public ServiceResult Submit(string userCode, SubmitInfoRequest model, IList<StoredProof> proofs)
        {
            var u = _repo.GetUserByCode(userCode);
            if (u == null) return ServiceResult.Fail("Your session expired. Please log in again.");
            if (!u.IsActive || u.AccountStatus != AccountStatuses.Active)
                return ServiceResult.Fail("Your account can't send requests right now.");
            if (model == null) return ServiceResult.Fail("Please fill in the form.");

            if (_repo.GetRequests().Any(r => r.UserId == u.Id && r.Status == "pending"))
                return ServiceResult.Fail("You already have a request waiting for review. Cancel it first if you want to send a new one.");

            var submitted = new Dictionary<string, string>
            {
                ["first_name"] = InfoFields.Normalize("first_name", model.FirstName),
                ["middle_name"] = InfoFields.Normalize("middle_name", model.MiddleName),
                ["last_name"] = InfoFields.Normalize("last_name", model.LastName),
                ["user_code"] = InfoFields.Normalize("user_code", model.UserCode),
                ["email"] = InfoFields.Normalize("email", model.Email)
            };

            // Only the fields that were changed become part of the request
            var items = new List<InfoChangeRequestItem>();
            foreach (var f in InfoFields.All)
            {
                var oldValue = InfoFields.Read(u, f.Key) ?? "";
                var newValue = submitted[f.Key];
                if (string.Equals(oldValue, newValue, StringComparison.Ordinal)) continue;

                var error = InfoFields.Validate(f.Key, newValue);
                if (error != null) return ServiceResult.Fail(error);

                items.Add(new InfoChangeRequestItem { FieldKey = f.Key, OldValue = oldValue, NewValue = newValue });
            }

            if (items.Count == 0)
                return ServiceResult.Fail("You didn't change anything. Edit the details that are wrong, then send the request.");

            var reason = (model.Reason ?? "").Trim();
            if (reason.Length < 10 || reason.Length > 500)
                return ServiceResult.Fail("Please explain what is wrong in 10 to 500 characters.");

            if (proofs == null || proofs.Count == 0)
                return ServiceResult.Fail("Please attach at least one photo of a document that shows your correct details.");
            if (proofs.Count > MaxFiles)
                return ServiceResult.Fail($"You can attach up to {MaxFiles} photos.");

            var newCode = items.FirstOrDefault(i => i.FieldKey == "user_code")?.NewValue;
            if (newCode != null && _repo.GetUsers().Any(x => x.Id != u.Id && x.UserId == newCode))
                return ServiceResult.Fail("That ID is not available.");

            var newEmail = items.FirstOrDefault(i => i.FieldKey == "email")?.NewValue;
            if (newEmail != null && _repo.GetUsers().Any(x => x.Id != u.Id && x.Email == newEmail))
                return ServiceResult.Fail("That email address is not available.");

            var now = DateTime.Now;
            var request = new InfoChangeRequest
            {
                UserId = u.Id,
                Reason = reason,
                Status = "pending",
                CreatedTime = now,
                UpdatedTime = now
            };

            foreach (var item in items) request.Items.Add(item);
            foreach (var p in proofs)
            {
                request.Files.Add(new InfoChangeRequestFile
                {
                    StoredName = p.StoredName,
                    OriginalName = p.OriginalName,
                    ContentType = p.ContentType,
                    SizeBytes = p.SizeBytes,
                    UploadedTime = now
                });
            }

            _repo.AddRequest(request);

            try
            {
                _repo.SaveChanges();
            }
            catch (DbUpdateException)
            {
                return ServiceResult.Fail("Could not send your request. If you already have one waiting for review, please wait for the result.");
            }

            return ServiceResult.Ok("Request sent. An administrator will review it, and you'll see the result on this page.");
        }

        public ServiceResult Cancel(int requestId, string userCode)
        {
            var u = _repo.GetUserByCode(userCode);
            if (u == null) return ServiceResult.Fail("Your session expired. Please log in again.");

            var r = _repo.GetRequestById(requestId);
            if (r == null || r.UserId != u.Id) return ServiceResult.Fail("Request not found.");
            if (r.Status != "pending") return ServiceResult.Fail("Only a request that is waiting for review can be cancelled.");

            r.Status = "cancelled";
            r.UpdatedTime = DateTime.Now;

            try
            {
                _repo.SaveChanges();
            }
            catch (DbUpdateException)
            {
                return ServiceResult.Fail("Could not cancel the request. Please try again.");
            }

            return ServiceResult.Ok("Your request was cancelled.");
        }

        // ---------- Admin side ----------
        public AdminInfoRequestsViewModel GetAdminPage(string status, string search, int page)
        {
            var all = _repo.GetRequests();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                var codeId = ParseCode(term);

                all = all.Where(r =>
                    r.Id == codeId ||
                    r.User.UserId.Contains(term) ||
                    (r.User.FirstName + " " + r.User.LastName).Contains(term));
            }

            var byStatus = all.GroupBy(r => r.Status)
                              .Select(g => new { Status = g.Key, Count = g.Count() })
                              .ToList();
            int CountOf(string s) => byStatus.Where(x => x.Status == s).Sum(x => x.Count);

            var filtered = all;
            if (status == "pending" || status == "approved" || status == "rejected")
                filtered = filtered.Where(r => r.Status == status);

            var totalRows = filtered.Count();
            var totalPages = Math.Max(1, (int)Math.Ceiling(totalRows / (double)PageSize));
            page = Math.Min(Math.Max(1, page), totalPages);

            // Oldest first while reviewing, newest first everywhere else
            var ordered = status == "pending"
                ? filtered.OrderBy(r => r.CreatedTime).ThenBy(r => r.Id)
                : filtered.OrderByDescending(r => r.CreatedTime).ThenByDescending(r => r.Id);

            var rows = ordered.Skip((page - 1) * PageSize).Take(PageSize)
                              .ToList()
                              .Select(InfoRequestMapper.ToRow)
                              .ToList();

            return new AdminInfoRequestsViewModel
            {
                Status = status,
                Search = search,
                PendingCount = CountOf("pending"),
                ApprovedCount = CountOf("approved"),
                RejectedCount = CountOf("rejected"),
                TotalCount = byStatus.Sum(x => x.Count),
                Rows = rows,
                Page = page,
                PageSize = PageSize,
                TotalRows = totalRows,
                TotalPages = totalPages
            };
        }

        public InfoRequestRow GetRequestRow(int id)
        {
            var r = _repo.GetRequestById(id);
            return r == null ? null : InfoRequestMapper.ToRow(r);
        }

        public ServiceResult Approve(int requestId, IDictionary<string, string> values, string note, string adminCode)
        {
            var admin = _repo.GetUserByCode(adminCode);
            if (admin == null) return ServiceResult.Fail("Your session expired. Please log in again.");

            var r = _repo.GetRequestById(requestId);
            if (r == null) return ServiceResult.Fail("Request not found.");
            if (r.Status != "pending") return ServiceResult.Fail("This request was already handled.");

            note = (note ?? "").Trim();
            if (note.Length > 255) return ServiceResult.Fail("The note must be 255 characters or fewer.");

            var user = r.User;

            // The admin may correct the requested values before approving
            var finals = new Dictionary<string, string>();
            foreach (var item in r.Items)
            {
                var raw = (values != null && values.TryGetValue(item.FieldKey, out var typed)) ? typed : item.NewValue;
                var final = InfoFields.Normalize(item.FieldKey, raw);

                var error = InfoFields.Validate(item.FieldKey, final);
                if (error != null) return ServiceResult.Fail(error);

                finals[item.FieldKey] = final;
            }

            if (finals.TryGetValue("user_code", out var code) &&
                _repo.GetUsers().Any(x => x.Id != user.Id && x.UserId == code))
                return ServiceResult.Fail($"The ID \"{code}\" is already used by another account.");

            if (finals.TryGetValue("email", out var email) &&
                _repo.GetUsers().Any(x => x.Id != user.Id && x.Email == email))
                return ServiceResult.Fail($"The email \"{email}\" is already used by another account.");

            var now = DateTime.Now;
            foreach (var item in r.Items)
            {
                InfoFields.Apply(user, item.FieldKey, finals[item.FieldKey]);
                item.ApprovedValue = finals[item.FieldKey];
            }

            user.UpdatedBy = adminCode;
            user.UpdatedTime = now;

            r.Status = "approved";
            r.ReviewedBy = admin.Id;
            r.ReviewedAt = now;
            r.ReviewNote = note.Length == 0 ? null : note;
            r.UpdatedTime = now;

            try
            {
                _repo.SaveChanges();
            }
            catch (DbUpdateException)
            {
                return ServiceResult.Fail("Could not save the changes. The ID or email may already be in use.");
            }

            return ServiceResult.Ok($"Request approved. {InfoRequestMapper.FullName(user)}'s account was updated.");
        }

        public ServiceResult Reject(int requestId, string reason, string adminCode)
        {
            var admin = _repo.GetUserByCode(adminCode);
            if (admin == null) return ServiceResult.Fail("Your session expired. Please log in again.");

            var r = _repo.GetRequestById(requestId);
            if (r == null) return ServiceResult.Fail("Request not found.");
            if (r.Status != "pending") return ServiceResult.Fail("This request was already handled.");

            reason = (reason ?? "").Trim();
            if (reason.Length < 3 || reason.Length > 255)
                return ServiceResult.Fail("Please give a reason (3 to 255 characters).");

            var now = DateTime.Now;
            r.Status = "rejected";
            r.ReviewedBy = admin.Id;
            r.ReviewedAt = now;
            r.ReviewNote = reason;
            r.UpdatedTime = now;

            try
            {
                _repo.SaveChanges();
            }
            catch (DbUpdateException)
            {
                return ServiceResult.Fail("Could not reject the request. Please try again.");
            }

            return ServiceResult.Ok("Request rejected. The user can see your reason and send a new request.");
        }

        public InfoChangeRequestFile GetFile(int fileId) => _repo.GetFileById(fileId);

        public int PendingCount() => _repo.GetRequests().Count(r => r.Status == "pending");

        private static int ParseCode(string term)
        {
            var m = Regex.Match(term, @"^REQ-\d{4}-(\d+)$", RegexOptions.IgnoreCase);
            return m.Success && int.TryParse(m.Groups[1].Value, out var n) ? n : -1;
        }
    }
}