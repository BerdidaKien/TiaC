using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ASI.Basecode.Services.ServiceModels;

namespace ASI.Basecode.Services.Interfaces
{
    public interface IUserStatusService
    {
        UserEditViewModel GetEditPage(int id, string adminCode);
        ServiceResult ChangeStatus(int id, string newStatus, string adminCode);
    }
}