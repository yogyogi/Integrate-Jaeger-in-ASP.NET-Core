using JaegerTutorial.Models;
using Microsoft.AspNetCore.Mvc;

namespace JaegerTutorial.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EmployeeController : ControllerBase
    {
        private CompanyContext context;
        public EmployeeController(CompanyContext cc)
        {
            context = cc;
        }

        [HttpGet("GetEmployees")]
        public IActionResult GetEmployees()
        {
            var employees = context.Employee;

            return Ok(employees);
        }
    }
}
