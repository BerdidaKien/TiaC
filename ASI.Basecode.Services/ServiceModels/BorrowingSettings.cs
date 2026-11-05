using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace ASI.Basecode.Services.ServiceModels
{
    // The custodian's borrowing rules. The property values below are the defaults used until someone saves.
    // The student reservation form can read these through ISettingsService.Get().
    public class BorrowingSettings
    {
        public int MaxActiveItems { get; set; } = 2;
        public int DefaultDurationHours { get; set; } = 8;
        public int MaxDurationDays { get; set; } = 7;
        public int AdvanceBookingDays { get; set; } = 14;
        public int OverdueGraceMinutes { get; set; } = 30;
        public int DueSoonHours { get; set; } = 2;
        public string OpeningTime { get; set; } = "08:00";
        public string ClosingTime { get; set; } = "17:00";
    }
}
