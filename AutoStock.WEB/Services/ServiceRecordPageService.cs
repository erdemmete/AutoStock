using AutoStock.Repositories.Enums;
using AutoStock.Services.Dtos.ServiceRecords;
using AutoStock.Web.Models.ServiceRecords;
using AutoStock.WEB.Models.Common;
using AutoStock.WEB.Models.ServiceRecords;

namespace AutoStock.WEB.Services
{
    public class ServiceRecordPageService
    {
        private readonly ServiceRecordApiService _serviceRecordApiService;

        public ServiceRecordPageService(ServiceRecordApiService serviceRecordApiService)
        {
            _serviceRecordApiService = serviceRecordApiService;
        }

        public async Task<PageViewResult<ServiceRecordIndexViewModel>> GetIndexPageAsync(
            ServiceRecordListQueryViewModel query)
        {
            NormalizeQuery(query);

            var result = await _serviceRecordApiService.GetPagedAsync(query);

            var viewModel = new ServiceRecordIndexViewModel
            {
                Query = query,
                ServiceRecords = result.Data ?? CreateEmptyPagedResult(query)
            };

            if (result.IsFailure)
            {
                var errors = result.ErrorMessages.Any()
                    ? result.ErrorMessages
                    : new List<string>
                    {
                        result.ErrorMessage ?? "Servis kayıtları alınırken hata oluştu."
                    };

                return PageViewResult<ServiceRecordIndexViewModel>.WithErrors(
                    viewModel,
                    errors);
            }

            return PageViewResult<ServiceRecordIndexViewModel>.Success(viewModel);
        }

        public async Task<ApiResponse<object>> UpdateRequestItemAsync(UpdateServiceRequestItemFormModel form)
        {
            if (form.ServiceRequestItemId <= 0)
                return ApiResponse<object>.Fail("Şikayet bilgisi bulunamadı.");

            if (string.IsNullOrWhiteSpace(form.Title))
                return ApiResponse<object>.Fail("Şikayet başlığı zorunludur.");

            var request = new UpdateServiceRequestItemRequest
            {
                Title = form.Title.Trim(),
                Note = string.IsNullOrWhiteSpace(form.Note)
                    ? null
                    : form.Note.Trim(),
                EstimatedAmount = form.EstimatedAmount
            };

            return await _serviceRecordApiService.UpdateRequestItemAsync(
                form.ServiceRequestItemId,
                request);
        }

        public async Task<ApiResponse<ServiceOperationDto>> UpdateOperationAsync(UpdateServiceOperationFormModel form)
        {
            if (form.OperationId <= 0)
                return ApiResponse<ServiceOperationDto>.Fail("İşlem bilgisi bulunamadı.");

            if (string.IsNullOrWhiteSpace(form.Description))
                return ApiResponse<ServiceOperationDto>.Fail("İşlem açıklaması zorunludur.");

            if (form.Quantity <= 0)
                return ApiResponse<ServiceOperationDto>.Fail("Miktar 1 veya daha büyük olmalıdır.");

            if (form.UnitPrice < 0)
                return ApiResponse<ServiceOperationDto>.Fail("Birim fiyat negatif olamaz.");

            var request = new UpdateServiceOperationRequest
            {
                Type = (OperationType)form.Type,
                Description = form.Description.Trim(),
                Quantity = form.Quantity,
                UnitPrice = form.UnitPrice,
                Note = string.IsNullOrWhiteSpace(form.Note)
                    ? null
                    : form.Note.Trim(),
                ServiceRequestItemId = form.ServiceRequestItemId,
                StockItemId = form.StockItemId
            };

            return await _serviceRecordApiService.UpdateOperationAsync(
                form.OperationId,
                request);
        }

        private static void NormalizeQuery(ServiceRecordListQueryViewModel query)
        {
            query.Search = string.IsNullOrWhiteSpace(query.Search)
                ? null
                : query.Search.Trim();

            query.PageNumber = query.PageNumber <= 0
                ? 1
                : query.PageNumber;

            query.PageSize = query.PageSize <= 0
                ? 10
                : query.PageSize;

            query.PageSize = query.PageSize > 100
                ? 100
                : query.PageSize;

            var allowedStatusFilters = new[] { "active", "completed", "cancelled", "all" };

            query.StatusFilter = string.IsNullOrWhiteSpace(query.StatusFilter)
                ? "active"
                : query.StatusFilter.Trim().ToLowerInvariant();

            if (!allowedStatusFilters.Contains(query.StatusFilter))
            {
                query.StatusFilter = "active";
            }

        }



        private static PagedResultViewModel<ServiceRecordListItemViewModel> CreateEmptyPagedResult(
            ServiceRecordListQueryViewModel query)
        {
            return new PagedResultViewModel<ServiceRecordListItemViewModel>
            {
                Items = new List<ServiceRecordListItemViewModel>(),
                PageNumber = query.PageNumber,
                PageSize = query.PageSize,
                TotalCount = 0
            };
        }
    }
}
