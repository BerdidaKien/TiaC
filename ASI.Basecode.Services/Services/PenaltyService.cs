using System;
using System.Linq;
using ASI.Basecode.Data.Interfaces;
using ASI.Basecode.Services.Interfaces;
using ASI.Basecode.Services.ServiceModels;

namespace ASI.Basecode.Services.Services
{
    public class PenaltyService : IPenaltyService
    {
        private readonly IReservationRepository _reservationRepository;

        public PenaltyService(IReservationRepository reservationRepository)
        {
            _reservationRepository = reservationRepository;
        }

        public ServiceResult Resolve(int penaltyId, string custodianCode)
        {
            var custodianDbId = _reservationRepository.GetUserDbId(custodianCode);
            if (custodianDbId == 0) return ServiceResult.Fail("Your session expired. Please log in again.");

            var penalty = _reservationRepository.GetPenalties().FirstOrDefault(x => x.Id == penaltyId);
            if (penalty == null) return ServiceResult.Fail("Penalty not found.");
            if (penalty.Transaction?.EquipmentItem?.CustodianId != custodianDbId)
                return ServiceResult.Fail("This penalty belongs to another custodian's equipment.");
            if (penalty.Status != PenaltyRules.Unresolved)
                return ServiceResult.Fail("This penalty is already settled.");

            penalty.Status = PenaltyRules.Resolved;
            penalty.ResolvedTime = DateTime.Now;
            penalty.ResolvedBy = custodianDbId;
            _reservationRepository.SaveChanges();

            return ServiceResult.Ok(penalty.PenaltyType == PenaltyRules.Late
                ? "Late fee marked as paid."
                : "Replacement confirmed.");
        }

        public BorrowerPenaltySummary GetSummary(int userDbId)
        {
            var user = _reservationRepository.GetUserByDbId(userDbId);

            var open = _reservationRepository.GetPenalties()
                .Where(p => p.BorrowerId == userDbId && p.Status == PenaltyRules.Unresolved)
                .ToList();

            var cutoff = DateTime.Now.AddDays(-PenaltyRules.SettleLateWithinDays);
            var unreturned = _reservationRepository.GetTransactions().Count(t =>
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
            var user = _reservationRepository.GetUserByDbId(userDbId);
            if (user == null) return ServiceResult.Fail("User not found.");
            if (user.BorrowerStatus != PenaltyRules.Restricted)
                return ServiceResult.Fail("This account is not restricted.");

            var summary = GetSummary(userDbId);
            if (summary.UnresolvedCount > 0)
                return ServiceResult.Fail($"This student still has {summary.UnresolvedCount} unresolved penalty(ies). The custodian must mark them paid or replaced first.");
            if (summary.UnreturnedOverdue > 0)
                return ServiceResult.Fail("This student still has equipment out more than 3 days past its due date.");

            user.BorrowerStatus = PenaltyRules.Eligible;
            user.UpdatedTime = DateTime.Now;
            user.UpdatedBy = adminCode;
            _reservationRepository.SaveChanges();

            return ServiceResult.Ok("Borrowing restored. The student can reserve equipment again.");
        }
    }
}