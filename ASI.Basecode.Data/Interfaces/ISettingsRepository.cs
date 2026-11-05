using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ASI.Basecode.Data.Models;

namespace ASI.Basecode.Data.Interfaces
{
    public interface ISettingsRepository
    {
        List<SystemSetting> GetAll();
        void SaveAll(Dictionary<string, string> values, string updatedBy);
    }
}