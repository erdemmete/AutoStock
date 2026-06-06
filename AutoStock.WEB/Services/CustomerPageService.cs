using AutoStock.WEB.Models.Common;
using AutoStock.WEB.Models.Customers;

namespace AutoStock.WEB.Services
{
    public class CustomerPageService
    {
        private readonly CustomerApiService _customerApiService;

        public CustomerPageService(CustomerApiService customerApiService)
        {
            _customerApiService = customerApiService;
        }

        public async Task<PageViewResult<CustomerIndexViewModel>> BuildIndexAsync(
            CustomerListQueryViewModel? query)
        {
            query ??= new CustomerListQueryViewModel();
            query.Normalize();

            var customersResult = await _customerApiService.GetListAsync(query);

            var model = new CustomerIndexViewModel
            {
                Query = query,
                Customers = customersResult.Data ?? new PagedResultViewModel<CustomerListItemViewModel>
                {
                    PageNumber = query.PageNumber,
                    PageSize = query.PageSize
                }
            };

            if (customersResult.IsFailure)
            {
                return PageViewResult<CustomerIndexViewModel>.WithErrors(
                    model,
                    customersResult.ErrorMessages.Any()
                        ? customersResult.ErrorMessages
                        : new[] { customersResult.ErrorMessage ?? "Müşteri listesi alınırken hata oluştu." });
            }

            return PageViewResult<CustomerIndexViewModel>.Success(model);
        }
    }
}