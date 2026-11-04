using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASI.Basecode.Services.ServiceModels
{
    public static class ItemStatuses
    {
        // Items in these states have no approved category yet:
        // hidden from students and left out of counts
        public static readonly string[] AwaitingCategory = { "pending_category", "category_rejected" };
    }

    public class CategoryRow
    {
        public int Id { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public bool IsActive { get; set; }
        public int ItemCount { get; set; }     // items not archived
        public int TotalItems { get; set; }    // including archived (decides if it can be deleted)
    }

    public class PendingCategoryRequest
    {
        public int ItemId { get; set; }
        public string ItemCode { get; set; }
        public string ItemName { get; set; }
        public string SerialNumber { get; set; }
        public string StorageLocation { get; set; }
        public string ImagePath { get; set; }
        public string Suggested { get; set; }
        public string RequestedBy { get; set; }
        public DateTime RequestedAt { get; set; }
    }

    public class CategoriesPageViewModel
    {
        public List<CategoryRow> Categories { get; set; } = new List<CategoryRow>();
        public List<PendingCategoryRequest> Pending { get; set; } = new List<PendingCategoryRequest>();
    }
}
