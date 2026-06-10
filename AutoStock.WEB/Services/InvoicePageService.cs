using AutoStock.WEB.Models.Common;
using AutoStock.WEB.Models.Invoices;

namespace AutoStock.WEB.Services
{
    public class InvoicePageService
    {
        private readonly InvoiceApiService _invoiceApiService;
        private readonly ServiceRecordApiService _serviceRecordApiService;

        public InvoicePageService(
            InvoiceApiService invoiceApiService,
            ServiceRecordApiService serviceRecordApiService)
        {
            _invoiceApiService = invoiceApiService;
            _serviceRecordApiService = serviceRecordApiService;
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

        public async Task<PageViewResult<InvoiceDetailViewModel>> BuildDetailAsync(int invoiceId)
        {
            var invoiceResult = await _invoiceApiService.GetDetailAsync(invoiceId);

            if (invoiceResult.IsFailure || invoiceResult.Data is null)
            {
                return PageViewResult<InvoiceDetailViewModel>.WithErrors(
                    new InvoiceDetailViewModel(),
                    invoiceResult.ErrorMessages.Any()
                        ? invoiceResult.ErrorMessages
                        : new[] { invoiceResult.ErrorMessage ?? "Fatura detayı alınırken hata oluştu." });
            }

            var model = invoiceResult.Data;

            if (model.Status == 1 && model.ServiceRecordId.HasValue)
            {
                var serviceRecordResult = await _serviceRecordApiService.GetDetailAsync(
                    model.ServiceRecordId.Value);

                if (serviceRecordResult.IsSuccess && serviceRecordResult.Data is not null)
                {
                    model.ServiceRequestItems = serviceRecordResult.Data.RequestItems
                        .Select(x => new InvoiceServiceRequestItemOptionViewModel
                        {
                            Id = x.Id,
                            Title = x.Title
                        })
                        .ToList();
                }
            }

            return PageViewResult<InvoiceDetailViewModel>.Success(model);
        }

        public async Task<ApiResponse<InvoiceNavigationViewModel>> CreateOrGetDraftFromServiceRecordAsync(
            int serviceRecordId)
        {
            return await _invoiceApiService.CreateOrGetDraftFromServiceRecordAsync(
                serviceRecordId);
        }
    }
}