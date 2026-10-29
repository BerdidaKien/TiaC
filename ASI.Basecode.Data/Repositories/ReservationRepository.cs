using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ASI.Basecode.Data.Interfaces;
using ASI.Basecode.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace ASI.Basecode.Data.Repositories
{
    public class ReservationRepository : IReservationRepository
    {
        private readonly TiaCContext _context;

        public ReservationRepository(TiaCContext context)
        {
            _context = context;
        }

        public IQueryable<Reservation> GetReservations()
        {
            return _context.Reservations
                           .Include(r => r.Borrower)
                           .Include(r => r.BorrowingTransaction)
                           .Include(r => r.EquipmentItem).ThenInclude(i => i.Category);
        }

        public IQueryable<BorrowingTransaction> GetTransactions()
        {
            return _context.BorrowingTransactions
                           .Include(t => t.Borrower)
                           .Include(t => t.ReleasedByNavigation)
                           .Include(t => t.ReturnedToNavigation)
                           .Include(t => t.Reservation)
                           .Include(t => t.EquipmentItem).ThenInclude(i => i.Category);
        }

        public IQueryable<EquipmentCategory> GetCategories()
        {
            return _context.EquipmentCategories;
        }

        public Reservation GetReservationById(int id)
        {
            return GetReservations().FirstOrDefault(r => r.Id == id);
        }

        public BorrowingTransaction GetTransactionById(int id)
        {
            return GetTransactions().FirstOrDefault(t => t.Id == id);
        }

        // Converts the login code (user.user_id) into the numeric primary key (user.id)
        public int GetUserDbId(string userCode)
        {
            return _context.Users
                           .Where(u => u.UserId == userCode)
                           .Select(u => u.Id)
                           .FirstOrDefault();
        }

        public void AddTransaction(BorrowingTransaction transaction)
        {
            _context.BorrowingTransactions.Add(transaction);
        }

        // One call saves every change made on tracked entities (atomic)
        public void SaveChanges()
        {
            _context.SaveChanges();
        }
    }
}