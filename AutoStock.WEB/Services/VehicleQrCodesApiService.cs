using AutoStock.WEB.Models.Common;
using AutoStock.WEB.Models.VehicleQrCodes;
using System.Net;

namespace AutoStock.WEB.Services
{
    public class VehicleQrCodesApiService : BaseApiService
    {
        public VehicleQrCodesApiService(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            IHttpContextAccessor httpContextAccessor,
            ILogger<VehicleQrCodesApiService> logger)
            : base(httpClientFactory, configuration, httpContextAccessor, logger)
        {
        }

        public async Task<ApiResponse<VehicleQrCodeResolveViewModel>> ResolveAsync(string code)
        {
            return await GetAsync<VehicleQrCodeResolveViewModel>(
                $"/api/VehicleQrCodes/resolve?code={WebUtility.UrlEncode(code)}",
                "QR kod çözümlenirken hata oluştu.");
        }
    }
}