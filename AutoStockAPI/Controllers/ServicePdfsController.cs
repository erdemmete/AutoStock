using AutoStock.Services;
using Microsoft.AspNetCore.Mvc;

namespace AutoStock.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ServicePdfsController : ControllerBase
    {
        private readonly IPdfService pdfService;

        public ServicePdfsController(IPdfService pdfService)
        {
            this.pdfService = pdfService;
        }

        [HttpPost]
        public IActionResult Create(CreateServicePdfRequest request)
        {
            var fileBytes = pdfService.CreateServicePdf(request);

            return File(fileBytes, "application/pdf", "servis-fisi.pdf");
        }
    }
}
