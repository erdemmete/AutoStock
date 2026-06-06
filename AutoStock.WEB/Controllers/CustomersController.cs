using AutoStock.Repositories.Enums;
using AutoStock.WEB.Models.Customers;
using AutoStock.WEB.Services;
using Microsoft.AspNetCore.Mvc;

namespace AutoStock.WEB.Controllers
{
    public class CustomersController : BaseController
    {
        private readonly CustomerApiService _customerApiService;
        private readonly CustomerPageService _customerPageService;

        public CustomersController(
            CustomerApiService customerApiService,
            CustomerPageService customerPageService)
        {
            _customerApiService = customerApiService;
            _customerPageService = customerPageService;
        }

        [HttpGet("Customers")]
        public async Task<IActionResult> Index([FromQuery] CustomerListQueryViewModel query)
        {
            var pageResult = await _customerPageService.BuildIndexAsync(query);

            if (pageResult.HasErrors)
            {
                ShowErrors(pageResult.ErrorMessages);
            }

            return View(pageResult.ViewModel);
        }

        [HttpGet("Customers/Create")]
        public IActionResult Create()
        {
            return View(new CreateCustomerViewModel());
        }

        [HttpPost("Customers/Create")]
        public async Task<IActionResult> Create(CreateCustomerViewModel model)
        {
            ValidateCreateModel(model);

            if (!ModelState.IsValid)
            {
                ShowError("Lütfen zorunlu alanları kontrol edin.");
                return View(model);
            }

            var result = await _customerApiService.CreateAsync(model);

            return HandleCommandResult(
                result,
                onSuccess: () => RedirectToAction(nameof(Index)),
                onFailure: () => View(model),
                defaultErrorMessage: "Müşteri oluşturulurken hata oluştu.",
                successMessage: "Müşteri başarıyla oluşturuldu.");
        }

        [HttpGet("Customers/Details/{id:int}")]
        public async Task<IActionResult> Details(int id)
        {
            var result = await _customerApiService.GetByIdAsync(id);

            return ViewObjectResult(
                result,
                "Müşteri bilgileri görüntülenirken hata oluştu.",
                onFailure: () => RedirectToAction(nameof(Index)));
        }

        [HttpGet("Customers/Edit/{id:int}")]
        public async Task<IActionResult> Edit(int id)
        {
            var result = await _customerApiService.GetEditModelAsync(id);

            return ViewObjectResult(
                result,
                "Müşteri düzenleme bilgileri alınırken hata oluştu.",
                onFailure: () => RedirectToAction(nameof(Index)));
        }

        [HttpPost("Customers/Edit/{id:int}")]
        public async Task<IActionResult> Edit(int id, EditCustomerViewModel model)
        {
            if (id != model.Id)
            {
                ShowError("Müşteri bilgisi hatalı.");
                return RedirectToAction(nameof(Index));
            }

            ValidateEditModel(model);

            if (!ModelState.IsValid)
            {
                ShowError("Lütfen zorunlu alanları kontrol edin.");
                return View(model);
            }

            var result = await _customerApiService.UpdateAsync(model);

            return HandleCommandResult(
                result,
                onSuccess: () => RedirectToAction(nameof(Details), new { id = model.Id }),
                onFailure: () => View(model),
                defaultErrorMessage: "Müşteri güncellenirken hata oluştu.",
                successMessage: "Müşteri başarıyla güncellendi.");
        }

        [HttpPost("Customers/Passive/{id:int}")]
        public async Task<IActionResult> SetPassive(int id)
        {
            var result = await _customerApiService.SetPassiveAsync(id);

            return HandleCommandResult(
                result,
                onSuccess: () => RedirectToAction(nameof(Index)),
                onFailure: () => RedirectToAction(nameof(Details), new { id }),
                defaultErrorMessage: "Müşteri pasifleştirilirken hata oluştu.",
                successMessage: "Müşteri başarıyla pasifleştirildi.");
        }

        private void ValidateCreateModel(CreateCustomerViewModel model)
        {
            ValidateCustomerFields(
                model.Type,
                model.PhoneNumber,
                model.FullName,
                model.CompanyName);
        }

        private void ValidateEditModel(EditCustomerViewModel model)
        {
            ValidateCustomerFields(
                model.Type,
                model.PhoneNumber,
                model.FullName,
                model.CompanyName);
        }

        private void ValidateCustomerFields(
            CustomerType type,
            string? phoneNumber,
            string? fullName,
            string? companyName)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
            {
                ModelState.AddModelError(nameof(CreateCustomerViewModel.PhoneNumber), "Telefon numarası zorunludur.");
            }

            if (type == CustomerType.Individual && string.IsNullOrWhiteSpace(fullName))
            {
                ModelState.AddModelError(nameof(CreateCustomerViewModel.FullName), "Ad soyad zorunludur.");
            }

            if (type == CustomerType.Corporate && string.IsNullOrWhiteSpace(companyName))
            {
                ModelState.AddModelError(nameof(CreateCustomerViewModel.CompanyName), "Firma adı zorunludur.");
            }
        }
    }
}