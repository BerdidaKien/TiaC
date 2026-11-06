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
        // custodianCode limits the history to that custodian's equipment (null = everything)
        HistoryPageViewModel GetPage(HistoryFilter filter, string custodianCode = null);

        HistoryPageViewModel GetStudentPage(
            HistoryFilter filter,
            string borrowerUserId);

        byte[] ExportCsv(HistoryFilter filter, string custodianCode = null);
    }
}
