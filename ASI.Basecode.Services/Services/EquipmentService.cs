using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ASI.Basecode.Data.Interfaces;
using ASI.Basecode.Data.Models;
using ASI.Basecode.Data.Repositories;
using ASI.Basecode.Services.Interfaces;

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
    }
}