using AutoStock.Services.Dtos.Pdfs;

namespace AutoStock.Services.Interfaces
{
    public interface IPdfService
    {
        byte[] CreateServicePdf(CreateServicePdfRequest request);
    }
}
