using System.Collections.Generic;
using ASI.Basecode.Data.Models;
using ASI.Basecode.Services.ServiceModels;

namespace ASI.Basecode.Services.Interfaces
{
    public interface IEquipmentService
    {
        // custodianCode = null means "all equipment" (students, admin)
        IEnumerable<EquipmentItem> GetAllEquipment(string custodianCode = null);
        IEnumerable<EquipmentCategory> GetCategories();
        ServiceResult AddEquipment(EquipmentItem item, string custodianCode = null);
        DashboardViewModel GetDashboardSummary(string search, int? categoryId, string status, string custodianCode = null);

        // ---- Edit / archive (custodian) ----
        EquipmentItem GetEquipmentById(int id);
        ServiceResult UpdateEquipment(EquipmentItem edited, string custodianCode = null);
        ServiceResult ArchiveEquipment(int id, string updatedBy, string custodianCode = null);
    }
}
