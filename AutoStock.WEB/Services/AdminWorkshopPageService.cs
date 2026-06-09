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

        public async Task<PageViewResult<CreateAdminWorkshopUserViewModel>> GetCreateUserPageAsync(
    int workshopId)
        {
            var model = new CreateAdminWorkshopUserViewModel
            {
                WorkshopId = workshopId,
                Role = "Staff"
            };

            return await PrepareCreateUserPageAsync(model);
        }

        public async Task<PageViewResult<CreateAdminWorkshopUserViewModel>> PrepareCreateUserPageAsync(
            CreateAdminWorkshopUserViewModel model)
        {
            model.WorkshopId = model.WorkshopId <= 0
                ? 0
                : model.WorkshopId;

            if (string.IsNullOrWhiteSpace(model.Role))
                model.Role = "Staff";

            var result = await _adminWorkshopApiService.GetByIdAsync(model.WorkshopId);

            if (result.IsFailure || result.Data == null)
            {
                model.WorkshopName = "Servis";

                var errors = result.ErrorMessages.Any()
                    ? result.ErrorMessages
                    : new List<string>
                    {
                result.ErrorMessage ?? "Servis bilgisi alınırken hata oluştu."
                    };

                return PageViewResult<CreateAdminWorkshopUserViewModel>.WithErrors(
                    model,
                    errors);
            }

            model.WorkshopName = result.Data.Name;

            return PageViewResult<CreateAdminWorkshopUserViewModel>.Success(model);
        }

        public async Task<ApiResponse<AdminWorkshopUserCreatedViewModel>> CreateUserAsync(
            int workshopId,
            CreateAdminWorkshopUserViewModel model)
        {
            model.WorkshopId = workshopId;

            return await _adminWorkshopApiService.CreateUserAsync(model);
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