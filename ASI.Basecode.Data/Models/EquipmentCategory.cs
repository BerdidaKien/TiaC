using System;
using System.Collections.Generic;

namespace ASI.Basecode.Data.Models;

public partial class EquipmentCategory
{
    public int Id { get; set; }

    public string CategoryCode { get; set; }

    public string CategoryName { get; set; }

    public string Description { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedTime { get; set; }

    public DateTime UpdatedTime { get; set; }

    public string CreatedBy { get; set; }

    public string UpdatedBy { get; set; }

    public virtual ICollection<EquipmentItem> EquipmentItems { get; set; } = new List<EquipmentItem>();
}
