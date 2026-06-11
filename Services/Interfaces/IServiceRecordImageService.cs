using AutoStock.Repositories.Enums;
using AutoStock.Services.Dtos.Common;
using AutoStock.Services.Dtos.ServiceRecords;

namespace AutoStock.Services.Interfaces
{
    public interface IServiceRecordImageService
    {
        Task<ServiceResult<List<ServiceRecordImageDto>>> GetByServiceRecordAsync(
            int workshopId,
            int serviceRecordId);

        Task<ServiceResult<ServiceRecordImageDto>> UploadAsync(
            int workshopId,
            int serviceRecordId,
            Stream fileStream,
            string originalFileName,
            string contentType,
            long fileLength,
            ServiceImageType type,
            string? description);

        Task<ServiceResult<ServiceRecordImageContentDto>> GetContentAsync(
            int workshopId,
            int imageId);

        Task<ServiceResult<bool>> DeleteAsync(int workshopId, int imageId);
    }
}