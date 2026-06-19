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

            ForwardRequestHeader(client, "X-Sente-Edit-Lock-Token");
            ForwardRequestHeader(client, "X-Sente-ServiceRecord-Id");
            ForwardRequestHeader(client, "X-Sente-RowVersion");

            return client;
        }

        private void ForwardRequestHeader(HttpClient client, string headerName)
        {
            var value = _httpContextAccessor.HttpContext?.Request.Headers[headerName].FirstOrDefault();

            if (!string.IsNullOrWhiteSpace(value))
            {
                client.DefaultRequestHeaders.TryAddWithoutValidation(headerName, value);
            }
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
                    defaultErrorMessage,
                    statusCode);
            }

            try
            {
                var result = JsonSerializer.Deserialize<ApiResponse<T>>(
                    responseText,
                    JsonOptions);

                if (result is not null)
                {
                    result.StatusCode = statusCode;
                    result.ErrorMessages ??= new List<string>();

                    if (response.IsSuccessStatusCode)
                    {
                        result.IsSuccess = true;
                        return result;
                    }

                    NormalizeFailedApiResponse(
                        result,
                        responseText,
                        defaultErrorMessage,
                        statusCode);

                    return result;
                }

                return ApiResponse<T>.Fail(
                    defaultErrorMessage,
                    statusCode);
            }
            catch (JsonException ex)
            {
                _logger.LogError(
                    ex,
                    "API response could not be deserialized. StatusCode: {StatusCode}, Response: {Response}",
                    statusCode,
                    responseText);

                var errorMessages = ExtractErrorMessagesFromRawResponse(
                    responseText,
                    defaultErrorMessage);

                var errorResponse = ApiResponse<T>.Fail(
                    errorMessages.FirstOrDefault() ?? defaultErrorMessage,
                    statusCode);

                errorResponse.ErrorMessages = errorMessages;

                return errorResponse;
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

        private static void NormalizeFailedApiResponse<T>(
    ApiResponse<T> result,
    string responseText,
    string defaultErrorMessage,
    int statusCode)
        {
            result.IsSuccess = false;
            result.StatusCode = statusCode;
            result.ErrorMessages ??= new List<string>();

            var extractedMessages = ExtractErrorMessagesFromRawResponse(
                responseText,
                defaultErrorMessage);

            if (result.ErrorMessages.Count == 0)
            {
                result.ErrorMessages = extractedMessages;
            }

            if (string.IsNullOrWhiteSpace(result.ErrorMessage))
            {
                result.ErrorMessage = result.ErrorMessages.FirstOrDefault()
                    ?? extractedMessages.FirstOrDefault()
                    ?? defaultErrorMessage;
            }

            if (result.ErrorMessages.Count == 0 &&
                !string.IsNullOrWhiteSpace(result.ErrorMessage))
            {
                result.ErrorMessages.Add(result.ErrorMessage);
            }
        }

        private static List<string> ExtractErrorMessagesFromRawResponse(
            string responseText,
            string defaultErrorMessage)
        {
            var messages = new List<string>();

            if (string.IsNullOrWhiteSpace(responseText))
            {
                messages.Add(defaultErrorMessage);
                return messages;
            }

            try
            {
                using var document = JsonDocument.Parse(responseText);
                var root = document.RootElement;

                if (root.TryGetProperty("errorMessages", out var errorMessagesElement) &&
                    errorMessagesElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in errorMessagesElement.EnumerateArray())
                    {
                        var message = item.GetString();

                        if (!string.IsNullOrWhiteSpace(message))
                            messages.Add(message);
                    }
                }

                if (root.TryGetProperty("errorMessage", out var errorMessageElement))
                {
                    var message = errorMessageElement.GetString();

                    if (!string.IsNullOrWhiteSpace(message))
                        messages.Add(message);
                }

                if (root.TryGetProperty("errors", out var errorsElement) &&
                    errorsElement.ValueKind == JsonValueKind.Object)
                {
                    foreach (var property in errorsElement.EnumerateObject())
                    {
                        if (property.Value.ValueKind != JsonValueKind.Array)
                            continue;

                        foreach (var item in property.Value.EnumerateArray())
                        {
                            var message = item.GetString();

                            if (!string.IsNullOrWhiteSpace(message))
                                messages.Add(message);
                        }
                    }
                }

                if (root.TryGetProperty("detail", out var detailElement))
                {
                    var detail = detailElement.GetString();

                    if (!string.IsNullOrWhiteSpace(detail))
                        messages.Add(detail);
                }

                if (root.TryGetProperty("title", out var titleElement))
                {
                    var title = titleElement.GetString();

                    if (!string.IsNullOrWhiteSpace(title) &&
                        !title.Equals("One or more validation errors occurred.", StringComparison.OrdinalIgnoreCase))
                    {
                        messages.Add(title);
                    }
                }
            }
            catch
            {
                if (!responseText.TrimStart().StartsWith("{") &&
                    !responseText.TrimStart().StartsWith("["))
                {
                    messages.Add(responseText.Trim());
                }
            }

            messages = messages
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct()
                .ToList();

            if (messages.Count == 0)
                messages.Add(defaultErrorMessage);

            return messages;
        }
    }
}
