using AutoStock.Repositories.Enums;
using AutoStock.Services.Dtos.Common;
using AutoStock.Services.Dtos.SupportRequests;
using AutoStock.WEB.Models.Common;
using AutoStock.WEB.Models.SupportRequests;
using System.Net.Http.Headers;
using System.Text.Json;

namespace AutoStock.WEB.Services
{
    public class SupportRequestApiService : BaseApiService
    {
        private readonly IHttpClientFactory _localHttpClientFactory;
        private readonly IConfiguration _localConfiguration;
        private readonly IHttpContextAccessor _localHttpContextAccessor;
        private readonly ILogger<SupportRequestApiService> _localLogger;

        public SupportRequestApiService(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            IHttpContextAccessor httpContextAccessor,
            ILogger<SupportRequestApiService> logger)
            : base(httpClientFactory, configuration, httpContextAccessor, logger)
        {
            _localHttpClientFactory = httpClientFactory;
            _localConfiguration = configuration;
            _localHttpContextAccessor = httpContextAccessor;
            _localLogger = logger;
        }

        public async Task<ApiResponse<PagedResult<SupportRequestListItemViewModel>>> GetPagedAsync(SupportRequestListQueryViewModel query)
        {
            query ??= new SupportRequestListQueryViewModel();

            var url = BuildUrlWithQuery("/api/supportrequests", new Dictionary<string, string?>
            {
                ["pageNumber"] = query.PageNumber.ToString(),
                ["pageSize"] = query.PageSize.ToString(),
                ["status"] = query.Status.HasValue ? ((int)query.Status.Value).ToString() : null,
                ["requestType"] = query.RequestType.HasValue ? ((int)query.RequestType.Value).ToString() : null,
                ["search"] = query.Search
            });

            var result = await GetAsync<PagedResult<SupportRequestListItemDto>>(url, "Destek talepleri alınırken hata oluştu.");

            return MapPagedList(result);
        }

        public async Task<ApiResponse<SupportRequestDetailViewModel>> GetByIdAsync(int id)
        {
            var result = await GetAsync<SupportRequestDetailDto>(
                $"/api/supportrequests/{id}",
                "Destek talebi detayı alınırken hata oluştu.");

            return MapDetail(result);
        }

        public async Task<ApiResponse<int>> CreateIssueAsync(CreateIssueSupportRequestViewModel model)
        {
            var dto = new CreateIssueSupportRequestDto
            {
                Subject = model.Subject,
                Description = model.Description,
                Priority = model.Priority
            };

            return await PostAsync<CreateIssueSupportRequestDto, int>(
                "/api/supportrequests/issue",
                dto,
                "Destek talebi oluşturulurken hata oluştu.");
        }

        public async Task<ApiResponse<int>> CreateUserRequestAsync(CreateUserSupportRequestViewModel model)
        {
            var dto = new CreateUserSupportRequestDto
            {
                RequestedUserFullName = model.RequestedUserFullName,
                RequestedUserPhone = model.RequestedUserPhone,
                RequestedUserEmail = model.RequestedUserEmail,
                RequestedUserRole = model.RequestedUserRole,
                Note = model.Note,
                Priority = model.Priority
            };

            return await PostAsync<CreateUserSupportRequestDto, int>(
                "/api/supportrequests/user-create-request",
                dto,
                "Kullanıcı ekleme talebi oluşturulurken hata oluştu.");
        }

        public async Task<ApiResponse<int>> AddMessageAsync(CreateSupportRequestMessageViewModel model)
        {
            var dto = new CreateSupportRequestMessageDto
            {
                Id = model.Id,
                Message = model.Message
            };

            return await PostAsync<CreateSupportRequestMessageDto, int>(
                $"/api/supportrequests/{model.Id}/messages",
                dto,
                "Destek talebine mesaj eklenirken hata oluştu.");
        }

        public async Task<ApiResponse<int>> CancelAsync(int id)
        {
            return await PostEmptyAsync<int>(
                $"/api/supportrequests/{id}/cancel",
                "Destek talebi iptal edilirken hata oluştu.");
        }

        public async Task<ApiResponse<PagedResult<SupportRequestListItemViewModel>>> GetPagedForAdminAsync(AdminSupportRequestListQueryViewModel query)
        {
            query ??= new AdminSupportRequestListQueryViewModel();

            var url = BuildUrlWithQuery("/api/adminsupportrequests", new Dictionary<string, string?>
            {
                ["pageNumber"] = query.PageNumber.ToString(),
                ["pageSize"] = query.PageSize.ToString(),
                ["status"] = query.Status.HasValue ? ((int)query.Status.Value).ToString() : null,
                ["requestType"] = query.RequestType.HasValue ? ((int)query.RequestType.Value).ToString() : null,
                ["search"] = query.Search,
                ["workshopId"] = query.WorkshopId?.ToString()
            });

            var result = await GetAsync<PagedResult<SupportRequestListItemDto>>(url, "Destek talepleri alınırken hata oluştu.");

            return MapPagedList(result);
        }

        public async Task<ApiResponse<SupportRequestDetailViewModel>> GetByIdForAdminAsync(int id)
        {
            var result = await GetAsync<SupportRequestDetailDto>(
                $"/api/adminsupportrequests/{id}",
                "Destek talebi detayı alınırken hata oluştu.");

            return MapDetail(result);
        }

        public async Task<ApiResponse<int>> AnswerAsync(AdminAnswerSupportRequestViewModel model)
        {
            var dto = new AdminAnswerSupportRequestDto
            {
                Id = model.Id,
                AdminResponse = model.AdminResponse,
                CloseAfterAnswer = model.CloseAfterAnswer
            };

            return await PostAsync<AdminAnswerSupportRequestDto, int>(
                $"/api/adminsupportrequests/{model.Id}/answer",
                dto,
                "Destek talebi yanıtlanırken hata oluştu.");
        }

        public async Task<ApiResponse<int>> UpdateStatusAsync(AdminUpdateSupportRequestStatusViewModel model)
        {
            var dto = new AdminUpdateSupportRequestStatusDto
            {
                Id = model.Id,
                Status = model.Status
            };

            return await PostAsync<AdminUpdateSupportRequestStatusDto, int>(
                $"/api/adminsupportrequests/{model.Id}/status",
                dto,
                "Destek talebi durumu güncellenirken hata oluştu.");
        }


        private async Task<ApiResponse<TResponse>> PostAsync<TRequest, TResponse>(
            string url,
            TRequest request,
            string defaultErrorMessage)
        {
            try
            {
                var client = _localHttpClientFactory.CreateClient();

                var baseUrl =
                    _localConfiguration["ApiSettings:BaseUrl"] ??
                    _localConfiguration["ApiBaseUrl"] ??
                    _localConfiguration["BaseUrl"];

                if (string.IsNullOrWhiteSpace(baseUrl))
                {
                    return ApiResponse<TResponse>.Fail("API adresi bulunamadı. appsettings içinde ApiSettings:BaseUrl / ApiBaseUrl / BaseUrl kontrol edilmeli.");
                }

                client.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");

                var token = _localHttpContextAccessor.HttpContext?.Session.GetString("AuthToken");

                if (!string.IsNullOrWhiteSpace(token))
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                }

                var normalizedUrl = url.TrimStart('/');
                var response = await client.PostAsJsonAsync(normalizedUrl, request);
                var content = await response.Content.ReadAsStringAsync();

                var jsonOptions = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                if (!response.IsSuccessStatusCode)
                {
                    var errorMessage = TryReadErrorMessage(content) ?? defaultErrorMessage;
                    return ApiResponse<TResponse>.Fail(errorMessage);
                }

                if (string.IsNullOrWhiteSpace(content))
                {
                    return ApiResponse<TResponse>.Success(default!);
                }

                var apiResponse = JsonSerializer.Deserialize<ApiResponse<TResponse>>(content, jsonOptions);

                if (apiResponse != null)
                {
                    return apiResponse;
                }

                var data = JsonSerializer.Deserialize<TResponse>(content, jsonOptions);

                return ApiResponse<TResponse>.Success(data!);
            }
            catch (Exception ex)
            {
                _localLogger.LogError(ex, defaultErrorMessage);
                return ApiResponse<TResponse>.Fail(defaultErrorMessage);
            }
        }

        private static string? TryReadErrorMessage(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                return null;

            try
            {
                using var document = JsonDocument.Parse(content);
                var root = document.RootElement;

                if (root.TryGetProperty("errorMessage", out var errorMessage) && errorMessage.ValueKind == JsonValueKind.String)
                    return errorMessage.GetString();

                if (root.TryGetProperty("message", out var message) && message.ValueKind == JsonValueKind.String)
                    return message.GetString();

                if (root.TryGetProperty("errors", out var errors))
                    return errors.ToString();
            }
            catch
            {
                // API bazen düz metin dönebilir.
            }

            return content;
        }

        private static ApiResponse<PagedResult<SupportRequestListItemViewModel>> MapPagedList(ApiResponse<PagedResult<SupportRequestListItemDto>> result)
        {
            if (result.IsFailure || result.Data == null)
            {
                return ApiResponse<PagedResult<SupportRequestListItemViewModel>>.Fail(result.ErrorMessage ?? "Destek talepleri alınırken hata oluştu.");
            }

            return ApiResponse<PagedResult<SupportRequestListItemViewModel>>.Success(new PagedResult<SupportRequestListItemViewModel>
            {
                Items = result.Data.Items.Select(MapListItem).ToList(),
                PageNumber = result.Data.PageNumber,
                PageSize = result.Data.PageSize,
                TotalCount = result.Data.TotalCount
            });
        }

        private static ApiResponse<SupportRequestDetailViewModel> MapDetail(ApiResponse<SupportRequestDetailDto> result)
        {
            if (result.IsFailure || result.Data == null)
            {
                return ApiResponse<SupportRequestDetailViewModel>.Fail(result.ErrorMessage ?? "Destek talebi detayı alınırken hata oluştu.");
            }

            return ApiResponse<SupportRequestDetailViewModel>.Success(MapDetail(result.Data));
        }

        private static SupportRequestListItemViewModel MapListItem(SupportRequestListItemDto dto)
        {
            return new SupportRequestListItemViewModel
            {
                Id = dto.Id,
                WorkshopId = dto.WorkshopId,
                WorkshopName = dto.WorkshopName,
                RequestType = dto.RequestType,
                RequestTypeText = dto.RequestTypeText,
                Status = dto.Status,
                StatusText = dto.StatusText,
                Priority = dto.Priority,
                PriorityText = dto.PriorityText,
                Subject = dto.Subject,
                CreatedByUserName = dto.CreatedByUserName,
                CreatedAt = dto.CreatedAt,
                RespondedAt = dto.RespondedAt,
                ClosedAt = dto.ClosedAt
            };
        }

        private static SupportRequestDetailViewModel MapDetail(SupportRequestDetailDto dto)
        {
            return new SupportRequestDetailViewModel
            {
                Id = dto.Id,
                WorkshopId = dto.WorkshopId,
                WorkshopName = dto.WorkshopName,
                CreatedByUserId = dto.CreatedByUserId,
                CreatedByUserName = dto.CreatedByUserName,
                RequestType = dto.RequestType,
                RequestTypeText = dto.RequestTypeText,
                Status = dto.Status,
                StatusText = dto.StatusText,
                Priority = dto.Priority,
                PriorityText = dto.PriorityText,
                Subject = dto.Subject,
                Description = dto.Description,
                RequestedUserFullName = dto.RequestedUserFullName,
                RequestedUserPhone = dto.RequestedUserPhone,
                RequestedUserEmail = dto.RequestedUserEmail,
                RequestedUserRole = dto.RequestedUserRole,
                RequestedUserRoleText = dto.RequestedUserRoleText,
                AdminResponse = dto.AdminResponse,
                RespondedByUserId = dto.RespondedByUserId,
                RespondedByUserName = dto.RespondedByUserName,
                RespondedAt = dto.RespondedAt,
                CreatedAt = dto.CreatedAt,
                UpdatedAt = dto.UpdatedAt,
                ClosedAt = dto.ClosedAt,
                Messages = dto.Messages.Select(x => new SupportRequestMessageViewModel
                {
                    Id = x.Id,
                    SupportRequestId = x.SupportRequestId,
                    SenderUserId = x.SenderUserId,
                    SenderUserName = x.SenderUserName,
                    IsAdminMessage = x.IsAdminMessage,
                    Message = x.Message,
                    CreatedAt = x.CreatedAt
                }).ToList()
            };
        }
    }
}
