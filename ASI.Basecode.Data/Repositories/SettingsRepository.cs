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
    public class SettingsRepository : ISettingsRepository
    {
        private readonly TiaCContext _context;

        public SettingsRepository(TiaCContext context)
        {
            _context = context;
        }

        public List<SystemSetting> GetAll()
        {
            return _context.SystemSettings.AsNoTracking().ToList();
        }

        // Updates the rows that exist and adds the missing ones, in one save
        public void SaveAll(Dictionary<string, string> values, string updatedBy)
        {
            var existing = _context.SystemSettings.ToDictionary(x => x.SettingKey);
            var now = DateTime.Now;

            foreach (var pair in values)
            {
                if (existing.TryGetValue(pair.Key, out var row))
                {
                    row.SettingValue = pair.Value;
                    row.UpdatedBy = updatedBy;
                    row.UpdatedTime = now;
                }
                else
                {
                    _context.SystemSettings.Add(new SystemSetting
                    {
                        SettingKey = pair.Key,
                        SettingValue = pair.Value,
                        UpdatedBy = updatedBy,
                        UpdatedTime = now
                    });
                }
            }

            _context.SaveChanges();
        }
    }
}