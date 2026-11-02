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

        // ---------- Edit ----------
        public EquipmentItem GetEquipmentById(int id)
        {
            return _equipmentRepository.GetEquipmentById(id);
        }

        public ServiceResult UpdateEquipment(EquipmentItem edited)
        {
            var existing = _equipmentRepository.GetEquipmentById(edited.Id);
            if (existing == null || existing.Status == "archived")
                return ServiceResult.Fail("Equipment item not found.");

            if (!_equipmentRepository.GetCategories().Any(c => c.Id == edited.CategoryId))
                return ServiceResult.Fail("Please select a valid category.");

            var code = edited.ItemCode?.Trim();
            if (_equipmentRepository.GetEquipmentItems().Any(e => e.Id != existing.Id && e.ItemCode == code))
                return ServiceResult.Fail($"Another item already uses the code \"{code}\".");

            existing.ItemCode = code;
            existing.CategoryId = edited.CategoryId;
            existing.BrandModel = edited.BrandModel?.Trim();
            existing.SerialNumber = edited.SerialNumber?.Trim();
            existing.StorageLocation = edited.StorageLocation?.Trim();

            if (!string.IsNullOrEmpty(edited.ConditionRating))
                existing.ConditionRating = edited.ConditionRating;

            // Status can only be toggled between available <-> maintenance, and never while the item is out
            if (existing.Status != "borrowed" &&
                (edited.Status == "available" || edited.Status == "maintenance"))
            {
                existing.Status = edited.Status;
            }

            // Only replace the photo when a new one was uploaded
            if (!string.IsNullOrEmpty(edited.ImagePath))
                existing.ImagePath = edited.ImagePath;

            existing.UpdatedBy = string.IsNullOrEmpty(edited.UpdatedBy) ? "Custodian" : edited.UpdatedBy;
            existing.UpdatedTime = DateTime.Now;

            try
            {
                _equipmentRepository.UpdateEquipment(existing);
            }
            catch (Exception)
            {
                return ServiceResult.Fail("Could not save the changes. The item code may already be in use.");
            }

            return ServiceResult.Ok("Equipment updated successfully.");
        }

        // ---------- Archive (soft delete) ----------
        public ServiceResult ArchiveEquipment(int id, string updatedBy)
        {
            var existing = _equipmentRepository.GetEquipmentById(id);
            if (existing == null || existing.Status == "archived")
                return ServiceResult.Fail("Equipment item not found.");

            var isOut = existing.Status == "borrowed" ||
                        _equipmentRepository.GetBorrowingTransactions()
                            .Any(t => t.EquipmentItemId == id && t.ActualReturnTime == null);
            if (isOut)
                return ServiceResult.Fail("This item is currently borrowed. Archive it after it is returned.");

            var hasOpenReservation = _equipmentRepository.GetReservations()
                .Any(r => r.EquipmentItemId == id &&
                          (r.Status == "pending" ||
                           (r.Status == "approved" && r.BorrowingTransaction == null)));
            if (hasOpenReservation)
                return ServiceResult.Fail("This item has pending or approved reservations. Resolve them first.");

            existing.UpdatedBy = string.IsNullOrEmpty(updatedBy) ? "Custodian" : updatedBy;
            existing.UpdatedTime = DateTime.Now;

            try
            {
                _equipmentRepository.DeleteEquipment(existing);   // soft delete: status = "archived"
            }
            catch (Exception)
            {
                return ServiceResult.Fail("Could not archive the item.");
            }

            return ServiceResult.Ok("Equipment archived. Its borrowing history is kept.");
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