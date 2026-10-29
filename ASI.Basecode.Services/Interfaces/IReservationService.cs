using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ASI.Basecode.Services.ServiceModels;

namespace ASI.Basecode.Services.Interfaces
{
    public interface IReservationService
    {
        ReservationsPageViewModel GetPage(string search, int? categoryId);
        ServiceResult Approve(int reservationId, string notes, string custodianCode);
        ServiceResult Decline(int reservationId, string reason, string custodianCode);
        ServiceResult Release(int reservationId, string custodianCode);
        ServiceResult Return(int transactionId, string condition, string notes, string custodianCode);
    }
}
