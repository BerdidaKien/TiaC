using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ASI.Basecode.Data.Models;

namespace ASI.Basecode.Data.Interfaces
{
    public interface ICategoryRepository
    {
        IQueryable<EquipmentCategory> GetCategories();
        EquipmentCategory GetCategoryById(int id);
        IQueryable<EquipmentItem> GetItems();          // all items, including archived
        EquipmentItem GetItemById(int id);
        void AddCategory(EquipmentCategory category);
        void DeleteCategory(EquipmentCategory category);
        void SaveChanges();
    }
}