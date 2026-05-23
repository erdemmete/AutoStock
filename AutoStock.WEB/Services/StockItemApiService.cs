using AutoStock.WEB.Models.StockItems;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace AutoStock.WEB.Services
{
    public class StockItemApiService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public StockItemApiService(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            IHttpContextAccessor httpContextAccessor)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<List<StockItemListViewModel>> GetListAsync()
        {
            var client = CreateApiClient();

            var response = await client.GetAsync("/api/StockItems");

            if (!response.IsSuccessStatusCode)
                return new List<StockItemListViewModel>();

            var json = await response.Content.ReadAsStringAsync();

            var result = JsonSerializer.Deserialize<List<StockItemListViewModel>>(
                json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return result ?? new List<StockItemListViewModel>();
        }

        public async Task<bool> CreateAsync(CreateStockItemViewModel model)
        {
            var client = CreateApiClient();

            var json = JsonSerializer.Serialize(model);

            var content = new StringContent(
                json,
                Encoding.UTF8,
                "application/json");

            var response = await client.PostAsync("/api/StockItems", content);

            return response.IsSuccessStatusCode;
        }

        public async Task<StockItemDetailViewModel?> GetByIdAsync(int id)
        {
            var client = CreateApiClient();

            var response = await client.GetAsync($"/api/StockItems/{id}");

            if (!response.IsSuccessStatusCode)
                return null;

            var json = await response.Content.ReadAsStringAsync();

            var result = JsonSerializer.Deserialize<StockItemDetailViewModel>(
                json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (result == null)
                return null;

            var movements = await GetMovementsAsync(id);

            result.Movements = movements;

            return result;
        }

        public async Task<List<StockMovementListViewModel>> GetMovementsAsync(int stockItemId)
        {
            var client = CreateApiClient();

            var response = await client.GetAsync($"/api/StockItems/{stockItemId}/movements");

            if (!response.IsSuccessStatusCode)
                return new List<StockMovementListViewModel>();

            var json = await response.Content.ReadAsStringAsync();

            var result = JsonSerializer.Deserialize<List<StockMovementListViewModel>>(
                json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return result ?? new List<StockMovementListViewModel>();
        }

        public async Task<bool> AdjustStockAsync(int stockItemId, AdjustStockViewModel model)
        {
            var client = CreateApiClient();

            var json = JsonSerializer.Serialize(model);

            var content = new StringContent(
                json,
                Encoding.UTF8,
                "application/json");

            var response = await client.PostAsync($"/api/StockItems/{stockItemId}/adjust-stock", content);

            return response.IsSuccessStatusCode;
        }

        public async Task<EditStockItemViewModel?> GetEditModelAsync(int id)
        {
            var detail = await GetByIdAsync(id);

            if (detail == null)
                return null;

            return new EditStockItemViewModel
            {
                Id = detail.Id,
                Name = detail.Name,
                Code = detail.Code,
                Barcode = detail.Barcode,
                Brand = detail.Brand,
                Unit = detail.Unit,
                PurchasePrice = detail.PurchasePrice,
                SalePrice = detail.SalePrice,
                MinimumQuantity = detail.MinimumQuantity
            };
        }

        public async Task<bool> UpdateAsync(EditStockItemViewModel model)
        {
            var client = CreateApiClient();

            var json = JsonSerializer.Serialize(model);

            var content = new StringContent(
                json,
                Encoding.UTF8,
                "application/json");

            var response = await client.PutAsync($"/api/StockItems/{model.Id}", content);

            return response.IsSuccessStatusCode;
        }

        public async Task<bool> SetPassiveAsync(int id)
        {
            var client = CreateApiClient();

            var response = await client.PostAsync($"/api/StockItems/{id}/passive", null);

            return response.IsSuccessStatusCode;
        }

        private HttpClient CreateApiClient()
        {
            var client = _httpClientFactory.CreateClient();

            client.BaseAddress = new Uri(_configuration["ApiSettings:BaseUrl"]!);

            var token = _httpContextAccessor.HttpContext?.Session.GetString("AuthToken");

            if (!string.IsNullOrWhiteSpace(token))
            {
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);
            }

            return client;
        }
    }
}