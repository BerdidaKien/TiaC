using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASI.Basecode.Data.Models;

public partial class SystemSetting
{
    public string SettingKey { get; set; }

    public string SettingValue { get; set; }

    public string UpdatedBy { get; set; }

    public DateTime UpdatedTime { get; set; }
}