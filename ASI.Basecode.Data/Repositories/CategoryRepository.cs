using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ASI.Basecode.Data.Models;
using ASI.Basecode.Data.Interfaces;

namespace ASI.Basecode.Data.Repositories
{
    public class CategoryRepository : ICategoryRepository
    {
        private readonly TiaCContext _context;

        public CategoryRepository(TiaCContext context)
        {
            _context = context;
        }

        public IQueryable<EquipmentCategory> GetCategories() => _context.EquipmentCategories;

        public EquipmentCategory GetCategoryById(int id) =>
            _context.EquipmentCategories.FirstOrDefault(c => c.Id == id);

        public IQueryable<EquipmentItem> GetItems() => _context.EquipmentItems;

        public EquipmentItem GetItemById(int id) =>
            _context.EquipmentItems.FirstOrDefault(i => i.Id == id);

        public void AddCategory(EquipmentCategory category) => _context.EquipmentCategories.Add(category);

        public void DeleteCategory(EquipmentCategory category) => _context.EquipmentCategories.Remove(category);

        public void SaveChanges() => _context.SaveChanges();
    }
}