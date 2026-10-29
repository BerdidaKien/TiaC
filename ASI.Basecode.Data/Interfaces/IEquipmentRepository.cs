using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ASI.Basecode.Data.Models;

namespace ASI.Basecode.Data.Interfaces
{
    public interface IEquipmentRepository
    {
        IQueryable<EquipmentItem> GetEquipmentItems();
        IQueryable<EquipmentCategory> GetCategories();
        EquipmentItem GetEquipmentById(int id);
        bool EquipmentExists(string itemCode);
        void AddEquipment(EquipmentItem equipment);
        void UpdateEquipment(EquipmentItem equipment);
        void DeleteEquipment(EquipmentItem equipment);
    }
}