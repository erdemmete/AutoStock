using AutoStock.Web.Models.ServiceRecords;
using AutoStock.WEB.Models.Common;

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