using AutoStock.WEB.Models.Admin.Workshops;
using AutoStock.WEB.Models.Common;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace AutoStock.WEB.Services
{
    public class AdminWorkshopApiService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _httpContextAccessor;

        private readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public AdminWorkshopApiService(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            IHttpContextAccessor httpContextAccessor)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<List<AdminWorkshopListItemViewModel>> GetListAsync()
        {
            var client = CreateApiClient();

            var response = await client.GetAsync("/api/admin/workshops");

            if (!response.IsSuccessStatusCode)
                return new List<AdminWorkshopListItemViewModel>();

            var json = await response.Content.ReadAsStringAsync();

            var result = JsonSerializer.Deserialize<ApiResponse<List<AdminWorkshopListItemViewModel>>>(
                json,
                _jsonOptions);

            return result?.Data ?? new List<AdminWorkshopListItemViewModel>();
        }

        public async Task<AdminWorkshopDetailViewModel?> GetByIdAsync(int id)
        {
            var client = CreateApiClient();

            var response = await client.GetAsync($"/api/admin/workshops/{id}");

            if (!response.IsSuccessStatusCode)
                return null;

            var json = await response.Content.ReadAsStringAsync();

            var result = JsonSerializer.Deserialize<ApiResponse<AdminWorkshopDetailViewModel>>(
                json,
                _jsonOptions);

            return result?.Data;
        }

        public async Task<(bool IsSuccess, int? WorkshopId, string ErrorMessage)> CreateAsync(CreateAdminWorkshopViewModel model)
        {
            var client = CreateApiClient();

            var json = JsonSerializer.Serialize(model);

            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PostAsync("/api/admin/workshops", content);

            var responseText = await response.Content.ReadAsStringAsync();

            if (string.IsNullOrWhiteSpace(responseText))
                return (false, null, "API boş cevap döndü.");

            var parsedResult = ParseApiResponse(responseText);

            if (parsedResult.IsSuccess)
            {
                int? workshopId = null;

                using var document = JsonDocument.Parse(responseText);

                if (document.RootElement.TryGetProperty("data", out var dataElement) &&
                    dataElement.ValueKind == JsonValueKind.Number)
                {
                    workshopId = dataElement.GetInt32();
                }

                return (true, workshopId, string.Empty);
            }

            return (false, null, parsedResult.ErrorMessage);
        }

        public async Task<(bool IsSuccess, string ErrorMessage)> UpdateSubscriptionAsync(UpdateAdminWorkshopSubscriptionViewModel model)
        {
            var client = CreateApiClient();

            var requestBody = new
            {
                isActive = model.IsActive,
                subscriptionStatus = model.SubscriptionStatus,
                subscriptionEndDate = model.SubscriptionEndDate,
                subscriptionNote = model.SubscriptionNote
            };

            var json = JsonSerializer.Serialize(requestBody);

            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PutAsync(
                $"/api/admin/workshops/{model.WorkshopId}/subscription",
                content);

            var responseText = await response.Content.ReadAsStringAsync();

            if (string.IsNullOrWhiteSpace(responseText))
                return (false, "API boş cevap döndü.");

            return ParseApiResponse(responseText);
        }

        public async Task<(bool IsSuccess, string ErrorMessage)> UpdateProfileAsync(UpdateAdminWorkshopProfileViewModel model)
        {
            var client = CreateApiClient();

            var requestBody = new
            {
                displayName = model.DisplayName,
                legalTitle = model.LegalTitle,
                taxOffice = model.TaxOffice,
                taxNumber = model.TaxNumber,
                tradeRegistryNumber = model.TradeRegistryNumber,
                mersisNumber = model.MersisNumber,
                email = model.Email,
                phoneNumber = model.PhoneNumber,
                faxNumber = model.FaxNumber,
                website = model.Website,
                addressLine = model.AddressLine,
                city = model.City,
                district = model.District,
                postalCode = model.PostalCode,
                country = model.Country
            };

            var json = JsonSerializer.Serialize(requestBody);

            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PutAsync(
                $"/api/admin/workshops/{model.WorkshopId}/profile",
                content);

            var responseText = await response.Content.ReadAsStringAsync();

            if (string.IsNullOrWhiteSpace(responseText))
                return (false, "API boş cevap döndü.");

            return ParseApiResponse(responseText);
        }

        public async Task<(bool IsSuccess, string ErrorMessage)> CreatePartnerAsync(CreateAdminWorkshopPartnerViewModel model)
        {
            var client = CreateApiClient();

            var requestBody = new
            {
                fullName = model.FullName,
                title = model.Title,
                phoneNumber = model.PhoneNumber,
                email = model.Email,
                isPrimary = model.IsPrimary,
                note = model.Note
            };

            var json = JsonSerializer.Serialize(requestBody);

            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PostAsync(
                $"/api/admin/workshops/{model.WorkshopId}/partners",
                content);

            var responseText = await response.Content.ReadAsStringAsync();

            if (string.IsNullOrWhiteSpace(responseText))
                return (false, "API boş cevap döndü.");

            return ParseApiResponse(responseText);
        }

        public async Task<(bool IsSuccess, string ErrorMessage)> DeletePartnerAsync(int workshopId, int partnerId)
        {
            var client = CreateApiClient();

            var response = await client.DeleteAsync(
                $"/api/admin/workshops/{workshopId}/partners/{partnerId}");

            var responseText = await response.Content.ReadAsStringAsync();

            if (string.IsNullOrWhiteSpace(responseText))
                return (false, "API boş cevap döndü.");

            return ParseApiResponse(responseText);
        }

        public async Task<(bool IsSuccess, string ErrorMessage)> CreateUserAsync(
    CreateAdminWorkshopUserViewModel model)
        {
            var client = CreateApiClient();

            var requestBody = new
            {
                fullName = model.FullName,
                userName = model.UserName,
                email = model.Email,
                password = model.Password,
                role = model.Role
            };

            var json = JsonSerializer.Serialize(requestBody);

            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PostAsync(
                $"/api/admin/workshops/{model.WorkshopId}/users",
                content);

            var responseText = await response.Content.ReadAsStringAsync();

            if (string.IsNullOrWhiteSpace(responseText))
                return (false, "API boş cevap döndü.");

            return ParseApiResponse(responseText);
        }

        public async Task<(bool IsSuccess, string ErrorMessage)> UpdateUserStatusAsync(int workshopId, int userId, bool isActive)
        {
            var client = CreateApiClient();

            var requestBody = new
            {
                isActive
            };

            var json = JsonSerializer.Serialize(requestBody);

            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PutAsync(
                $"/api/admin/workshops/{workshopId}/users/{userId}/status",
                content);

            var responseText = await response.Content.ReadAsStringAsync();

            if (string.IsNullOrWhiteSpace(responseText))
                return (false, "API boş cevap döndü.");

            return ParseApiResponse(responseText);
        }

        public async Task<(bool IsSuccess, SuggestedAdminWorkshopCredentialsViewModel? Data, string ErrorMessage)> SuggestCredentialsAsync(int workshopId, string fullName)
        {
            var client = CreateApiClient();

            var url = $"/api/admin/workshops/{workshopId}/users/suggest-credentials?fullName={Uri.EscapeDataString(fullName)}";

            var response = await client.GetAsync(url);

            var responseText = await response.Content.ReadAsStringAsync();

            if (string.IsNullOrWhiteSpace(responseText))
                return (false, null, "API boş cevap döndü.");

            var result = JsonSerializer.Deserialize<ApiResponse<SuggestedAdminWorkshopCredentialsViewModel>>(
                responseText,
                _jsonOptions);

            if (result == null)
                return (false, null, "API cevabı okunamadı.");

            if (!result.IsSuccess || result.Data == null)
                return (false, null, result.ErrorMessage ?? "Kullanıcı adı ve şifre oluşturulamadı.");

            return (true, result.Data, string.Empty);
        }

        private (bool IsSuccess, string ErrorMessage) ParseApiResponse(string responseText)
        {
            using var document = JsonDocument.Parse(responseText);

            var root = document.RootElement;

            var isSuccess = root.TryGetProperty("isSuccess", out var isSuccessElement)
                            && isSuccessElement.ValueKind == JsonValueKind.True;

            if (isSuccess)
                return (true, string.Empty);

            var errorMessage = "İşlem başarısız.";

            if (root.TryGetProperty("errorMessage", out var errorElement))
            {
                if (errorElement.ValueKind == JsonValueKind.String)
                {
                    errorMessage = errorElement.GetString() ?? errorMessage;
                }
                else if (errorElement.ValueKind == JsonValueKind.Array)
                {
                    var errors = errorElement
                        .EnumerateArray()
                        .Select(x => x.GetString())
                        .Where(x => !string.IsNullOrWhiteSpace(x))
                        .ToList();

                    if (errors.Any())
                        errorMessage = string.Join("<br>", errors);
                }
            }

            return (false, errorMessage);
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