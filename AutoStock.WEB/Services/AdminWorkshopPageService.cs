using AutoStock.WEB.Models.Admin.Workshops;
using AutoStock.WEB.Models.Common;

namespace AutoStock.WEB.Services
{
    public class AdminWorkshopPageService
    {
        private readonly AdminWorkshopApiService _adminWorkshopApiService;

        public AdminWorkshopPageService(AdminWorkshopApiService adminWorkshopApiService)
        {
            _adminWorkshopApiService = adminWorkshopApiService;
        }

        public async Task<PageViewResult<AdminWorkshopIndexViewModel>> GetIndexPageAsync(
            AdminWorkshopListQueryViewModel query)
        {
            NormalizeQuery(query);

            var result = await _adminWorkshopApiService.GetPagedAsync(query);

            var viewModel = new AdminWorkshopIndexViewModel
            {
                Query = query,
                Workshops = result.Data ?? CreateEmptyPagedResult(query)
            };

            if (result.IsFailure)
            {
                var errors = result.ErrorMessages.Any()
                    ? result.ErrorMessages
                    : new List<string>
                    {
                        result.ErrorMessage ?? "Servis listesi alınırken hata oluştu."
                    };

                return PageViewResult<AdminWorkshopIndexViewModel>.WithErrors(
                    viewModel,
                    errors);
            }

            return PageViewResult<AdminWorkshopIndexViewModel>.Success(viewModel);
        }

        private static void NormalizeQuery(AdminWorkshopListQueryViewModel query)
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
        }

        private static PagedResultViewModel<AdminWorkshopListItemViewModel> CreateEmptyPagedResult(
            AdminWorkshopListQueryViewModel query)
        {
            return new PagedResultViewModel<AdminWorkshopListItemViewModel>
            {
                Items = new List<AdminWorkshopListItemViewModel>(),
                PageNumber = query.PageNumber,
                PageSize = query.PageSize,
                TotalCount = 0
            };
        }
    }
}