using AutoStock.Repositories;
using AutoStock.Repositories.Entities;
using AutoStock.Repositories.Enums;
using AutoStock.Services.Dtos.Common;
using AutoStock.Services.Dtos.ServiceRecords;
using AutoStock.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Net;

namespace AutoStock.Services.Services
{
    public class ServiceRecordImageService : IServiceRecordImageService
    {
        private const long MaxFileSize = 5 * 1024 * 1024;

        private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "image/jpeg",
            "image/png",
            "image/webp"
        };

        private static readonly Dictionary<string, string> ExtensionByContentType = new(StringComparer.OrdinalIgnoreCase)
        {
            ["image/jpeg"] = ".jpg",
            ["image/png"] = ".png",
            ["image/webp"] = ".webp"
        };

        private readonly AppDbContext _context;

        public ServiceRecordImageService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<ServiceResult<List<ServiceRecordImageDto>>> GetByServiceRecordAsync(
            int workshopId,
            int serviceRecordId)
        {
            var serviceRecordExists = await _context.ServiceRecords
                .AsNoTracking()
                .AnyAsync(x => x.Id == serviceRecordId && x.WorkshopId == workshopId);

            if (!serviceRecordExists)
            {
                return ServiceResult<List<ServiceRecordImageDto>>.Fail(
                    "Servis kaydı bulunamadı.",
                    HttpStatusCode.NotFound);
            }

            var images = await _context.ServiceRecordImages
                .AsNoTracking()
                .Where(x => x.ServiceRecordId == serviceRecordId &&
                            x.ServiceRecord.WorkshopId == workshopId)
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => new ServiceRecordImageDto
                {
                    Id = x.Id,
                    ServiceRecordId = x.ServiceRecordId,
                    Type = x.Type,
                    TypeText = GetImageTypeText(x.Type),
                    Description = x.Description,
                    CreatedAt = x.CreatedAt
                })
                .ToListAsync();

            return ServiceResult<List<ServiceRecordImageDto>>.Success(images);
        }

        public async Task<ServiceResult<ServiceRecordImageDto>> UploadAsync(
            int workshopId,
            int serviceRecordId,
            Stream fileStream,
            string originalFileName,
            string contentType,
            long fileLength,
            ServiceImageType type,
            string? description)
        {
            if (fileLength <= 0)
                return ServiceResult<ServiceRecordImageDto>.Fail("Fotoğraf dosyası boş.");

            if (fileLength > MaxFileSize)
                return ServiceResult<ServiceRecordImageDto>.Fail("Fotoğraf en fazla 5 MB olabilir.");

            if (!AllowedContentTypes.Contains(contentType))
                return ServiceResult<ServiceRecordImageDto>.Fail("Sadece JPG, PNG veya WEBP fotoğraf yüklenebilir.");

            var serviceRecordExists = await _context.ServiceRecords
                .AsNoTracking()
                .AnyAsync(x => x.Id == serviceRecordId && x.WorkshopId == workshopId);

            if (!serviceRecordExists)
            {
                return ServiceResult<ServiceRecordImageDto>.Fail(
                    "Servis kaydı bulunamadı.",
                    HttpStatusCode.NotFound);
            }

            var extension = ExtensionByContentType[contentType];
            var safeFileName = $"{Guid.NewGuid():N}{extension}";

            var relativeDirectory = Path.Combine(
                "App_Data",
                "service-record-images",
                workshopId.ToString(),
                serviceRecordId.ToString());

            var fullDirectory = Path.Combine(
                Directory.GetCurrentDirectory(),
                relativeDirectory);

            Directory.CreateDirectory(fullDirectory);

            var fullPath = Path.Combine(fullDirectory, safeFileName);

            await using (var output = new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write))
            {
                await fileStream.CopyToAsync(output);
            }

            var relativePath = Path.Combine(relativeDirectory, safeFileName)
                .Replace("\\", "/");

            var entity = new ServiceRecordImage
            {
                ServiceRecordId = serviceRecordId,
                Type = type,
                FilePath = relativePath,
                Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
                CreatedAt = DateTime.Now
            };

            _context.ServiceRecordImages.Add(entity);
            await _context.SaveChangesAsync();

            var dto = new ServiceRecordImageDto
            {
                Id = entity.Id,
                ServiceRecordId = entity.ServiceRecordId,
                Type = entity.Type,
                TypeText = GetImageTypeText(entity.Type),
                Description = entity.Description,
                CreatedAt = entity.CreatedAt
            };

            return ServiceResult<ServiceRecordImageDto>.Success(dto, HttpStatusCode.Created);
        }

        public async Task<ServiceResult<ServiceRecordImageContentDto>> GetContentAsync(
            int workshopId,
            int imageId)
        {
            var image = await _context.ServiceRecordImages
                .AsNoTracking()
                .Include(x => x.ServiceRecord)
                .FirstOrDefaultAsync(x =>
                    x.Id == imageId &&
                    x.ServiceRecord.WorkshopId == workshopId);

            if (image is null)
            {
                return ServiceResult<ServiceRecordImageContentDto>.Fail(
                    "Fotoğraf bulunamadı.",
                    HttpStatusCode.NotFound);
            }

            var fullPath = Path.Combine(
                Directory.GetCurrentDirectory(),
                image.FilePath.Replace("/", Path.DirectorySeparatorChar.ToString()));

            if (!File.Exists(fullPath))
            {
                return ServiceResult<ServiceRecordImageContentDto>.Fail(
                    "Fotoğraf dosyası bulunamadı.",
                    HttpStatusCode.NotFound);
            }

            var extension = Path.GetExtension(fullPath).ToLowerInvariant();

            var contentType = extension switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".webp" => "image/webp",
                _ => "application/octet-stream"
            };

            var content = await File.ReadAllBytesAsync(fullPath);

            return ServiceResult<ServiceRecordImageContentDto>.Success(new ServiceRecordImageContentDto
            {
                Content = content,
                ContentType = contentType,
                FileName = Path.GetFileName(fullPath)
            });
        }

        public async Task<ServiceResult<bool>> DeleteAsync(int workshopId, int imageId)
        {
            var image = await _context.ServiceRecordImages
                .Include(x => x.ServiceRecord)
                .FirstOrDefaultAsync(x =>
                    x.Id == imageId &&
                    x.ServiceRecord.WorkshopId == workshopId);

            if (image is null)
            {
                return ServiceResult<bool>.Fail(
                    "Fotoğraf bulunamadı.",
                    HttpStatusCode.NotFound);
            }

            var fullPath = Path.Combine(
                Directory.GetCurrentDirectory(),
                image.FilePath.Replace("/", Path.DirectorySeparatorChar.ToString()));

            _context.ServiceRecordImages.Remove(image);
            await _context.SaveChangesAsync();

            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }

            return ServiceResult<bool>.Success(true);
        }

        private static string GetImageTypeText(ServiceImageType type)
        {
            return type switch
            {
                ServiceImageType.BeforeRepair => "Geliş Fotoğrafı",
                ServiceImageType.AfterRepair => "Teslim / Sonrası",
                ServiceImageType.Odometer => "Kilometre",
                ServiceImageType.FuelGauge => "Yakıt Göstergesi",
                ServiceImageType.Damage => "Hasar",
                ServiceImageType.Interior => "İç Mekan",
                ServiceImageType.Other => "Diğer",
                _ => "Fotoğraf"
            };
        }
    }
}