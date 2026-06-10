using AutoStock.Services.Dtos.Common;
using AutoStock.Services.Dtos.Customers;
using AutoStock.Services.Dtos.ServiceRecords;
using AutoStock.Services.Dtos.Vehicles;
using AutoStock.Web.Models.ServiceRecords;
using AutoStock.WEB.Models.Common;
using AutoStock.WEB.Models.ServiceRecords;
using AutoStock.WEB.Models.StockItems;

namespace AutoStock.WEB.Services
{
    public class ServiceRecordApiService : BaseApiService
    {
        public ServiceRecordApiService(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            IHttpContextAccessor httpContextAccessor,
            ILogger<ServiceRecordApiService> logger)
            : base(httpClientFactory, configuration, httpContextAccessor, logger)
        {
        }

        public async Task<ApiResponse<PagedResultViewModel<ServiceRecordListItemViewModel>>> GetPagedAsync(
            ServiceRecordListQueryViewModel query)
        {
            var url = BuildUrlWithQuery(
                "/api/ServiceRecords",
                new Dictionary<string, string?>
                {
                    ["search"] = query.Search,
                    ["statusFilter"] = query.StatusFilter,
                    ["createdFrom"] = query.CreatedFrom?.ToString("yyyy-MM-dd"),
                    ["createdTo"] = query.CreatedTo?.ToString("yyyy-MM-dd"),
                    ["pageNumber"] = query.PageNumber.ToString(),
                    ["pageSize"] = query.PageSize.ToString()
                });

            return await GetAsync<PagedResultViewModel<ServiceRecordListItemViewModel>>(
                url,
                "Servis kayıtları alınırken hata oluştu.");
        }

        public async Task<ApiResponse<CreateServiceRecordResponseViewModel>> CreateAsync(
            CreateServiceRecordViewModel model)
        {
            return await PostJsonAsync<CreateServiceRecordViewModel, CreateServiceRecordResponseViewModel>(
                "/api/ServiceRecords",
                model,
                "Servis kaydı oluşturulurken hata oluştu.");
        }

        public async Task<ApiResponse<ServiceRecordDetailViewModel>> GetDetailAsync(int id)
        {
            return await GetAsync<ServiceRecordDetailViewModel>(
                $"/api/ServiceRecords/{id}",
                "Servis kaydı detayı alınırken hata oluştu.");
        }

        public async Task<ApiResponse<List<VehicleBrandViewModel>>> GetBrandsAsync()
        {
            return await GetAsync<List<VehicleBrandViewModel>>(
                "/api/VehicleCatalog/brands",
                "Araç markaları alınırken hata oluştu.");
        }

        public async Task<ApiResponse<List<VehicleModelViewModel>>> GetModelsAsync(int brandId)
        {
            return await GetAsync<List<VehicleModelViewModel>>(
                $"/api/VehicleCatalog/brands/{brandId}/models",
                "Araç modelleri alınırken hata oluştu.");
        }

        public async Task<ApiResponse<List<CustomerSearchDto>>> SearchCustomersAsync(string query)
        {
            var url = BuildUrlWithQuery(
                "/api/Customers/search",
                new Dictionary<string, string?>
                {
                    ["query"] = query
                });

            return await GetAsync<List<CustomerSearchDto>>(
                url,
                "Müşteri araması yapılırken hata oluştu.");
        }

        public async Task<ApiResponse<object>> AssignQrCodeAsync(int vehicleId, string code)
        {
            var request = new
            {
                vehicleId,
                code
            };

            return await PostJsonAsync<object, object>(
                "/api/VehicleQrCodes/assign",
                request,
                "QR kod araca atanırken hata oluştu.");
        }

        public async Task<ApiResponse<object>> UpdateRequestItemAsync(
            UpdateServiceRequestItemViewModel model)
        {
            var request = new
            {
                repairDetail = model.RepairDetail,
                finalAmount = model.FinalAmount,
                isResolved = model.IsResolved
            };

            return await PutJsonAsync<object, object>(
                $"/api/ServiceRecords/request-items/{model.RequestItemId}",
                request,
                "Talep güncellenirken hata oluştu.");
        }

        public async Task<ApiResponse<int>> AddRequestItemAsync(
            CreateServiceRequestItemViewModel model,
            int serviceRecordId)
        {
            var request = new
            {
                title = model.Title,
                note = model.Note,
                estimatedAmount = model.EstimatedAmount
            };

            return await PostJsonAsync<object, int>(
                $"/api/ServiceRecords/{serviceRecordId}/request-items",
                request,
                "Talep eklenirken hata oluştu.");
        }

        public async Task<ApiResponse<ServiceOperationDto>> AddOperationAsync(
            AddServiceOperationViewModel model)
        {
            var request = new
            {
                serviceRequestItemId = model.ServiceRequestItemId,
                type = model.Type,
                description = model.Description,
                quantity = model.Quantity,
                unitPrice = model.UnitPrice,
                note = model.Note,
                stockItemId = model.StockItemId
            };

            return await PostJsonAsync<object, ServiceOperationDto>(
                $"/api/ServiceRecords/{model.ServiceRecordId}/operations",
                request,
                "Operasyon eklenirken hata oluştu.");
        }

        public async Task<ApiResponse<object>> UpdateStatusAsync(
            UpdateServiceRecordStatusViewModel model)
        {
            var request = new
            {
                status = model.Status
            };

            return await PutJsonAsync<object, object>(
                $"/api/ServiceRecords/{model.ServiceRecordId}/status",
                request,
                "Servis durumu güncellenirken hata oluştu.");
        }

        public async Task<ApiResponse<object>> DeleteOperationAsync(int operationId)
        {
            return await DeleteAsync<object>(
                $"/api/ServiceRecords/operations/{operationId}",
                "Operasyon silinirken hata oluştu.");
        }

        public async Task<ApiResponse<object>> DeleteRequestItemAsync(int requestItemId)
        {
            return await DeleteAsync<object>(
                $"/api/ServiceRecords/request-items/{requestItemId}",
                "Talep silinirken hata oluştu.");
        }

        public async Task<ApiResponse<List<StockItemSelectViewModel>>> SearchStockItemsAsync(string q)
        {
            var url = BuildUrlWithQuery(
                "/api/StockItems/search",
                new Dictionary<string, string?>
                {
                    ["q"] = q
                });

            return await GetAsync<List<StockItemSelectViewModel>>(
                url,
                "Stok araması yapılırken hata oluştu.");
        }

        public async Task<ApiResponse<List<StockItemSelectViewModel>>> GetStockSelectListAsync()
        {
            return await GetAsync<List<StockItemSelectViewModel>>(
                "/api/StockItems/select-list",
                "Stok listesi alınırken hata oluştu.");
        }

        public async Task<ApiResponse<RestoreServiceRequestItemResponse>> RestoreRequestItemAsync(int requestItemId)
        {
            return await PutJsonAsync<object, RestoreServiceRequestItemResponse>(
                $"/api/ServiceRecords/request-items/{requestItemId}/restore",
                new { },
                "Talep geri alınırken hata oluştu.");
        }

        public async Task<ApiResponse<object>> UpdateRequestItemAsync(
    int requestItemId,
    UpdateServiceRequestItemRequest request)
        {
            return await PutJsonAsync<UpdateServiceRequestItemRequest, object>(
                $"/api/ServiceRecords/request-items/{requestItemId}",
                request,
                "Şikayet güncellenirken hata oluştu.");
        }

        public async Task<ApiResponse<object>> UpdateOperationAsync(
            int operationId,
            UpdateServiceOperationRequest request)
        {
            return await PutJsonAsync<UpdateServiceOperationRequest, object>(
                $"/api/ServiceRecords/operations/{operationId}",
                request,
                "İşlem güncellenirken hata oluştu.");
        }

        public async Task<ApiResponse<List<VehicleSearchDto>>> SearchVehiclesAsync(string plate)
        {
            var url = BuildUrlWithQuery(
                "/api/ServiceRecords/search-vehicles",
                new Dictionary<string, string?>
                {
                    ["plate"] = plate
                });

            return await GetAsync<List<VehicleSearchDto>>(
                url,
                "Araç/plaka araması yapılırken hata oluştu.");
        }
    }
}