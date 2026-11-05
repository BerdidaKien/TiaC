using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ASI.Basecode.Data.Models;
using ASI.Basecode.Services.ServiceModels;

namespace ASI.Basecode.Services.Services
{
    internal static class InfoRequestMapper
    {
        public static string FullName(User u)
        {
            if (u == null) return "-";
            return string.Join(" ", new[] { u.FirstName, u.MiddleName, u.LastName }
                .Where(s => !string.IsNullOrWhiteSpace(s)));
        }

        public static InfoRequestRow ToRow(InfoChangeRequest r)
        {
            return new InfoRequestRow
            {
                Id = r.Id,
                Status = r.Status,
                Reason = r.Reason,
                ReviewNote = r.ReviewNote,
                ReviewedBy = r.ReviewedByNavigation == null ? null : FullName(r.ReviewedByNavigation),
                ReviewedAt = r.ReviewedAt,
                CreatedTime = r.CreatedTime,

                UserDbId = r.UserId,
                UserCode = r.User?.UserId,
                UserName = FullName(r.User),
                UserRole = r.User?.Role,
                UserEmail = r.User?.Email,

                Items = InfoFields.All
                    .Select(f => r.Items.FirstOrDefault(i => i.FieldKey == f.Key))
                    .Where(i => i != null)
                    .Select(i => new InfoRequestItemRow
                    {
                        Key = i.FieldKey,
                        Label = InfoFields.Label(i.FieldKey),
                        OldValue = i.OldValue,
                        NewValue = i.NewValue,
                        ApprovedValue = i.ApprovedValue,
                        MaxLength = InfoFields.Max(i.FieldKey),
                        Required = i.FieldKey != "middle_name"
                    })
                    .ToList(),

                Files = r.Files
                    .OrderBy(f => f.Id)
                    .Select(f => new InfoRequestFileRow { Id = f.Id, Name = f.OriginalName, Size = f.SizeBytes })
                    .ToList()
            };
        }
    }
}