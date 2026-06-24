using AutoStock.Repositories.Enums;
using AutoStock.Services.Dtos.Common;
using AutoStock.Services.Dtos.Customers;
using AutoStock.Services.Dtos.ServiceRecords;
using AutoStock.Services.Dtos.VehicleQrCodes;
using AutoStock.Services.Dtos.Vehicles;
using AutoStock.Web.Models.ServiceRecords;
using AutoStock.WEB.Models.Common;
using AutoStock.WEB.Models.ServiceRecords;
using AutoStock.WEB.Models.StockItems;
using System.Net.Http.Headers;

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

        public async Task<ApiResponse<VehicleQrCodeActionResultDto>> CreateVehicleQrCodeAsync(int vehicleId)
        {
            return await PostJsonAsync<object, VehicleQrCodeActionResultDto>(
                $"/api/VehicleQrCodes/vehicles/{vehicleId}/create",
                new { },
                "Araç QR'ı oluşturulurken hata oluştu.");
        }

        public async Task<ApiResponse<VehicleQrCodeActionResultDto>> EnsureVehicleQrCodeAsync(int vehicleId)
        {
            return await PostJsonAsync<object, VehicleQrCodeActionResultDto>(
                $"/api/VehicleQrCodes/vehicles/{vehicleId}/ensure",
                new { },
                "Güvenli belge bağlantısı hazırlanırken hata oluştu.");
        }

        public async Task<(bool Success, byte[] Content, string ContentType, string? ErrorMessage)> DownloadVehicleQrPngAsync(
            int vehicleId,
            string publicBaseUrl)
        {
            try
            {
                var url = BuildUrlWithQuery(
                    $"/api/VehicleQrCodes/vehicles/{vehicleId}/png",
                    new Dictionary<string, string?>
                    {
                        ["publicBaseUrl"] = publicBaseUrl
                    });

                var client = CreateApiClient();
                using var response = await client.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    return (false, Array.Empty<byte>(), "image/png", string.IsNullOrWhiteSpace(error)
                        ? "QR görseli indirilemedi."
                        : error);
                }

                var bytes = await response.Content.ReadAsByteArrayAsync();
                var contentType = response.Content.Headers.ContentType?.MediaType ?? "image/png";

                return (true, bytes, contentType, null);
            }
            catch
            {
                return (false, Array.Empty<byte>(), "image/png", "QR görseli indirilemedi.");
            }
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

        public async Task<ApiResponse<DeleteServiceOperationResponse>> DeleteOperationAsync(int operationId)
        {
            return await DeleteAsync<DeleteServiceOperationResponse>(
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

        public async Task<ApiResponse<ServiceOperationDto>> UpdateOperationAsync(
            int operationId,
            UpdateServiceOperationRequest request)
        {
            return await PutJsonAsync<UpdateServiceOperationRequest, ServiceOperationDto>(
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

        public async Task<ApiResponse<VehicleSearchDto>> GetVehiclePrefillAsync(int vehicleId)
        {
            return await GetAsync<VehicleSearchDto>(
                $"/api/ServiceRecords/vehicles/{vehicleId}/prefill",
                "Araç bilgisi alınırken hata oluştu.");
        }

        public async Task<ApiResponse<ServiceRecordCreateWorkshopInfoDto>> GetCreateWorkshopInfoAsync()
        {
            return await GetAsync<ServiceRecordCreateWorkshopInfoDto>(
                "/api/ServiceRecords/create-workshop-info",
                "Servis bilgileri alınırken hata oluştu.");
        }

        public async Task<ApiResponse<ServiceRecordImageDto>> UploadImageAsync(
    int serviceRecordId,
    IFormFile file,
    ServiceImageType type,
    string? description)
        {
            using var content = new MultipartFormDataContent();

            await using var fileStream = file.OpenReadStream();

            var fileContent = new StreamContent(fileStream);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);

            content.Add(fileContent, "file", file.FileName);
            content.Add(new StringContent(((int)type).ToString()), "type");

            if (!string.IsNullOrWhiteSpace(description))
            {
                content.Add(new StringContent(description), "description");
            }

            return await SendAsync<ServiceRecordImageDto>(
    $"/api/service-record-images/{serviceRecordId}",
    client => client.PostAsync($"/api/service-record-images/{serviceRecordId}", content),
    "Fotoğraf yüklenirken hata oluştu.");
        }

        public async Task<(bool Success, byte[] Content, string ContentType, string? ErrorMessage)> GetImageContentAsync(
            int imageId)
        {
            try
            {
                var client = CreateApiClient();

                using var response = await client.GetAsync($"/api/service-record-images/{imageId}/content");

                if (!response.IsSuccessStatusCode)
                {
                    return (false, Array.Empty<byte>(), "application/octet-stream", "Fotoğraf alınamadı.");
                }

                var bytes = await response.Content.ReadAsByteArrayAsync();
                var contentType = response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";

                return (true, bytes, contentType, null);
            }
            catch
            {
                return (false, Array.Empty<byte>(), "application/octet-stream", "Fotoğraf alınamadı.");
            }
        }

        public async Task<ApiResponse<bool>> DeleteImageAsync(int imageId)
        {
            return await SendAsync<bool>(
                $"/api/service-record-images/{imageId}",
                client => client.DeleteAsync($"/api/service-record-images/{imageId}"),
                "Fotoğraf silinirken hata oluştu.");
        }

        public async Task<ApiResponse<List<VehicleVariantViewModel>>> GetVariantsAsync(int modelId)
        {
            return await GetAsync<List<VehicleVariantViewModel>>(
                $"/api/VehicleCatalog/models/{modelId}/variants",
                "Araç versiyonları alınırken hata oluştu.");
        }
    }
}
