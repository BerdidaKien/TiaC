using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASI.Basecode.Services.ServiceModels
{
    // One line per student in Custodian History
    public class StudentChargeRow
    {
        public int BorrowerId { get; set; }
        public string UserCode { get; set; }
        public string Name { get; set; }
        public decimal LateTotal { get; set; }
        public int LateCount { get; set; }
        public int ReplacementCount { get; set; }
        public DateTime EarliestDeadline { get; set; }
        public bool PastDeadline { get; set; }
        public bool Restricted { get; set; }
    }

    // Shown to the admin on the Edit User page
    public class BorrowerPenaltySummary
    {
        public bool Restricted { get; set; }
        public int UnresolvedCount { get; set; }
        public decimal LateTotal { get; set; }
        public int ReplacementCount { get; set; }
        public int UnreturnedOverdue { get; set; }

        public bool CanLift => Restricted && UnresolvedCount == 0 && UnreturnedOverdue == 0;
    }
}