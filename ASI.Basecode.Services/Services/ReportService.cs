using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ASI.Basecode.Data.Interfaces;
using ASI.Basecode.Services.Interfaces;
using ASI.Basecode.Services.ServiceModels;

namespace ASI.Basecode.Services.Services
{
    public class ReportService : IReportService
    {
        private readonly IReservationRepository _resRepo;
        private readonly IEquipmentRepository _equipRepo;

        public ReportService(IReservationRepository resRepo, IEquipmentRepository equipRepo)
        {
            _resRepo = resRepo;
            _equipRepo = equipRepo;
        }

        public ReportsPageViewModel GetPage(DateTime? from, DateTime? to)
        {
            var end = (to ?? DateTime.Today).Date;
            var start = (from ?? end.AddDays(-29)).Date;
            if (start > end) { var tmp = start; start = end; end = tmp; }
            var endExclusive = end.AddDays(1);
            var now = DateTime.Now;

            var items = _equipRepo.GetEquipmentItems();
            var totalItems = items.Count();

            // Borrowings that started in the selected range
            var tx = _resRepo.GetTransactions()
                             .Where(t => t.ActualPickupTime >= start && t.ActualPickupTime < endExclusive);

            var totalTx = tx.Count();
            var distinctItems = tx.Select(t => t.EquipmentItemId).Distinct().Count();

            var model = new ReportsPageViewModel
            {
                From = start,
                To = end,
                TotalItems = totalItems,
                TotalTransactions = totalTx,
                OverdueFlagged = tx.Count(t => t.IsOverdue
                                            || (t.ActualReturnTime == null && t.Reservation.EstimatedReturnDate < now)),
                TotalDelayHours = tx.Where(t => t.IsOverdue).Sum(t => (int?)t.DelayDurationHours) ?? 0,
                DamagedLogs = tx.Count(t => t.ReturnCondition == "needs_maintenance"),
                // Utilization = share of equipment items borrowed at least once in the range
                UtilizationRate = totalItems == 0 ? 0 : (int)Math.Round(distinctItems * 100.0 / totalItems),

                Categories = _resRepo.GetCategories().OrderBy(c => c.CategoryName).ToList(),
                ItemOptions = items.OrderBy(i => i.ItemCode)
                                   .Select(i => new CalendarItemOption
                                   {
                                       Id = i.Id,
                                       CategoryId = i.CategoryId,
                                       Label = i.ItemCode + " - " + i.BrandModel
                                   })
                                   .ToList()
            };

            // Top requested equipment (all requests whose pickup date is in the range)
            var topIds = _resRepo.GetReservations()
                .Where(r => r.RequestedPickupDate >= start && r.RequestedPickupDate < endExclusive)
                .GroupBy(r => r.EquipmentItemId)
                .Select(g => new { ItemId = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(5)
                .ToList();

            var ids = topIds.Select(x => x.ItemId).ToList();
            var itemMap = _equipRepo.GetEquipmentItems().Where(i => ids.Contains(i.Id)).ToList();

            foreach (var row in topIds)
            {
                var item = itemMap.FirstOrDefault(i => i.Id == row.ItemId);
                if (item == null) continue; // archived items are skipped
                model.TopItems.Add(new TopItemRow
                {
                    ItemCode = item.ItemCode,
                    BrandModel = item.BrandModel,
                    CategoryName = item.Category?.CategoryName ?? "General",
                    Requests = row.Count
                });
            }

            // Borrowings per category
            var byCategory = tx.GroupBy(t => t.EquipmentItem.Category.CategoryName)
                               .Select(g => new { Name = g.Key, Count = g.Count() })
                               .OrderByDescending(x => x.Count)
                               .ToList();

            model.CategoryUsage = byCategory.Select(x => new CategoryUsageRow
            {
                CategoryName = x.Name,
                Borrows = x.Count,
                Percent = totalTx == 0 ? 0 : (int)Math.Round(x.Count * 100.0 / totalTx)
            }).ToList();

            return model;
        }

        // 'end' is exclusive. Returns every booking that touches [start, end).
        public CalendarDataDto GetCalendar(DateTime start, DateTime end, int? categoryId, int? itemId)
        {
            var now = DateTime.Now;

            // Items that can be booked (maintenance items are left out of the "free" count)
            var items = _equipRepo.GetEquipmentItems().Where(i => i.Status != "maintenance");
            if (categoryId.HasValue) items = items.Where(i => i.CategoryId == categoryId.Value);
            if (itemId.HasValue) items = items.Where(i => i.Id == itemId.Value);

            // Requests not yet handed over (pending or approved)
            var reservations = _resRepo.GetReservations()
                .Where(r => (r.Status == "pending" || r.Status == "approved")
                         && r.BorrowingTransaction == null
                         && r.RequestedPickupDate < end
                         && r.EstimatedReturnDate >= start);

            // Handed-over loans: active ones always count, returned ones only if they ended inside the window
            var transactions = _resRepo.GetTransactions()
                .Where(t => t.ActualPickupTime < end
                         && (t.ActualReturnTime == null || t.ActualReturnTime >= start));

            if (categoryId.HasValue)
            {
                reservations = reservations.Where(r => r.EquipmentItem.CategoryId == categoryId.Value);
                transactions = transactions.Where(t => t.EquipmentItem.CategoryId == categoryId.Value);
            }
            if (itemId.HasValue)
            {
                reservations = reservations.Where(r => r.EquipmentItemId == itemId.Value);
                transactions = transactions.Where(t => t.EquipmentItemId == itemId.Value);
            }

            var result = new CalendarDataDto { TotalItems = items.Count() };

            foreach (var r in reservations.OrderBy(r => r.RequestedPickupDate).ToList())
            {
                result.Events.Add(new CalendarEventDto
                {
                    Id = "r" + r.Id,
                    ItemId = r.EquipmentItemId,
                    ItemCode = r.EquipmentItem.ItemCode,
                    Brand = r.EquipmentItem.BrandModel,
                    Category = r.EquipmentItem.Category?.CategoryName,
                    Borrower = r.Borrower.FirstName + " " + r.Borrower.LastName,
                    BorrowerCode = r.Borrower.UserId,
                    Status = r.Status,
                    Start = Iso(r.RequestedPickupDate),
                    End = Iso(r.EstimatedReturnDate)
                });
            }

            foreach (var t in transactions.OrderBy(t => t.ActualPickupTime).ToList())
            {
                var due = t.Reservation?.EstimatedReturnDate;
                string status;
                DateTime endAt;

                if (t.ActualReturnTime != null)
                {
                    status = "returned";
                    endAt = t.ActualReturnTime.Value;
                }
                else if (due.HasValue && due.Value < now)
                {
                    status = "overdue";      // still out past the due date
                    endAt = now;
                }
                else
                {
                    status = "borrowed";
                    endAt = due ?? now;
                }

                result.Events.Add(new CalendarEventDto
                {
                    Id = "t" + t.Id,
                    ItemId = t.EquipmentItemId,
                    ItemCode = t.EquipmentItem.ItemCode,
                    Brand = t.EquipmentItem.BrandModel,
                    Category = t.EquipmentItem.Category?.CategoryName,
                    Borrower = t.Borrower.FirstName + " " + t.Borrower.LastName,
                    BorrowerCode = t.Borrower.UserId,
                    Status = status,
                    Start = Iso(t.ActualPickupTime),
                    End = Iso(endAt)
                });
            }

            return result;
        }

        private static string Iso(DateTime? d)
        {
            return d.HasValue ? d.Value.ToString("yyyy-MM-ddTHH:mm") : "";
        }
    }
}
