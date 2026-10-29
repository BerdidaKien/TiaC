using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASI.Basecode.Services.ServiceModels
{
    public class ServiceResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }

        public static ServiceResult Ok(string message) => new ServiceResult { Success = true, Message = message };
        public static ServiceResult Fail(string message) => new ServiceResult { Success = false, Message = message };
    }
}
