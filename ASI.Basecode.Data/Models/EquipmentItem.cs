using System;
using System.Collections.Generic;

namespace ASI.Basecode.Data.Models;

public partial class EquipmentItem
{
    public int Id { get; set; }

    public int CategoryId { get; set; }

    public string ItemCode { get; set; }

    public string BrandModel { get; set; }

    public string SerialNumber { get; set; }

    public string ConditionRating { get; set; }

    public string StorageLocation { get; set; }

    public string Status { get; set; }

    public DateTime CreatedTime { get; set; }

    public DateTime UpdatedTime { get; set; }

    public string CreatedBy { get; set; }

    public string UpdatedBy { get; set; }

    public virtual ICollection<BorrowingTransaction> BorrowingTransactions { get; set; } = new List<BorrowingTransaction>();

    public virtual EquipmentCategory Category { get; set; }

    public virtual ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
}
