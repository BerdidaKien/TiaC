using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ASI.Basecode.Services.ServiceModels;

namespace ASI.Basecode.Services.Interfaces
{
    public interface IItemReportService
    {
        ItemReportsPageViewModel GetPage(ItemReportFilter filter);
        ItemReportsPageViewModel GetAll(ItemReportFilter filter);   // no paging (export and print)
        ItemDetailsDto GetItemDetails(int id);
        byte[] ExportCsv(ItemReportFilter filter);
    }
}