using System;
using System.Collections.Generic;
using System.Linq;
using ASI.Basecode.Data.Interfaces;
using ASI.Basecode.Data.Models;
using ASI.Basecode.Services.Interfaces;
using ASI.Basecode.Services.ServiceModels;

namespace ASI.Basecode.Services.Services
{
    public class EquipmentService : IEquipmentService
    {
        private static readonly string[] BlankWords = { "n/a", "na", "none", "-", "--" };
        private static readonly string[] Conditions = { "excellent", "good", "minor_wear", "needs_maintenance" };

        private readonly IEquipmentRepository _equipmentRepository;

        public EquipmentService(IEquipmentRepository equipmentRepository)
        {
            _equipmentRepository = equipmentRepository;
        }

        // null code = no filter (students, admin). An unknown code resolves to 0 and matches nothing.
        private int? OwnerId(string custodianCode) =>
            string.IsNullOrEmpty(custodianCode) ? (int?)null : _equipmentRepository.GetUserDbId(custodianCode);

        public IEnumerable<EquipmentItem> GetAllEquipment(string custodianCode = null)
        {
            var q = _equipmentRepository.GetEquipmentItems();
            var owner = OwnerId(custodianCode);
            if (owner.HasValue) q = q.Where(e => e.CustodianId == owner.Value);
            return q.ToList();
        }

        public IEnumerable<EquipmentCategory> GetCategories()
        {
            return _equipmentRepository.GetCategories().ToList();
        }

        // Blank, "N/A", "none" and "-" all become empty (NULL), so they never clash with the unique serial index
        private static string Optional(string value)
        {
            var v = value?.Trim();
            if (string.IsNullOrEmpty(v) || BlankWords.Contains(v, StringComparer.OrdinalIgnoreCase)) return null;
            return v;
        }

        private static bool BadSuggestion(string suggestion) =>
            suggestion == null || suggestion.Length < 2 || suggestion.Length > 100;

        // ---------- Add ----------
        public ServiceResult AddEquipment(EquipmentItem item, string custodianCode = null)
        {
            item.Id = 0;

            // The owner always comes from the logged-in custodian, never from the posted form
            var owner = OwnerId(custodianCode);
            if (owner == 0) return ServiceResult.Fail("Your session expired. Please log in again.");
            item.CustodianId = owner;
            item.ItemCode = item.ItemCode?.Trim();
            item.BrandModel = item.BrandModel?.Trim();

            if (string.IsNullOrEmpty(item.ItemCode) || string.IsNullOrEmpty(item.BrandModel))
                return ServiceResult.Fail("Item code and brand & model are required.");

            item.SerialNumber = Optional(item.SerialNumber);
            item.StorageLocation = Optional(item.StorageLocation);

            if (_equipmentRepository.EquipmentExists(item.ItemCode))
                return ServiceResult.Fail($"The item code \"{item.ItemCode}\" is already in use.");

            if (item.SerialNumber != null &&
                _equipmentRepository.GetEquipmentItems().Any(e => e.SerialNumber == item.SerialNumber))
                return ServiceResult.Fail($"The serial number \"{item.SerialNumber}\" is already in use.");

            // Category: a real one, or "Other" (no category id) which waits for the admin
            if (item.CategoryId.HasValue)
            {
                var cat = _equipmentRepository.GetCategories().FirstOrDefault(c => c.Id == item.CategoryId.Value);
                if (cat == null || !cat.IsActive)
                    return ServiceResult.Fail("Please select a valid category.");

                item.Status = "available";
                item.SuggestedCategory = null;
            }
            else
            {
                var suggestion = Optional(item.SuggestedCategory);
                if (BadSuggestion(suggestion))
                    return ServiceResult.Fail("Please type the category this item needs (2 to 100 characters).");

                item.Status = "pending_category";
                item.SuggestedCategory = suggestion;
            }

            item.CategoryReviewNote = null;
            if (!Conditions.Contains(item.ConditionRating)) item.ConditionRating = "good";

            item.Category = null;   // only the foreign key is used
            item.CreatedTime = DateTime.Now;
            item.UpdatedTime = DateTime.Now;
            if (string.IsNullOrEmpty(item.CreatedBy)) item.CreatedBy = "Custodian";
            if (string.IsNullOrEmpty(item.UpdatedBy)) item.UpdatedBy = "Custodian";

            try
            {
                _equipmentRepository.AddEquipment(item);
            }
            catch (Exception)
            {
                return ServiceResult.Fail("Could not save the item. The item code or serial number may already be in use.");
            }

            return ServiceResult.Ok(item.Status == "pending_category"
                ? "Item saved and sent to the admin for category review. It will be available to students once the category is approved."
                : "Equipment asset registered successfully.");
        }

        // ---------- Edit ----------
        public EquipmentItem GetEquipmentById(int id)
        {
            return _equipmentRepository.GetEquipmentById(id);
        }

        public ServiceResult UpdateEquipment(EquipmentItem edited, string custodianCode = null)
        {
            var existing = _equipmentRepository.GetEquipmentById(edited.Id);
            if (existing == null || existing.Status == "archived")
                return ServiceResult.Fail("Equipment item not found.");

            var owner = OwnerId(custodianCode);
            if (owner.HasValue && existing.CustodianId != owner)
                return ServiceResult.Fail("Equipment item not found.");

            var code = edited.ItemCode?.Trim();
            var brand = edited.BrandModel?.Trim();
            if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(brand))
                return ServiceResult.Fail("Item code and brand & model are required.");

            if (_equipmentRepository.GetEquipmentItems().Any(e => e.Id != existing.Id && e.ItemCode == code))
                return ServiceResult.Fail($"Another item already uses the code \"{code}\".");

            var serial = Optional(edited.SerialNumber);
            if (serial != null &&
                _equipmentRepository.GetEquipmentItems().Any(e => e.Id != existing.Id && e.SerialNumber == serial))
                return ServiceResult.Fail($"Another item already uses the serial number \"{serial}\".");

            var wasAwaiting = ItemStatuses.AwaitingCategory.Contains(existing.Status);

            if (edited.CategoryId.HasValue)
            {
                var cat = _equipmentRepository.GetCategories().FirstOrDefault(c => c.Id == edited.CategoryId.Value);
                // a disabled category is only accepted if the item already has it
                if (cat == null || (!cat.IsActive && existing.CategoryId != cat.Id))
                    return ServiceResult.Fail("Please select a valid category.");

                existing.CategoryId = cat.Id;
                if (wasAwaiting)
                {
                    existing.Status = "available";
                    existing.SuggestedCategory = null;
                    existing.CategoryReviewNote = null;
                }
            }
            else
            {
                // "Other" is only for items that don't have a category yet
                if (!wasAwaiting)
                    return ServiceResult.Fail("This item already has a category. Please pick one from the list.");

                var suggestion = Optional(edited.SuggestedCategory);
                if (BadSuggestion(suggestion))
                    return ServiceResult.Fail("Please type the category this item needs (2 to 100 characters).");

                existing.SuggestedCategory = suggestion;
                existing.CategoryReviewNote = null;
                existing.Status = "pending_category";
            }

            existing.ItemCode = code;
            existing.BrandModel = brand;
            existing.SerialNumber = serial;
            existing.StorageLocation = Optional(edited.StorageLocation);

            if (Conditions.Contains(edited.ConditionRating))
                existing.ConditionRating = edited.ConditionRating;

            // Availability toggles between available <-> maintenance, never while out on loan
            // and never for items still waiting for a category
            if (!wasAwaiting && existing.Status != "borrowed" &&
                (edited.Status == "available" || edited.Status == "maintenance"))
            {
                existing.Status = edited.Status;
            }

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
                return ServiceResult.Fail("Could not save the changes. The item code or serial number may already be in use.");
            }

            return ServiceResult.Ok(existing.Status == "pending_category"
                ? "Changes saved. The item is waiting for the admin to review its category."
                : "Equipment updated successfully.");
        }

        // ---------- Archive (soft delete) ----------
        public ServiceResult ArchiveEquipment(int id, string updatedBy, string custodianCode = null)
        {
            var existing = _equipmentRepository.GetEquipmentById(id);
            if (existing == null || existing.Status == "archived")
                return ServiceResult.Fail("Equipment item not found.");

            var owner = OwnerId(custodianCode);
            if (owner.HasValue && existing.CustodianId != owner)
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

        // ---------- Custodian dashboard ----------
        public DashboardViewModel GetDashboardSummary(string search, int? categoryId, string status, string custodianCode = null)
        {
            // Items still waiting for a category are not part of the inventory yet
            var owner = OwnerId(custodianCode);

            var items = _equipmentRepository.GetEquipmentItems()   // non-archived, includes Category
                .Where(i => !ItemStatuses.AwaitingCategory.Contains(i.Status));
            var openLoans = _equipmentRepository.GetBorrowingTransactions()
                .Where(t => t.ActualReturnTime == null);
            var pending = _equipmentRepository.GetReservations()
                .Where(r => r.Status == "pending");

            if (owner.HasValue)
            {
                items = items.Where(i => i.CustodianId == owner.Value);
                openLoans = openLoans.Where(t => t.EquipmentItem.CustodianId == owner.Value);
                pending = pending.Where(r => r.EquipmentItem.CustodianId == owner.Value);
            }

            var borrowedIds = openLoans
                .Select(t => t.EquipmentItemId)
                .Distinct()
                .ToList();

            var model = new DashboardViewModel
            {
                TotalEquipment = items.Count(),
                CurrentlyBorrowed = items.Count(i => borrowedIds.Contains(i.Id)),
                AvailableItems = items.Count(i => i.Status == "available" && !borrowedIds.Contains(i.Id)),
                PendingRequests = pending.Count(),
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