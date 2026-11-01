using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ASI.Basecode.Data.Models;

namespace ASI.Basecode.Data.Interfaces
{
    public interface IUserAdminRepository
    {
        IQueryable<User> GetUsers();
        void AddUsers(IEnumerable<User> users);
        void SaveChanges();
    }
}