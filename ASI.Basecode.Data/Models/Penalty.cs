using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASI.Basecode.Data.Models;

public partial class Penalty
{
    public int Id { get; set; }
    public int TransactionId { get; set; }
    public int BorrowerId { get; set; }
    public string PenaltyType { get; set; }     // late | damage
    public decimal Amount { get; set; }         // pesos for late; 0 for damage (replacement)
    public string Notes { get; set; }
    public string Status { get; set; }          // unresolved | resolved | waived
    public DateTime DueBy { get; set; }
    public DateTime CreatedTime { get; set; }
    public DateTime? ResolvedTime { get; set; }
    public int? ResolvedBy { get; set; }

    public virtual BorrowingTransaction Transaction { get; set; }
    public virtual User Borrower { get; set; }
    public virtual User ResolvedByNavigation { get; set; }
}
