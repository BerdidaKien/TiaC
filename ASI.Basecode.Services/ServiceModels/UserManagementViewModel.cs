using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASI.Basecode.Services.ServiceModels
{
    public class UserManagementViewModel
    {
        public int TotalUsers { get; set; }
        public int TotalBorrowers { get; set; }
        public int TotalCustodians { get; set; }
        public int TotalAdministrators { get; set; }

        public string SearchTerm { get; set; }
        public string SelectedRole { get; set; }
        public string SelectedStatus { get; set; }

        public List<UserListItemModel> Users { get; set; } = new List<UserListItemModel>();
    }

    public class UserListItemModel
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Role { get; set; }
        public bool IsActive { get; set; }
        public DateTime DateCreated { get; set; }
        public string Initials { get; set; }
    }
    public class UserRowInput
    {
        public string FullName { get; set; }
        public string Email { get; set; }
        public string StudentEmployeeId { get; set; }
    }
}