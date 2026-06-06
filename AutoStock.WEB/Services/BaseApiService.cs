using AutoStock.WEB.Models.Common;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace AutoStock.WEB.Services
{
    public abstract class BaseApiService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger _logger;

        protected static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        protected BaseApiService(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            IHttpContextAccessor httpContextAccessor,
            ILogger logger)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        protected HttpClient CreateApiClient()
        {
            var apiBaseUrl = _configuration["ApiSettings:BaseUrl"];

            if (string.IsNullOrWhiteSpace(apiBaseUrl))
            {
                throw new InvalidOperationException("ApiSettings:BaseUrl is not configured.");
            }

            var client = _httpClientFactory.CreateClient();

            client.BaseAddress = new Uri(apiBaseUrl);

            var token = _httpContextAccessor.HttpContext?.Session.GetString("AuthToken");

            if (!string.IsNullOrWhiteSpace(token))
            {
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);
            }

            return client;
        }

        protected async Task<ApiResponse<TResponse>> SendAsync<TResponse>(
            string url,
            Func<HttpClient, Task<HttpResponseMessage>> sendRequestAsync,
            string defaultErrorMessage = "API işlemi başarısız oldu.")
        {
            try
            {
                var client = CreateApiClient();

                using var response = await sendRequestAsync(client);

                return await ReadApiResponseAsync<TResponse>(
                    response,
                    defaultErrorMessage);
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogError(
                    ex,
                    "API request timed out. Url: {Url}",
                    url);

                return ApiResponse<TResponse>.Fail(
                    "API isteği zaman aşımına uğradı. Lütfen tekrar deneyin.");
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(
                    ex,
                    "API connection error. Url: {Url}",
                    url);

                return ApiResponse<TResponse>.Fail(
                    "API ile bağlantı kurulamadı. Lütfen tekrar deneyin.");
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Unexpected WEB API client error. Url: {Url}",
                    url);

                return ApiResponse<TResponse>.Fail(defaultErrorMessage);
            }
        }

        protected async Task<ApiResponse<T>> ReadApiResponseAsync<T>(
            HttpResponseMessage response,
            string defaultErrorMessage = "API işlemi başarısız oldu.")
        {
            var statusCode = (int)response.StatusCode;

            var responseText = await response.Content.ReadAsStringAsync();

            if (string.IsNullOrWhiteSpace(responseText))
            {
                if (response.IsSuccessStatusCode)
                    return ApiResponse<T>.Success(default, statusCode);

                return ApiResponse<T>.Fail(
                    "API boş cevap döndü.",
                    statusCode);
            }

            try
            {
                var result = JsonSerializer.Deserialize<ApiResponse<T>>(
                    responseText,
                    JsonOptions);

                if (result == null)
                {
                    return ApiResponse<T>.Fail(
                        "API cevabı okunamadı.",
                        statusCode);
                }

                result.StatusCode = statusCode;
                result.ErrorMessages ??= new List<string>();

                if (result.IsFailure && string.IsNullOrWhiteSpace(result.ErrorMessage))
                {
                    result.ErrorMessage = result.ErrorMessages.FirstOrDefault()
                        ?? defaultErrorMessage;
                }

                if (!response.IsSuccessStatusCode && string.IsNullOrWhiteSpace(result.ErrorMessage))
                {
                    result.ErrorMessage = defaultErrorMessage;
                }

                if (result.IsFailure &&
                    result.ErrorMessages.Count == 0 &&
                    !string.IsNullOrWhiteSpace(result.ErrorMessage))
                {
                    result.ErrorMessages.Add(result.ErrorMessage);
                }

                return result;
            }
            catch (JsonException ex)
            {
                _logger.LogError(
                    ex,
                    "API response could not be deserialized. StatusCode: {StatusCode}, Response: {Response}",
                    statusCode,
                    responseText);

                return ApiResponse<T>.Fail(
                    "API cevabı beklenen formatta değil.",
                    statusCode);
            }
        }

        protected async Task<ApiResponse<TResponse>> GetAsync<TResponse>(
            string url,
            string defaultErrorMessage = "API işlemi başarısız oldu.")
        {
            return await SendAsync<TResponse>(
                url,
                client => client.GetAsync(url),
                defaultErrorMessage);
        }

        protected async Task<ApiResponse<TResponse>> PostJsonAsync<TRequest, TResponse>(string url, TRequest model, string defaultErrorMessage = "API işlemi başarısız oldu.")
        {
            var json = JsonSerializer.Serialize(model, JsonOptions);

            using var content = new StringContent(
                json,
                Encoding.UTF8,
                "application/json");

            return await SendAsync<TResponse>(
                url,
                client => client.PostAsync(url, content),
                defaultErrorMessage);
        }

        protected async Task<ApiResponse<TResponse>> PutJsonAsync<TRequest, TResponse>(string url, TRequest model, string defaultErrorMessage = "API işlemi başarısız oldu.")
        {
            var json = JsonSerializer.Serialize(model, JsonOptions);

            using var content = new StringContent(
                json,
                Encoding.UTF8,
                "application/json");

            return await SendAsync<TResponse>(
                url,
                client => client.PutAsync(url, content),
                defaultErrorMessage);
        }

        protected async Task<ApiResponse<TResponse>> PostEmptyAsync<TResponse>(
            string url,
            string defaultErrorMessage = "API işlemi başarısız oldu.")
        {
            return await SendAsync<TResponse>(
                url,
                client => client.PostAsync(url, null),
                defaultErrorMessage);
        }

        protected static string BuildUrlWithQuery(string url, Dictionary<string, string?> queryParams)
        {
            var queryString = string.Join("&",
                queryParams
                    .Where(x => !string.IsNullOrWhiteSpace(x.Value))
                    .Select(x =>
                        $"{Uri.EscapeDataString(x.Key)}={Uri.EscapeDataString(x.Value!)}"));

            if (string.IsNullOrWhiteSpace(queryString))
                return url;

            return $"{url}?{queryString}";
        }

        protected async Task<ApiResponse<TResponse>> DeleteAsync<TResponse>(string url, string defaultErrorMessage = "API işlemi başarısız oldu.")
        {
            return await SendAsync<TResponse>(
                url,
                client => client.DeleteAsync(url),
                defaultErrorMessage);
        }
    }
}