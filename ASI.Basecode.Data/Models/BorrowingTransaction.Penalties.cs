using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASI.Basecode.Data.Models;

public partial class BorrowingTransaction
{
    public virtual ICollection<Penalty> Penalties { get; set; } = new List<Penalty>();
}
