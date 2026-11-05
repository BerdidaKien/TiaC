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
    public class AccountAdminRepository : IAccountAdminRepository
    {
        private readonly TiaCContext _context;

        public AccountAdminRepository(TiaCContext context)
        {
            _context = context;
        }

        public IQueryable<User> GetUsers() => _context.Users;

        public User GetUserById(int id) => _context.Users.FirstOrDefault(u => u.Id == id);

        public User GetUserByCode(string code) => _context.Users.FirstOrDefault(u => u.UserId == code);

        public IQueryable<Reservation> GetReservations() =>
            _context.Reservations.Include(r => r.BorrowingTransaction);

        public IQueryable<BorrowingTransaction> GetTransactions() => _context.BorrowingTransactions;

        public IQueryable<InfoChangeRequest> GetRequests() =>
            _context.InfoChangeRequests
                    .Include(r => r.User)
                    .Include(r => r.ReviewedByNavigation)
                    .Include(r => r.Items)
                    .Include(r => r.Files);

        public InfoChangeRequest GetRequestById(int id) => GetRequests().FirstOrDefault(r => r.Id == id);

        public InfoChangeRequestFile GetFileById(int id) =>
            _context.InfoChangeRequestFiles.FirstOrDefault(f => f.Id == id);

        public void AddRequest(InfoChangeRequest request) => _context.InfoChangeRequests.Add(request);

        public void SaveChanges() => _context.SaveChanges();
    }
}