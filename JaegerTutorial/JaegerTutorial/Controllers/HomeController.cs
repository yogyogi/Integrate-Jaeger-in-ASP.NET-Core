using JaegerTutorial.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;
using RabbitMQ.Client;
using System.Diagnostics;
using System.Text;

namespace JaegerTutorial.Controllers
{
    public class HomeController : Controller
    {
        private CompanyContext context;
        private AppActivitySource appActivitySource;
        private readonly IDistributedCache cache;

        public HomeController(CompanyContext context, AppActivitySource appActivitySource, IDistributedCache cache)
        {
            this.context = context;
            this.appActivitySource = appActivitySource;
            this.cache = cache;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(Employee emp)
        {
            context.Add(emp);
            await context.SaveChangesAsync();

            using (Activity? activity = appActivitySource.AppActivity("CRUD Operations"))
            {
                activity?.SetTag("CREATE Employee", "Create Action HTTPOST");
            }

            ViewBag.Message = "Employee created successfully!";
            return View();
        }

        public IActionResult CreateError()
        {
            return View();
        }

        [HttpPost]
        [ActionName("CreateError")]
        public async Task<IActionResult> CreateE()
        {
            Employee emp = new Employee
            {
                FirstName = "John ",
                LastName = " Doe",
                Gender = "Male",
                Email = "johndoe@yahoo.com",
                DateOfBirth = Convert.ToDateTime("Jan 20, 1999"),
                Designation = "Software Engineer"
            };
            context.Add(emp);
            await context.SaveChangesAsync();
            ViewBag.Message = "Error Has Occurred";
            return View();
        }

        public async Task<IActionResult> Read()
        {
            var employees = context.Employee;
            return View(employees);
        }

        public async Task<IActionResult> ReadApi()
        {
            var employees = new List<Employee>();
            using (var httpClient = new HttpClient())
            {
                using (var response = await httpClient.GetAsync("https://localhost:7190/api/Employee/GetEmployees"))
                {
                    string apiResponse = await response.Content.ReadAsStringAsync();
                    employees = JsonConvert.DeserializeObject<List<Employee>>(apiResponse);
                }
            }
            return View(employees);
        }

        public async Task<IActionResult> ReadCache()
        {
            var employees = await GetAll();
            return View(employees);
        }

        public async Task<List<Employee>> GetAll()
        {
            var cacheKey = "employees";
            var cacheOptions = new DistributedCacheEntryOptions()
                    .SetAbsoluteExpiration(TimeSpan.FromMinutes(20))
                    .SetSlidingExpiration(TimeSpan.FromMinutes(2));
            var products = await cache.GetOrSetAsync(
                cacheKey,
                async () =>
                {
                    return await context.Employee.ToListAsync();
                },
                cacheOptions)!;
            return products!;
        }

        public async Task<IActionResult> SendMessage(Employee emp)
        {
            var factory = new ConnectionFactory { HostName = "localhost" };
            using var connection = await factory.CreateConnectionAsync();
            using var channel = await connection.CreateChannelAsync();

            await channel.QueueDeclareAsync(queue: "hello", durable: false, exclusive: false, autoDelete: false,
                arguments: null);

            const string message = "Hello World!";
            var body = Encoding.UTF8.GetBytes(message);

            await channel.BasicPublishAsync(exchange: string.Empty, routingKey: "hello", body: body);

            return RedirectToAction("Index");
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
