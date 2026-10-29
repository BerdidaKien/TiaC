using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ASI.Basecode.Data.Models;

namespace ASI.Basecode.Services.ServiceModels
{
    public class HistoryFilter
    {
        public string Search { get; set; }       // borrower ID, name, item code, brand
        public int? CategoryId { get; set; }
        public string Result { get; set; }       // active | ontime | overdue | damaged
        public DateTime? DateFrom { get; set; }  // filters on handover date
        public DateTime? DateTo { get; set; }
    }

    public class HistoryPageViewModel
    {
        public HistoryFilter Filter { get; set; } = new HistoryFilter();
        public List<EquipmentCategory> Categories { get; set; } = new List<EquipmentCategory>();

        public List<BorrowingTransaction> Rows { get; set; } = new List<BorrowingTransaction>();
        public List<BorrowingTransaction> PenaltyRows { get; set; } = new List<BorrowingTransaction>();

        // Summary metrics
        public int TotalTransactions { get; set; }
        public int ActiveLoans { get; set; }
        public int LateReturns { get; set; }
        public int CurrentlyOverdue { get; set; }
        public int TotalDelayHours { get; set; }
        public int DamagedReturns { get; set; }
        public int OnTimeRate { get; set; }      // percent of returned items

        public static string TxnCode(BorrowingTransaction t) => $"TXN-{t.ActualPickupTime:yyyy}-{t.Id:D4}";
    }
}
