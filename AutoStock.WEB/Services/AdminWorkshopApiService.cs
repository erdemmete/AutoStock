using AutoStock.WEB.Models.Admin.Workshops;
using AutoStock.WEB.Models.Common;

namespace AutoStock.WEB.Services
{
    public class AdminWorkshopApiService : BaseApiService
    {
        public AdminWorkshopApiService(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            IHttpContextAccessor httpContextAccessor,
            ILogger<AdminWorkshopApiService> logger)
            : base(httpClientFactory, configuration, httpContextAccessor, logger)
        {
        }

        public async Task<ApiResponse<List<AdminWorkshopListItemViewModel>>> GetListAsync()
        {
            return await GetAsync<List<AdminWorkshopListItemViewModel>>(
                "/api/admin/workshops",
                "Servis listesi alınırken hata oluştu.");
        }

        public async Task<ApiResponse<AdminWorkshopDetailViewModel>> GetByIdAsync(int id)
        {
            return await GetAsync<AdminWorkshopDetailViewModel>(
                $"/api/admin/workshops/{id}",
                "Servis detayı alınırken hata oluştu.");
        }

        public async Task<ApiResponse<int>> CreateAsync(CreateAdminWorkshopViewModel model)
        {
            return await PostJsonAsync<CreateAdminWorkshopViewModel, int>(
                "/api/admin/workshops",
                model,
                "Servis oluşturulurken hata oluştu.");
        }

        public async Task<ApiResponse<object>> UpdateSubscriptionAsync(UpdateAdminWorkshopSubscriptionViewModel model)
        {
            var requestBody = new
            {
                isActive = model.IsActive,
                subscriptionStatus = model.SubscriptionStatus,
                subscriptionEndDate = model.SubscriptionEndDate,
                subscriptionNote = model.SubscriptionNote
            };

            return await PutJsonAsync<object, object>(
                $"/api/admin/workshops/{model.WorkshopId}/subscription",
                requestBody,
                "Servis abonelik bilgileri güncellenirken hata oluştu.");
        }

        public async Task<ApiResponse<object>> UpdateProfileAsync(UpdateAdminWorkshopProfileViewModel model)
        {
            var requestBody = new
            {
                displayName = model.DisplayName,
                legalTitle = model.LegalTitle,
                taxOffice = model.TaxOffice,
                taxNumber = model.TaxNumber,
                tradeRegistryNumber = model.TradeRegistryNumber,
                mersisNumber = model.MersisNumber,
                email = model.Email,
                phoneNumber = model.PhoneNumber,
                faxNumber = model.FaxNumber,
                website = model.Website,
                addressLine = model.AddressLine,
                city = model.City,
                district = model.District,
                postalCode = model.PostalCode,
                country = model.Country
            };

            return await PutJsonAsync<object, object>(
                $"/api/admin/workshops/{model.WorkshopId}/profile",
                requestBody,
                "Servis profil bilgileri güncellenirken hata oluştu.");
        }

        public async Task<ApiResponse<object>> CreatePartnerAsync(CreateAdminWorkshopPartnerViewModel model)
        {
            var requestBody = new
            {
                fullName = model.FullName,
                title = model.Title,
                phoneNumber = model.PhoneNumber,
                email = model.Email,
                isPrimary = model.IsPrimary,
                note = model.Note
            };

            return await PostJsonAsync<object, object>(
                $"/api/admin/workshops/{model.WorkshopId}/partners",
                requestBody,
                "Servis yetkilisi oluşturulurken hata oluştu.");
        }

        public async Task<ApiResponse<object>> DeletePartnerAsync(int workshopId, int partnerId)
        {
            return await DeleteAsync<object>(
                $"/api/admin/workshops/{workshopId}/partners/{partnerId}",
                "Servis yetkilisi silinirken hata oluştu.");
        }

        public async Task<ApiResponse<AdminWorkshopUserCreatedViewModel>> CreateUserAsync(CreateAdminWorkshopUserViewModel model)
        {
            var requestBody = new
            {
                fullName = model.FullName,
                userName = model.UserName,
                email = model.Email,
                phoneNumber = model.PhoneNumber,
                role = model.Role
            };

            return await PostJsonAsync<object, AdminWorkshopUserCreatedViewModel>(
                $"/api/admin/workshops/{model.WorkshopId}/users",
                requestBody,
                "Servis kullanıcısı oluşturulurken hata oluştu.");
        }

        public async Task<ApiResponse<object>> UpdateUserStatusAsync(int workshopId, int userId, bool isActive)
        {
            var requestBody = new
            {
                isActive
            };

            return await PutJsonAsync<object, object>(
                $"/api/admin/workshops/{workshopId}/users/{userId}/status",
                requestBody,
                "Kullanıcı durumu güncellenirken hata oluştu.");
        }

        public async Task<ApiResponse<AdminWorkshopUserPasswordResetLinkViewModel>> CreateUserPasswordResetLinkAsync(int workshopId, int userId)
        {
            return await PostEmptyAsync<AdminWorkshopUserPasswordResetLinkViewModel>(
                $"/api/admin/workshops/{workshopId}/users/{userId}/password-reset-link",
                "Şifre sıfırlama bağlantısı oluşturulurken hata oluştu.");
        }

        public async Task<ApiResponse<SuggestedAdminWorkshopCredentialsViewModel>> SuggestCredentialsAsync(int workshopId, string fullName)
        {
            var url = BuildUrlWithQuery(
                $"/api/admin/workshops/{workshopId}/users/suggest-credentials",
                new Dictionary<string, string?>
                {
                    ["fullName"] = fullName
                });

            return await GetAsync<SuggestedAdminWorkshopCredentialsViewModel>(
                url,
                "Kullanıcı adı ve geçici şifre oluşturulurken hata oluştu.");
        }

        public async Task<ApiResponse<PagedResultViewModel<AdminWorkshopListItemViewModel>>> GetPagedAsync(AdminWorkshopListQueryViewModel query)
        {
            var url = BuildUrlWithQuery(
                "/api/admin/workshops",
                new Dictionary<string, string?>
                {
                    ["search"] = query.Search,
                    ["isActive"] = query.IsActive?.ToString(),
                    ["subscriptionStatus"] = query.SubscriptionStatus?.ToString(),
                    ["pageNumber"] = query.PageNumber.ToString(),
                    ["pageSize"] = query.PageSize.ToString()
                });

            return await GetAsync<PagedResultViewModel<AdminWorkshopListItemViewModel>>(
                url,
                "Servis listesi alınırken hata oluştu.");
        }
    }
}