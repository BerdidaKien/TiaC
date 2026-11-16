using System;
using System.Linq;
using ASI.Basecode.Data.Interfaces;
using ASI.Basecode.Services.Interfaces;
using ASI.Basecode.Services.ServiceModels;

namespace ASI.Basecode.Services.Services
{
    public class PenaltyService : IPenaltyService
    {
        private readonly IReservationRepository _repo;

        public PenaltyService(IReservationRepository repo)
        {
            _repo = repo;
        }

        public ServiceResult Resolve(int penaltyId, string custodianCode)
        {
            var custodianId = _repo.GetUserDbId(custodianCode);
            if (custodianId == 0) return ServiceResult.Fail("Your session expired. Please log in again.");

            var p = _repo.GetPenalties().FirstOrDefault(x => x.Id == penaltyId);
            if (p == null) return ServiceResult.Fail("Penalty not found.");
            if (p.Transaction?.EquipmentItem?.CustodianId != custodianId)
                return ServiceResult.Fail("This penalty belongs to another custodian's equipment.");
            if (p.Status != PenaltyRules.Unresolved)
                return ServiceResult.Fail("This penalty is already settled.");

            p.Status = PenaltyRules.Resolved;
            p.ResolvedTime = DateTime.Now;
            p.ResolvedBy = custodianId;
            _repo.SaveChanges();

            return ServiceResult.Ok(p.PenaltyType == PenaltyRules.Late
                ? "Late fee marked as paid."
                : "Replacement confirmed.");
        }

        public BorrowerPenaltySummary GetSummary(int userDbId)
        {
            var user = _repo.GetUserByDbId(userDbId);

            var open = _repo.GetPenalties()
                .Where(p => p.BorrowerId == userDbId && p.Status == PenaltyRules.Unresolved)
                .ToList();

            var cutoff = DateTime.Now.AddDays(-PenaltyRules.SettleLateWithinDays);
            var unreturned = _repo.GetTransactions().Count(t =>
                t.BorrowerId == userDbId && t.ActualReturnTime == null &&
                t.Reservation.EstimatedReturnDate < cutoff);

            return new BorrowerPenaltySummary
            {
                Restricted = user?.BorrowerStatus == PenaltyRules.Restricted,
                UnresolvedCount = open.Count,
                LateTotal = open.Where(p => p.PenaltyType == PenaltyRules.Late).Sum(p => p.Amount),
                ReplacementCount = open.Count(p => p.PenaltyType == PenaltyRules.Damage),
                UnreturnedOverdue = unreturned
            };
        }

        public ServiceResult LiftRestriction(int userDbId, string adminCode)
        {
            var user = _repo.GetUserByDbId(userDbId);
            if (user == null) return ServiceResult.Fail("User not found.");
            if (user.BorrowerStatus != PenaltyRules.Restricted)
                return ServiceResult.Fail("This account is not restricted.");

            var s = GetSummary(userDbId);
            if (s.UnresolvedCount > 0)
                return ServiceResult.Fail($"This student still has {s.UnresolvedCount} unresolved penalty(ies). The custodian must mark them paid or replaced first.");
            if (s.UnreturnedOverdue > 0)
                return ServiceResult.Fail("This student still has equipment out more than 3 days past its due date.");

            user.BorrowerStatus = PenaltyRules.Eligible;
            user.UpdatedTime = DateTime.Now;
            user.UpdatedBy = adminCode;
            _repo.SaveChanges();

            return ServiceResult.Ok("Borrowing restored. The student can reserve equipment again.");
        }
    }
}