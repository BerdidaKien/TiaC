using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ASI.Basecode.Data.Models;

namespace ASI.Basecode.Data.Interfaces
{
    public interface IReservationRepository
    {
        IQueryable<Reservation> GetReservations();
        IQueryable<BorrowingTransaction> GetTransactions();
        IQueryable<EquipmentCategory> GetCategories();
        Reservation GetReservationById(int id);
        BorrowingTransaction GetTransactionById(int id);
        int GetUserDbId(string userCode);
        void AddTransaction(BorrowingTransaction transaction);
        void SaveChanges();
    }
}