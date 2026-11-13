using System;
using System.Collections.Generic;

namespace ASI.Basecode.Data.Models;

public partial class User
{
    public int Id { get; set; }

    public string UserId { get; set; }

    public string Role { get; set; }

    public string FirstName { get; set; }

    public string LastName { get; set; }

    public string MiddleName { get; set; }

    public string ContactNumber { get; set; }

    public string Email { get; set; }

    public string PasswordHash { get; set; }

    public string BorrowerStatus { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedTime { get; set; }

    public DateTime UpdatedTime { get; set; }

    public string CreatedBy { get; set; }

    public string UpdatedBy { get; set; }

    public string ProfileImagePath { get; set; }
    public string AccountStatus { get; set; } = "active";

    // True for accounts an admin created: the user must set a new password and
    // fill in their details at first login before using the system
    public bool MustCompleteSetup { get; set; }

    public virtual ICollection<BorrowingTransaction> BorrowingTransactionBorrowers { get; set; } = new List<BorrowingTransaction>();

    public virtual ICollection<BorrowingTransaction> BorrowingTransactionReleasedByNavigations { get; set; } = new List<BorrowingTransaction>();

    public virtual ICollection<BorrowingTransaction> BorrowingTransactionReturnedToNavigations { get; set; } = new List<BorrowingTransaction>();

    public virtual ICollection<Reservation> ReservationBorrowers { get; set; } = new List<Reservation>();

    public virtual ICollection<Reservation> ReservationReviewedByNavigations { get; set; } = new List<Reservation>();
}
