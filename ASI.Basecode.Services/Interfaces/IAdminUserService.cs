using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ASI.Basecode.Data.Interfaces;
using ASI.Basecode.Data.Models;
using ASI.Basecode.Services.ServiceModels;

namespace ASI.Basecode.Services.Interfaces
{
    public interface IAdminUserService
    {
        AddUsersResult CreateUsers(AddUsersPostModel model, string adminCode);
    }
}