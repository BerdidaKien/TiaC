using System;
using System.Collections.Generic;

namespace ASI.Basecode.Data.Models;

public partial class Reservation
{
    public int Id { get; set; }

    public int BorrowerId { get; set; }

    public int EquipmentItemId { get; set; }

    public DateTime RequestedPickupDate { get; set; }

    public DateTime EstimatedReturnDate { get; set; }

    public string Status { get; set; }

    public int? ReviewedBy { get; set; }

    public string ReviewNotes { get; set; }

    public DateTime? ReviewedAt { get; set; }

    public DateTime CreatedTime { get; set; }

    public DateTime UpdatedTime { get; set; }

    public virtual User Borrower { get; set; }

    public virtual BorrowingTransaction BorrowingTransaction { get; set; }

    public virtual EquipmentItem EquipmentItem { get; set; }

    public virtual User ReviewedByNavigation { get; set; }
}
