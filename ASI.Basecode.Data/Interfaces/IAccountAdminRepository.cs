using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ASI.Basecode.Data.Models;

namespace ASI.Basecode.Data.Interfaces
{
    public interface IAccountAdminRepository
    {
        IQueryable<User> GetUsers();
        User GetUserById(int id);
        User GetUserByCode(string code);
        IQueryable<Reservation> GetReservations();
        IQueryable<BorrowingTransaction> GetTransactions();
        IQueryable<InfoChangeRequest> GetRequests();
        InfoChangeRequest GetRequestById(int id);
        InfoChangeRequestFile GetFileById(int id);
        void AddRequest(InfoChangeRequest request);
        void SaveChanges();
    }
}