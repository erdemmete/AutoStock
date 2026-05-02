namespace AutoStock.Services
{
    public interface IPdfService
    {
        byte[] CreateServicePdf(CreateServicePdfRequest request);
    }
}
