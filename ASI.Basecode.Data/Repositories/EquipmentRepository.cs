using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ASI.Basecode.Data.Interfaces;
using ASI.Basecode.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace ASI.Basecode.Data.Repositories
{
    public class EquipmentRepository : IEquipmentRepository
    {
        private readonly TiaCContext _context;

        public EquipmentRepository(TiaCContext context)
        {
            _context = context;
        }

        public IQueryable<EquipmentItem> GetEquipmentItems()
        {
            return _context.EquipmentItems
                           .Include(e => e.Category)
                           .Where(e => e.Status != "archived");
        }

        public IQueryable<EquipmentCategory> GetCategories()
        {
            return _context.EquipmentCategories;
        }

        public EquipmentItem GetEquipmentById(int id)
        {
            return _context.EquipmentItems
                           .Include(e => e.Category)
                           .FirstOrDefault(e => e.Id == id);
        }

        public bool EquipmentExists(string itemCode)
        {
            return _context.EquipmentItems.Any(e => e.ItemCode == itemCode);
        }

        public void AddEquipment(EquipmentItem equipment)
        {
            _context.EquipmentItems.Add(equipment);
            _context.SaveChanges();
        }

        public void UpdateEquipment(EquipmentItem equipment)
        {
            _context.EquipmentItems.Update(equipment);
            _context.SaveChanges();
        }

        public void DeleteEquipment(EquipmentItem equipment)
        {
            // Set status to archived to perform a soft delete without altering schema
            equipment.Status = "archived";
            _context.EquipmentItems.Update(equipment);
            _context.SaveChanges();
        }
    }
}