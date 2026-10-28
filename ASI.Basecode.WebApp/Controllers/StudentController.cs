using ASI.Basecode.WebApp.Mvc;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ASI.Basecode.WebApp.Controllers
{
    [Authorize(Roles = "Student,user")]
    public class StudentController : ControllerBase<StudentController>
    {
        public StudentController(
            IHttpContextAccessor httpContextAccessor,
            ILoggerFactory loggerFactory,
            IConfiguration configuration,
            IMapper mapper) : base(httpContextAccessor, loggerFactory, configuration, mapper)
        {
        }

        /// <summary>
        /// Student Catalog / Dashboard
        /// </summary>
        public IActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// Active and past equipment reservation history
        /// </summary>
        public IActionResult MyReservations()
        {
            return View();
        }

        /// <summary>
        /// View specific equipment details before borrowing
        /// </summary>
        public IActionResult EquipmentDetails(int id)
        {
            return View();
        }
    }
}