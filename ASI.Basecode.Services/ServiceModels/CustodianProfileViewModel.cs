using System;
using System.Linq;

namespace ASI.Basecode.Services.ServiceModels
{
    public class CustodianProfileViewModel
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public string LastName { get; set; }
        public string EmployeeId { get; set; }
        public string Email { get; set; }
        public string ContactNumber { get; set; }
        public string Role { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedTime { get; set; }
        public string ProfileImagePath { get; set; }
        public int ApprovedRequestsCount { get; set; }
        public int CompletedBorrowingsCount { get; set; }

        public string FullName =>
            string.Join(" ", new[] { FirstName, MiddleName, LastName }.Where(s => !string.IsNullOrWhiteSpace(s)));
    }
}