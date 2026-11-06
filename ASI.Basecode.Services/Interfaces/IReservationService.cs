using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ASI.Basecode.Data.Models;
using ASI.Basecode.Services.ServiceModels;

namespace ASI.Basecode.Services.Interfaces
{
    public interface IReservationService
    {
        ReservationsPageViewModel GetPage(string search, int? categoryId, string custodianCode = null);
        ServiceResult Approve(int reservationId, string notes, string custodianCode);
        ServiceResult Decline(int reservationId, string reason, string custodianCode);
        ServiceResult Release(int reservationId, string custodianCode);
        ServiceResult Return(int transactionId, string condition, string notes, string custodianCode);

        // ---- Student side ----
        List<Reservation> GetForUser(string userCode);
        int CountActiveForUser(string userCode);
        int CountPendingForUser(string userCode);

        // Student asks to borrow an item; the custodian approves or declines it
        ServiceResult CreateReservation(string borrowerCode, int equipmentItemId, DateTime pickup, DateTime returnDate);

        // Student cancels their own request (pending, or approved but not yet picked up)
        ServiceResult CancelReservation(int reservationId, string borrowerCode);
    }
}
