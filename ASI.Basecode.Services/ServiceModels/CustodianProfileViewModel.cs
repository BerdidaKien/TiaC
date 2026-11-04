using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASI.Basecode.Services.ServiceModels
{
    public class CustodianProfileViewModel
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string FullName => $"{FirstName} {LastName}".Trim();

        public string EmployeeId { get; set; } // Matches user_id column
        public string Email { get; set; }
        public string ContactNumber { get; set; }
        public string Role { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedTime { get; set; }

        // Live Metric Counters
        public int ApprovedRequestsCount { get; set; }
        public int CompletedBorrowingsCount { get; set; }
    }
}