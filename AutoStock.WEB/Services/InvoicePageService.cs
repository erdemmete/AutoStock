using AutoStock.WEB.Models.Common;
using AutoStock.WEB.Models.Invoices;

namespace AutoStock.WEB.Services
{
    public class InvoicePageService
    {
        private readonly InvoiceApiService _invoiceApiService;

        public InvoicePageService(InvoiceApiService invoiceApiService)
        {
            _invoiceApiService = invoiceApiService;
        }

        public async Task<PageViewResult<InvoiceIndexViewModel>> BuildIndexAsync(
            InvoiceListQueryViewModel? query)
        {
            query ??= new InvoiceListQueryViewModel();
            query.Normalize();

            var invoicesResult = await _invoiceApiService.GetListAsync(query);

            var model = new InvoiceIndexViewModel
            {
                Query = query,
                Invoices = invoicesResult.Data ?? new PagedResultViewModel<InvoiceListItemViewModel>
                {
                    PageNumber = query.PageNumber,
                    PageSize = query.PageSize
                }
            };

            if (invoicesResult.IsFailure)
            {
                return PageViewResult<InvoiceIndexViewModel>.WithErrors(
                    model,
                    invoicesResult.ErrorMessages.Any()
                        ? invoicesResult.ErrorMessages
                        : new[] { invoicesResult.ErrorMessage ?? "Fatura listesi alınırken hata oluştu." });
            }

            return PageViewResult<InvoiceIndexViewModel>.Success(model);
        }
    }
}