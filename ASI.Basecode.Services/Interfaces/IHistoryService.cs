using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ASI.Basecode.Services.ServiceModels;

namespace ASI.Basecode.Services.Interfaces
{
    public interface IHistoryService
    {
        HistoryPageViewModel GetPage(HistoryFilter filter);

        HistoryPageViewModel GetStudentPage(
            HistoryFilter filter,
            string borrowerUserId);

        byte[] ExportCsv(HistoryFilter filter);
    }
}