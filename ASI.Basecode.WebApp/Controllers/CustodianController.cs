using ASI.Basecode.WebApp.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using AutoMapper;

namespace ASI.Basecode.WebApp.Controllers
{
    [Authorize(Roles = "Custodian,Admin")]
    public class CustodianController : ControllerBase<CustodianController>
    {
        public CustodianController(
            IHttpContextAccessor httpContextAccessor,
            ILoggerFactory loggerFactory,
            IConfiguration configuration,
            IMapper mapper) : base(httpContextAccessor, loggerFactory, configuration, mapper)
        {
        }

        public IActionResult Index()
        {
            return View();
        }
        public IActionResult Reservations()
        {
            return View();
        }
    }
}