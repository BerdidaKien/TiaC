using System;
using System.Collections.Generic;

namespace ASI.Basecode.Data.Models;

public partial class BorrowingTransaction
{
    public int Id { get; set; }

    public int? ReservationId { get; set; }

    public int EquipmentItemId { get; set; }

    public int BorrowerId { get; set; }

    public int ReleasedBy { get; set; }

    public DateTime ActualPickupTime { get; set; }

    public int? ReturnedTo { get; set; }

    public DateTime? ActualReturnTime { get; set; }

    public string ReturnCondition { get; set; }

    public string DamageNotes { get; set; }

    public bool IsOverdue { get; set; }

    public int DelayDurationHours { get; set; }

    public DateTime CreatedTime { get; set; }

    public DateTime UpdatedTime { get; set; }

    public virtual User Borrower { get; set; }

    public virtual EquipmentItem EquipmentItem { get; set; }

    public virtual User ReleasedByNavigation { get; set; }

    public virtual Reservation Reservation { get; set; }

    public virtual User ReturnedToNavigation { get; set; }
}
