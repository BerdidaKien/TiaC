using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ASI.Basecode.Services.ServiceModels;

namespace ASI.Basecode.Services.Interfaces
{
    public interface ICategoryService
    {
        CategoriesPageViewModel GetPage();
        int PendingCount();
        ServiceResult Create(string name, string description, string adminCode);
        ServiceResult Update(int id, string name, string description, string adminCode);
        ServiceResult SetActive(int id, bool active, string adminCode);
        ServiceResult Delete(int id);
        ServiceResult ApproveRequest(int itemId, string categoryName, string adminCode);
        ServiceResult RejectRequest(int itemId, string reason, string adminCode);
    }
}
