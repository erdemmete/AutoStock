using AutoStock.Repositories;
using AutoStock.Repositories.Entities;
using AutoStock.Services.Dtos.Common;
using AutoStock.Services.Dtos.Vehicles;
using AutoStock.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using System.Text.Json;

namespace AutoStock.Services.Services
{
    public class VehicleCatalogSeeder : IVehicleCatalogSeeder
    {
        private readonly AppDbContext _context;
        private readonly IHostEnvironment _hostEnvironment;

        public VehicleCatalogSeeder(
            AppDbContext context,
            IHostEnvironment hostEnvironment)
        {
            _context = context;
            _hostEnvironment = hostEnvironment;
        }

        public async Task<ServiceResult<VehicleCatalogSeedResultDto>> SeedAsync(CancellationToken cancellationToken = default)
        {
            var filePath = ResolveCatalogFilePath();

            if (filePath is null)
                return ServiceResult<VehicleCatalogSeedResultDto>.Fail("Araç katalog JSON dosyası bulunamadı.");

            var json = await File.ReadAllTextAsync(filePath, cancellationToken);

            var catalog = JsonSerializer.Deserialize<VehicleCatalogJson>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (catalog?.Brands is null || catalog.Brands.Count == 0)
                return ServiceResult<VehicleCatalogSeedResultDto>.Fail("Araç katalog datası boş veya hatalı.");

            var result = new VehicleCatalogSeedResultDto();

            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

            var brands = await _context.VehicleBrands.ToListAsync(cancellationToken);
            var models = await _context.VehicleModels.ToListAsync(cancellationToken);
            var variants = await _context.Set<VehicleVariant>().ToListAsync(cancellationToken);

            foreach (var brandJson in catalog.Brands.Where(x => !string.IsNullOrWhiteSpace(x.Name)))
            {
                var brandName = brandJson.Name.Trim();
                var brandKey = NormalizeKey(brandName);

                var brand = brands.FirstOrDefault(x => NormalizeKey(x.Name) == brandKey);

                if (brand is null)
                {
                    brand = new VehicleBrand
                    {
                        Name = brandName,
                        IsActive = true
                    };

                    _context.VehicleBrands.Add(brand);
                    brands.Add(brand);
                    result.CreatedBrands++;

                    await _context.SaveChangesAsync(cancellationToken);
                }
                else
                {
                    var changed = false;

                    if (brand.Name != brandName)
                    {
                        brand.Name = brandName;
                        changed = true;
                    }

                    if (!brand.IsActive)
                    {
                        brand.IsActive = true;
                        changed = true;
                    }

                    if (changed)
                        result.UpdatedBrands++;
                }

                foreach (var modelJson in brandJson.Models.Where(x => !string.IsNullOrWhiteSpace(x.Name)))
                {
                    var modelName = modelJson.Name.Trim();
                    var modelKey = NormalizeKey(modelName);

                    var model = models.FirstOrDefault(x =>
                        x.VehicleBrandId == brand.Id &&
                        NormalizeKey(x.Name) == modelKey);

                    if (model is null)
                    {
                        model = new VehicleModel
                        {
                            VehicleBrandId = brand.Id,
                            Name = modelName,
                            IsActive = true
                        };

                        _context.VehicleModels.Add(model);
                        models.Add(model);
                        result.CreatedModels++;

                        await _context.SaveChangesAsync(cancellationToken);
                    }
                    else
                    {
                        var changed = false;

                        if (model.Name != modelName)
                        {
                            model.Name = modelName;
                            changed = true;
                        }

                        if (!model.IsActive)
                        {
                            model.IsActive = true;
                            changed = true;
                        }

                        if (changed)
                            result.UpdatedModels++;
                    }

                    foreach (var variantJson in modelJson.Variants.Where(x => !string.IsNullOrWhiteSpace(x.Name)))
                    {
                        var variantName = variantJson.Name.Trim();
                        var variantKey = NormalizeKey(variantName);

                        var variant = variants.FirstOrDefault(x =>
                            x.VehicleModelId == model.Id &&
                            NormalizeKey(x.Name) == variantKey);

                        if (variant is null)
                        {
                            variant = new VehicleVariant
                            {
                                VehicleBrandId = brand.Id,
                                VehicleModelId = model.Id,
                                Name = variantName,
                                IsActive = true
                            };

                            _context.Set<VehicleVariant>().Add(variant);
                            variants.Add(variant);
                            result.CreatedVariants++;
                        }
                        else
                        {
                            result.UpdatedVariants++;
                        }

                        variant.VehicleBrandId = brand.Id;
                        variant.VehicleModelId = model.Id;
                        variant.Name = variantName;
                        variant.FuelType = NormalizeNullable(variantJson.FuelType);
                        variant.TransmissionType = NormalizeNullable(variantJson.TransmissionType);
                        variant.BodyType = NormalizeNullable(variantJson.BodyType ?? modelJson.VehicleType);
                        variant.EngineCapacityCc = variantJson.EngineCapacityCc;
                        variant.EnginePowerHp = variantJson.EnginePowerHp;
                        variant.EngineCode = NormalizeNullable(variantJson.EngineCode);
                        variant.ModelYearFrom = variantJson.ModelYearFrom;
                        variant.ModelYearTo = variantJson.ModelYearTo;
                        variant.SortOrder = variantJson.SortOrder;
                        variant.IsActive = true;
                    }
                }
            }

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            result.TotalBrands = await _context.VehicleBrands.CountAsync(cancellationToken);
            result.TotalModels = await _context.VehicleModels.CountAsync(cancellationToken);
            result.TotalVariants = await _context.Set<VehicleVariant>().CountAsync(cancellationToken);

            return ServiceResult<VehicleCatalogSeedResultDto>.Success(result);
        }

        private string? ResolveCatalogFilePath()
        {
            var candidates = new[]
            {
                Path.Combine(AppContext.BaseDirectory, "Data", "vehicle-catalog.tr.json"),
                Path.Combine(_hostEnvironment.ContentRootPath, "Data", "vehicle-catalog.tr.json"),
                Path.Combine(_hostEnvironment.ContentRootPath, "..", "AutoStock.Repositories", "Data", "vehicle-catalog.tr.json")
            };

            return candidates.FirstOrDefault(File.Exists);
        }

        private static string NormalizeKey(string? value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? string.Empty
                : value.Trim().ToUpperInvariant();
        }

        private static string? NormalizeNullable(string? value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? null
                : value.Trim();
        }

        private sealed class VehicleCatalogJson
        {
            public string? Version { get; set; }

            public List<VehicleCatalogBrandJson> Brands { get; set; } = new();
        }

        private sealed class VehicleCatalogBrandJson
        {
            public string Name { get; set; } = null!;

            public int SortOrder { get; set; }

            public List<VehicleCatalogModelJson> Models { get; set; } = new();
        }

        private sealed class VehicleCatalogModelJson
        {
            public string Name { get; set; } = null!;

            public string? VehicleType { get; set; }

            public int SortOrder { get; set; }

            public List<VehicleCatalogVariantJson> Variants { get; set; } = new();
        }

        private sealed class VehicleCatalogVariantJson
        {
            public string Name { get; set; } = null!;

            public string? FuelType { get; set; }

            public string? TransmissionType { get; set; }

            public string? BodyType { get; set; }

            public int? EngineCapacityCc { get; set; }

            public int? EnginePowerHp { get; set; }

            public string? EngineCode { get; set; }

            public int? ModelYearFrom { get; set; }

            public int? ModelYearTo { get; set; }

            public int SortOrder { get; set; }
        }
    }
}
