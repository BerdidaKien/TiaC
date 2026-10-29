using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ASI.Basecode.Data.Models;

namespace ASI.Basecode.Services.ServiceModels
{
    public class ReservationsPageViewModel
    {
        public List<Reservation> Pending { get; set; } = new List<Reservation>();
        public List<Reservation> Approved { get; set; } = new List<Reservation>();
        public List<BorrowingTransaction> Active { get; set; } = new List<BorrowingTransaction>();
        public List<EquipmentCategory> Categories { get; set; } = new List<EquipmentCategory>();

        public string Search { get; set; }
        public int? CategoryId { get; set; }
    }
}