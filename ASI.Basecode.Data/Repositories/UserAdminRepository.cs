using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ASI.Basecode.Data.Interfaces;
using ASI.Basecode.Data.Models;

namespace ASI.Basecode.Data.Repositories
{
    public class UserAdminRepository : IUserAdminRepository
    {
        private readonly TiaCContext _context;

        public UserAdminRepository(TiaCContext context)
        {
            _context = context;
        }

        public IQueryable<User> GetUsers()
        {
            return _context.Users;
        }

        public void AddUsers(IEnumerable<User> users)
        {
            _context.Users.AddRange(users);
        }

        public void SaveChanges()
        {
            _context.SaveChanges();
        }
    }
}