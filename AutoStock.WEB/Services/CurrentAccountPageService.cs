using AutoStock.WEB.Models.Common;
using AutoStock.WEB.Models.CurrentAccounts;

namespace AutoStock.WEB.Services
{
    public class CurrentAccountPageService
    {
        private readonly CurrentAccountApiService _currentAccountApiService;

        public CurrentAccountPageService(CurrentAccountApiService currentAccountApiService)
        {
            _currentAccountApiService = currentAccountApiService;
        }

        public async Task<PageViewResult<CurrentAccountIndexViewModel>> BuildIndexAsync(
            CurrentAccountListQueryViewModel? query)
        {
            query ??= new CurrentAccountListQueryViewModel();
            query.Normalize();

            var summaryResult = await _currentAccountApiService.GetSummaryAsync(query);

            var model = new CurrentAccountIndexViewModel
            {
                Query = query,
                Summary = summaryResult.Data ?? new CurrentAccountPagedSummaryViewModel
                {
                    CustomerBalances = new AutoStock.WEB.Models.Common.PagedResultViewModel<CustomerBalanceSummaryViewModel>
                    {
                        PageNumber = query.PageNumber,
                        PageSize = query.PageSize
                    }
                }
            };

            if (summaryResult.IsFailure)
            {
                return PageViewResult<CurrentAccountIndexViewModel>.WithErrors(
                    model,
                    summaryResult.ErrorMessages.Any()
                        ? summaryResult.ErrorMessages
                        : new[] { summaryResult.ErrorMessage ?? "Cari özet alınırken hata oluştu." });
            }

            return PageViewResult<CurrentAccountIndexViewModel>.Success(model);
        }
    }
}