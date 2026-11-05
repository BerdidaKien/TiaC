using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASI.Basecode.Data.Models;

public partial class InfoChangeRequest
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public string Reason { get; set; }

    public string Status { get; set; }

    public string ReviewNote { get; set; }

    public int? ReviewedBy { get; set; }

    public DateTime? ReviewedAt { get; set; }

    public DateTime CreatedTime { get; set; }

    public DateTime UpdatedTime { get; set; }

    public virtual User User { get; set; }

    public virtual User ReviewedByNavigation { get; set; }

    public virtual ICollection<InfoChangeRequestItem> Items { get; set; } = new List<InfoChangeRequestItem>();

    public virtual ICollection<InfoChangeRequestFile> Files { get; set; } = new List<InfoChangeRequestFile>();
}

public partial class InfoChangeRequestItem
{
    public int Id { get; set; }

    public int RequestId { get; set; }

    public string FieldKey { get; set; }

    public string OldValue { get; set; }

    public string NewValue { get; set; }

    public string ApprovedValue { get; set; }

    public virtual InfoChangeRequest Request { get; set; }
}

public partial class InfoChangeRequestFile
{
    public int Id { get; set; }

    public int RequestId { get; set; }

    public string StoredName { get; set; }

    public string OriginalName { get; set; }

    public string ContentType { get; set; }

    public long SizeBytes { get; set; }

    public DateTime UploadedTime { get; set; }

    public virtual InfoChangeRequest Request { get; set; }
}
