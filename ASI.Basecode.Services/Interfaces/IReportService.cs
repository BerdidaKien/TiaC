using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ASI.Basecode.Services.ServiceModels;

namespace ASI.Basecode.Services.Interfaces
{
    public interface IReportService
    {
        ReportsPageViewModel GetPage(DateTime? from, DateTime? to);
        CalendarDataDto GetCalendar(DateTime start, DateTime end, int? categoryId, int? itemId);
    }
}