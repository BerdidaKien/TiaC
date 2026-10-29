using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ASI.Basecode.Data.Models;

namespace ASI.Basecode.Services.Interfaces
{
    public interface IEquipmentService
    {
        IEnumerable<EquipmentItem> GetAllEquipment();
        IEnumerable<EquipmentCategory> GetCategories();
        void AddEquipment(EquipmentItem item);
    }
}
