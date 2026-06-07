using AutoStock.Services.Constants;
using AutoStock.Services.Dtos.Pdfs;
using AutoStock.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutoStock.API.Controllers
{
    [Authorize(Roles = AppRoles.Owner + "," + AppRoles.Staff)]
    [Route("api/[controller]")]
    [ApiController]
    public class QuickOfferPdfsController : BaseApiController
    {
        private readonly IPdfService _pdfService;
        private readonly IDateTimeProvider _dateTimeProvider;

        public QuickOfferPdfsController(
            IPdfService pdfService,
            IDateTimeProvider dateTimeProvider)
        {
            _pdfService = pdfService;
            _dateTimeProvider = dateTimeProvider;
        }

        [HttpPost]
        public IActionResult Create(CreateQuickOfferPdfRequest request)
        {
            var workshopIdResult = GetCurrentWorkshopId();

            if (workshopIdResult.IsFailure)
                return UnauthorizedResult(workshopIdResult);

            var fileBytes = _pdfService.CreateQuickOfferPdf(request);

            var plate = string.IsNullOrWhiteSpace(request.Plate)
                ? "hizli-teklif"
                : request.Plate.Trim().ToUpperInvariant();

            var dateText = _dateTimeProvider.Now.ToString("yyyyMMdd-HHmm");
            var fileName = $"{plate}-{dateText}-hizli-teklif.pdf";

            return File(fileBytes, "application/pdf", fileName);
        }
    }
}