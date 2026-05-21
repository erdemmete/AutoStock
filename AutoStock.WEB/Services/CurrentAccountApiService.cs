using AutoStock.Mobile.Models.CurrentAccounts;
using AutoStock.WEB.Models.Common;
using AutoStock.WEB.Models.CurrentAccounts;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace AutoStock.WEB.Services
{
    public class CurrentAccountApiService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentAccountApiService(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            IHttpContextAccessor httpContextAccessor)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<CustomerCurrentAccountViewModel?> GetCustomerAccountAsync(int customerId)
        {
            var client = CreateApiClient();

            var response = await client.GetAsync($"/api/current-accounts/customers/{customerId}");

            if (!response.IsSuccessStatusCode)
                return null;

            var json = await response.Content.ReadAsStringAsync();

            var result = JsonSerializer.Deserialize<ApiResponse<CustomerCurrentAccountViewModel>>(
                json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return result?.Data;
        }

        public async Task<bool> CreatePaymentAsync(CreatePaymentViewModel model)
        {
            var client = CreateApiClient();

            var json = JsonSerializer.Serialize(model);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PostAsync("/api/current-accounts/payments", content);

            return response.IsSuccessStatusCode;
        }

        public async Task<CurrentAccountSummaryViewModel?> GetSummaryAsync()
        {
            var client = CreateApiClient();

            var response = await client.GetAsync("/api/current-accounts/summary");

            if (!response.IsSuccessStatusCode)
                return null;

            var json = await response.Content.ReadAsStringAsync();

            var result = JsonSerializer.Deserialize<ApiResponse<CurrentAccountSummaryViewModel>>(
                json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return result?.Data;
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