using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using ASI.Basecode.Data.Interfaces;
using ASI.Basecode.Data.Models;
using ASI.Basecode.Services.Interfaces;
using ASI.Basecode.Services.ServiceModels;

namespace ASI.Basecode.Services.Services
{
    public class CategoryService : ICategoryService
    {
        private readonly ICategoryRepository _repo;

        public CategoryService(ICategoryRepository repo)
        {
            _repo = repo;
        }

        public CategoriesPageViewModel GetPage()
        {
            var categories = _repo.GetCategories()
                .OrderBy(c => c.CategoryName)
                .Select(c => new CategoryRow
                {
                    Id = c.Id,
                    Code = c.CategoryCode,
                    Name = c.CategoryName,
                    Description = c.Description,
                    IsActive = c.IsActive,
                    ItemCount = c.EquipmentItems.Count(i => i.Status != "archived"),
                    TotalItems = c.EquipmentItems.Count()
                })
                .ToList();

            var pending = _repo.GetItems()
                .Where(i => i.Status == "pending_category")
                .OrderBy(i => i.CreatedTime)
                .Select(i => new PendingCategoryRequest
                {
                    ItemId = i.Id,
                    ItemCode = i.ItemCode,
                    ItemName = i.BrandModel,
                    SerialNumber = i.SerialNumber,
                    StorageLocation = i.StorageLocation,
                    ImagePath = i.ImagePath,
                    Suggested = i.SuggestedCategory,
                    RequestedBy = i.CreatedBy,
                    RequestedAt = i.CreatedTime
                })
                .ToList();

            return new CategoriesPageViewModel { Categories = categories, Pending = pending };
        }

        public int PendingCount() => _repo.GetItems().Count(i => i.Status == "pending_category");

        public ServiceResult Create(string name, string description, string adminCode)
        {
            if (!TryName(name, out var clean, out var error)) return ServiceResult.Fail(error);
            if (_repo.GetCategories().Any(c => c.CategoryName == clean))
                return ServiceResult.Fail($"A category named \"{clean}\" already exists.");

            _repo.AddCategory(NewCategory(clean, description, adminCode));
            return Save($"Category \"{clean}\" created.");
        }

        public ServiceResult Update(int id, string name, string description, string adminCode)
        {
            var cat = _repo.GetCategoryById(id);
            if (cat == null) return ServiceResult.Fail("Category not found.");

            if (!TryName(name, out var clean, out var error)) return ServiceResult.Fail(error);
            if (_repo.GetCategories().Any(c => c.Id != id && c.CategoryName == clean))
                return ServiceResult.Fail($"Another category is already named \"{clean}\".");

            cat.CategoryName = clean;
            cat.Description = CleanDescription(description);
            cat.UpdatedBy = adminCode;
            cat.UpdatedTime = DateTime.Now;
            return Save("Category updated.");
        }

        public ServiceResult SetActive(int id, bool active, string adminCode)
        {
            var cat = _repo.GetCategoryById(id);
            if (cat == null) return ServiceResult.Fail("Category not found.");

            cat.IsActive = active;
            cat.UpdatedBy = adminCode;
            cat.UpdatedTime = DateTime.Now;
            return Save(active
                ? $"Category \"{cat.CategoryName}\" enabled."
                : $"Category \"{cat.CategoryName}\" disabled. Existing items keep it, but it can't be chosen for new items.");
        }

        public ServiceResult Delete(int id)
        {
            var cat = _repo.GetCategoryById(id);
            if (cat == null) return ServiceResult.Fail("Category not found.");

            if (_repo.GetItems().Any(i => i.CategoryId == id))
                return ServiceResult.Fail("This category has items (or archived items), so it can't be deleted. Disable it instead.");

            _repo.DeleteCategory(cat);
            return Save($"Category \"{cat.CategoryName}\" deleted.");
        }

        public ServiceResult ApproveRequest(int itemId, string categoryName, string adminCode)
        {
            var item = _repo.GetItemById(itemId);
            if (item == null || item.Status != "pending_category")
                return ServiceResult.Fail("This request was already handled.");

            if (!TryName(categoryName, out var clean, out var error)) return ServiceResult.Fail(error);

            string message;
            var existing = _repo.GetCategories().FirstOrDefault(c => c.CategoryName == clean);
            if (existing != null)
            {
                if (!existing.IsActive)
                    return ServiceResult.Fail($"The category \"{existing.CategoryName}\" exists but is disabled. Enable it first, or use a different name.");

                item.CategoryId = existing.Id;
                message = $"Item added to the existing category \"{existing.CategoryName}\" and is now available.";
            }
            else
            {
                var created = NewCategory(clean, null, adminCode);
                _repo.AddCategory(created);
                item.Category = created;
                message = $"Category \"{clean}\" created and the item is now available.";
            }

            item.Status = "available";
            item.SuggestedCategory = null;
            item.CategoryReviewNote = null;
            item.UpdatedBy = adminCode;
            item.UpdatedTime = DateTime.Now;
            return Save(message);
        }

        public ServiceResult RejectRequest(int itemId, string reason, string adminCode)
        {
            var item = _repo.GetItemById(itemId);
            if (item == null || item.Status != "pending_category")
                return ServiceResult.Fail("This request was already handled.");

            var note = (reason ?? "").Trim();
            if (note.Length < 3 || note.Length > 255)
                return ServiceResult.Fail("Please give a reason (3 to 255 characters).");

            item.Status = "category_rejected";
            item.CategoryReviewNote = note;
            item.UpdatedBy = adminCode;
            item.UpdatedTime = DateTime.Now;
            return Save("Request rejected. The custodian can see your reason and resubmit.");
        }

        // ---------- helpers ----------
        private EquipmentCategory NewCategory(string name, string description, string adminCode)
        {
            var now = DateTime.Now;
            return new EquipmentCategory
            {
                CategoryCode = MakeCode(name),
                CategoryName = name,
                Description = CleanDescription(description),
                IsActive = true,
                CreatedBy = adminCode,
                UpdatedBy = adminCode,
                CreatedTime = now,
                UpdatedTime = now
            };
        }

        private static bool TryName(string name, out string clean, out string error)
        {
            clean = Regex.Replace((name ?? "").Trim(), @"\s+", " ");
            error = null;
            if (clean.Length < 2 || clean.Length > 100)
            {
                error = "The category name must be 2 to 100 characters.";
                return false;
            }
            return true;
        }

        private static string CleanDescription(string description)
        {
            var d = (description ?? "").Trim();
            if (d.Length == 0) return null;
            return d.Length > 500 ? d.Substring(0, 500) : d;
        }

        // "Sewing & Garments" -> SEWING-GARMENTS (made unique with -2, -3, ...)
        private string MakeCode(string name)
        {
            var chars = name.ToUpperInvariant()
                            .Select(ch => char.IsLetterOrDigit(ch) && ch < 128 ? ch : '-')
                            .ToArray();
            var slug = Regex.Replace(new string(chars), "-+", "-").Trim('-');
            if (slug.Length == 0) slug = "CATEGORY";
            if (slug.Length > 40) slug = slug.Substring(0, 40).TrimEnd('-');

            var code = slug;
            var n = 2;
            while (_repo.GetCategories().Any(c => c.CategoryCode == code))
            {
                code = slug + "-" + n++;
            }
            return code;
        }

        private ServiceResult Save(string successMessage)
        {
            try
            {
                _repo.SaveChanges();
            }
            catch (Exception)
            {
                return ServiceResult.Fail("Could not save the change. Please try again.");
            }
            return ServiceResult.Ok(successMessage);
        }
    }
}
