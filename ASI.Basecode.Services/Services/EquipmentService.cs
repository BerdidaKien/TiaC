using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ASI.Basecode.Data.Interfaces;
using ASI.Basecode.Data.Models;
using ASI.Basecode.Data.Repositories;
using ASI.Basecode.Services.Interfaces;
using ASI.Basecode.Services.ServiceModels;

namespace ASI.Basecode.Services.Services
{
    public class EquipmentService : IEquipmentService
    {
        private readonly IEquipmentRepository _equipmentRepository;

        public EquipmentService(IEquipmentRepository equipmentRepository)
        {
            _equipmentRepository = equipmentRepository;
        }

        public IEnumerable<EquipmentItem> GetAllEquipment()
        {
            return _equipmentRepository.GetEquipmentItems().ToList();
        }

        public IEnumerable<EquipmentCategory> GetCategories()
        {
            return _equipmentRepository.GetCategories().ToList();
        }

        public void AddEquipment(EquipmentItem item)
        {
            item.CreatedTime = DateTime.Now;
            item.UpdatedTime = DateTime.Now;

            // CRITICAL: Force EF Core to ONLY use CategoryId foreign key
            // and stop trying to insert/modify a navigation property object
            item.Category = null;

            if (string.IsNullOrEmpty(item.Status))
            {
                item.Status = "available";
            }
            if (string.IsNullOrEmpty(item.ConditionRating))
            {
                item.ConditionRating = "good";
            }
            if (string.IsNullOrEmpty(item.CreatedBy))
            {
                item.CreatedBy = "Custodian";
            }
            if (string.IsNullOrEmpty(item.UpdatedBy))
            {
                item.UpdatedBy = "Custodian";
            }

            _equipmentRepository.AddEquipment(item);
        }
        public DashboardViewModel GetDashboardSummary(string search, int? categoryId, string status)
        {
            var items = _equipmentRepository.GetEquipmentItems(); // non-archived, includes Category

            // IDs of items that currently have an open (unreturned) transaction
            var borrowedIds = _equipmentRepository.GetBorrowingTransactions()
                .Where(t => t.ActualReturnTime == null)
                .Select(t => t.EquipmentItemId)
                .Distinct()
                .ToList();

            var model = new DashboardViewModel
            {
                TotalEquipment = items.Count(),
                CurrentlyBorrowed = items.Count(i => borrowedIds.Contains(i.Id)),
                AvailableItems = items.Count(i => i.Status == "available" && !borrowedIds.Contains(i.Id)),
                PendingRequests = _equipmentRepository.GetReservations().Count(r => r.Status == "pending"),
                BorrowedItemIds = borrowedIds,
                Categories = _equipmentRepository.GetCategories().OrderBy(c => c.CategoryName).ToList(),
                Search = search,
                CategoryId = categoryId,
                Status = status
            };

            var filtered = items;

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                filtered = filtered.Where(i => i.ItemCode.Contains(term)
                                            || i.BrandModel.Contains(term)
                                            || i.SerialNumber.Contains(term));
            }

            if (categoryId.HasValue)
            {
                filtered = filtered.Where(i => i.CategoryId == categoryId.Value);
            }

            if (status == "available")
            {
                filtered = filtered.Where(i => i.Status == "available" && !borrowedIds.Contains(i.Id));
            }
            else if (status == "borrowed")
            {
                filtered = filtered.Where(i => borrowedIds.Contains(i.Id));
            }

            model.Items = filtered.OrderBy(i => i.ItemCode).ToList();
            return model;
        }
    }
}