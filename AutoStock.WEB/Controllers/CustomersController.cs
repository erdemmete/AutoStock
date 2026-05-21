using AutoStock.WEB.Models.Common;
using AutoStock.WEB.Models.Customers;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace AutoStock.WEB.Controllers
{
    public class CustomersController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public CustomersController(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        [HttpGet("Customers")]
        public async Task<IActionResult> Index()
        {
            var client = CreateApiClient();

            var response = await client.GetAsync("/api/Customers");

            if (!response.IsSuccessStatusCode)
            {
                ViewBag.ErrorMessage = "Müşteriler getirilemedi.";
                return View(new CustomerListViewModel());
            }

            var json = await response.Content.ReadAsStringAsync();

            var result = JsonSerializer.Deserialize<ApiResponse<List<CustomerListItemViewModel>>>(
                json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return View(new CustomerListViewModel
            {
                Customers = result?.Data ?? new List<CustomerListItemViewModel>()
            });
        }

        [HttpGet("Customers/Create")]
        public IActionResult Create()
        {
            return View(new CreateCustomerViewModel());
        }

        [HttpPost("Customers/Create")]
        public async Task<IActionResult> Create(CreateCustomerViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.PhoneNumber))
                ModelState.AddModelError(nameof(model.PhoneNumber), "Telefon numarası zorunludur.");

            if (model.Type == AutoStock.Repositories.Enums.CustomerType.Individual &&
                string.IsNullOrWhiteSpace(model.FullName))
                ModelState.AddModelError(nameof(model.FullName), "Ad soyad zorunludur.");

            if (model.Type == AutoStock.Repositories.Enums.CustomerType.Corporate &&
                string.IsNullOrWhiteSpace(model.CompanyName))
                ModelState.AddModelError(nameof(model.CompanyName), "Firma adı zorunludur.");

            if (!ModelState.IsValid)
                return View(model);

            var client = CreateApiClient();

            var json = JsonSerializer.Serialize(model);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PostAsync("/api/Customers", content);

            if (!response.IsSuccessStatusCode)
            {
                ModelState.AddModelError("", "Müşteri oluşturulurken hata oluştu.");
                return View(model);
            }

            return RedirectToAction(nameof(Index));
        }

        private HttpClient CreateApiClient()
        {
            var client = _httpClientFactory.CreateClient();

            client.BaseAddress = new Uri(_configuration["ApiSettings:BaseUrl"]!);

            var token = HttpContext.Session.GetString("AuthToken");

            if (!string.IsNullOrWhiteSpace(token))
            {
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);
            }

            return client;
        }
    }
}