using AutoStock.Repositories;
using AutoStock.Repositories.Entities;
using AutoStock.Repositories.Enums;
using AutoStock.Services.Constants;
using AutoStock.Services.Dtos.Admin.Workshops;
using AutoStock.Services.Dtos.AuditLogs;
using AutoStock.Services.Dtos.Common;
using AutoStock.Services.Dtos.SecurityTokens;
using AutoStock.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace AutoStock.Services.Services
{
    public class AdminWorkshopService : IAdminWorkshopService
    {
        private readonly AppDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<IdentityRole<int>> _roleManager;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly IAuditLogService _auditLogService;
        private readonly IUserSecurityTokenService _userSecurityTokenService;

        public AdminWorkshopService(
    AppDbContext context,
    UserManager<AppUser> userManager,
    RoleManager<IdentityRole<int>> roleManager,
    IDateTimeProvider dateTimeProvider,
    IAuditLogService auditLogService,
    IUserSecurityTokenService userSecurityTokenService)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
            _dateTimeProvider = dateTimeProvider;
            _auditLogService = auditLogService;
            _userSecurityTokenService = userSecurityTokenService;
        }

        public async Task<ServiceResult<List<AdminWorkshopListItemDto>>> GetListAsync()
        {
            var now = _dateTimeProvider.Now;

            var workshops = await _context.Workshops
                .AsNoTracking()
                .Select(x => new AdminWorkshopListItemDto
                {
                    Id = x.Id,
                    Name = x.Name,
                    IsActive = x.IsActive,
                    SubscriptionStatus = x.SubscriptionStatus,
                    SubscriptionStartDate = x.SubscriptionStartDate,
                    SubscriptionEndDate = x.SubscriptionEndDate,
                    IsExpired = x.SubscriptionEndDate.HasValue &&
                                x.SubscriptionEndDate.Value <= now,
                    CreatedAt = x.CreatedAt,
                    UserCount = _context.WorkshopUsers.Count(u => u.WorkshopId == x.Id)
                })
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();

            return ServiceResult<List<AdminWorkshopListItemDto>>.Success(workshops);
        }

        public async Task<ServiceResult<AdminWorkshopDetailDto>> GetByIdAsync(int id)
        {
            var workshop = await _context.Workshops
                .AsNoTracking()
                .Where(x => x.Id == id)
                .Select(x => new AdminWorkshopDetailDto
                {
                    Id = x.Id,
                    Name = x.Name,
                    IsActive = x.IsActive,
                    SubscriptionStatus = x.SubscriptionStatus,
                    SubscriptionStartDate = x.SubscriptionStartDate,
                    SubscriptionEndDate = x.SubscriptionEndDate,
                    SubscriptionNote = x.SubscriptionNote,
                    CreatedAt = x.CreatedAt,

                    Profile = x.Profile == null
                        ? null
                        : new AdminWorkshopProfileDto
                        {
                            Id = x.Profile.Id,
                            WorkshopId = x.Profile.WorkshopId,

                            DisplayName = x.Profile.DisplayName,
                            LegalTitle = x.Profile.LegalTitle,

                            TaxOffice = x.Profile.TaxOffice,
                            TaxNumber = x.Profile.TaxNumber,

                            TradeRegistryNumber = x.Profile.TradeRegistryNumber,
                            MersisNumber = x.Profile.MersisNumber,

                            Email = x.Profile.Email,
                            PhoneNumber = x.Profile.PhoneNumber,
                            FaxNumber = x.Profile.FaxNumber,
                            Website = x.Profile.Website,

                            AddressLine = x.Profile.AddressLine,
                            City = x.Profile.City,
                            District = x.Profile.District,
                            PostalCode = x.Profile.PostalCode,
                            Country = x.Profile.Country
                        },
                    Partners = x.Partners
                        .Select(p => new AdminWorkshopPartnerDto
                        {
                            Id = p.Id,
                            WorkshopId = p.WorkshopId,
                            FullName = p.FullName,
                            Title = p.Title,
                            PhoneNumber = p.PhoneNumber,
                            Email = p.Email,
                            IsPrimary = p.IsPrimary,
                            Note = p.Note,
                            CreatedAt = p.CreatedAt
                        })
                        .OrderByDescending(p => p.IsPrimary)
                        .ThenBy(p => p.FullName)
                        .ToList(),
                    Users = x.WorkshopUsers
                        .Select(u => new AdminWorkshopUserDto
                        {
                            UserId = u.UserId,
                            FullName = u.User.FullName,
                            UserName = u.User.UserName!,
                            Email = u.User.Email,
                            Role = u.Role,
                            IsActive = u.User.IsActive,
                            CreatedAt = u.CreatedAt
                        })
                        .OrderBy(u => u.FullName)
                        .ToList()
                })
                .FirstOrDefaultAsync();

            if (workshop == null)
                return ServiceResult<AdminWorkshopDetailDto>.Fail(
                    "Servis bulunamadı.");

            return ServiceResult<AdminWorkshopDetailDto>.Success(workshop);
        }

        public async Task<ServiceResult<int>> CreateAsync(CreateAdminWorkshopRequestDto request)
        {
            var validationResult = await ValidateCreateRequestAsync(request);

            if (validationResult.IsFailure)
                return validationResult;

            var role = request.FirstUserRole.Trim();

            var subscriptionStartDate = _dateTimeProvider.Now;

            DateTime? subscriptionEndDate = request.SubscriptionEndDate;

            if (request.TrialDays.HasValue && request.TrialDays.Value > 0)
            {
                subscriptionEndDate = subscriptionStartDate.AddDays(request.TrialDays.Value);
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();

            var workshop = new Workshop
            {
                Name = request.WorkshopName.Trim(),
                IsActive = request.IsActive,
                SubscriptionStatus = request.SubscriptionStatus,
                SubscriptionStartDate = subscriptionStartDate,
                SubscriptionEndDate = subscriptionEndDate,
                SubscriptionNote = request.SubscriptionNote?.Trim()
            };

            _context.Workshops.Add(workshop);

            await _context.SaveChangesAsync();

            var workshopProfile = new WorkshopProfile
            {
                WorkshopId = workshop.Id,
                DisplayName = workshop.Name,
                Country = "Türkiye"
            };

            _context.WorkshopProfiles.Add(workshopProfile);

            await _context.SaveChangesAsync();

            var user = new AppUser
            {
                FullName = request.FirstUserFullName.Trim(),
                UserName = request.FirstUserName.Trim(),
                Email = string.IsNullOrWhiteSpace(request.FirstUserEmail)
                    ? null
                    : request.FirstUserEmail.Trim(),
                IsActive = true
            };

            var createUserResult = await _userManager.CreateAsync(user, request.FirstUserPassword);

            if (!createUserResult.Succeeded)
            {
                await transaction.RollbackAsync();

                var errors = createUserResult.Errors
                    .Select(x => x.Description)
                    .ToList();

                return ServiceResult<int>.Fail(errors);
            }

            var addRoleResult = await _userManager.AddToRoleAsync(user, role);

            if (!addRoleResult.Succeeded)
            {
                await transaction.RollbackAsync();

                var errors = addRoleResult.Errors
                    .Select(x => x.Description)
                    .ToList();

                return ServiceResult<int>.Fail(errors);
            }

            var workshopUser = new WorkshopUser
            {
                WorkshopId = workshop.Id,
                UserId = user.Id,
                Role = role
            };

            _context.WorkshopUsers.Add(workshopUser);

            await _context.SaveChangesAsync();

            await _auditLogService.AddAsync(new AuditLogCreateDto
            {
                WorkshopId = workshop.Id,
                ActionType = AuditActionType.Create,
                EntityType = AuditEntityType.Workshop,
                EntityId = workshop.Id,
                Description = $"Servis oluşturuldu: {workshop.Name}",
                NewValues = new
                {
                    workshop.Id,
                    workshop.Name,
                    workshop.IsActive,
                    workshop.SubscriptionStatus,
                    workshop.SubscriptionStartDate,
                    workshop.SubscriptionEndDate
                }
            });

            await _auditLogService.AddAsync(new AuditLogCreateDto
            {
                WorkshopId = workshop.Id,
                ActionType = AuditActionType.Create,
                EntityType = AuditEntityType.WorkshopUser,
                EntityId = user.Id,
                Description = $"İlk servis kullanıcısı oluşturuldu: {user.FullName} / {role}",
                NewValues = new
                {
                    WorkshopId = workshop.Id,
                    UserId = user.Id,
                    user.FullName,
                    user.UserName,
                    Role = role,
                    user.IsActive
                }
            });

            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            return ServiceResult<int>.Success(workshop.Id);
        }

        public async Task<ServiceResult<bool>> UpdateSubscriptionAsync(int id, UpdateAdminWorkshopSubscriptionRequestDto request)
        {
            var workshop = await _context.Workshops
                .FirstOrDefaultAsync(x => x.Id == id);

            if (workshop == null)
                return ServiceResult<bool>.Fail(
                    "Servis bulunamadı.");

            if (request.SubscriptionEndDate.HasValue &&
                request.SubscriptionEndDate.Value <= _dateTimeProvider.Now)
            {
                return ServiceResult<bool>.Fail("Üyelik bitiş tarihi geçmişte olamaz.");
            }

            var oldValues = new
            {
                workshop.IsActive,
                workshop.SubscriptionStatus,
                workshop.SubscriptionEndDate,
                workshop.SubscriptionNote
            };

            workshop.IsActive = request.IsActive;
            workshop.SubscriptionStatus = request.SubscriptionStatus;
            workshop.SubscriptionEndDate = request.SubscriptionEndDate;
            workshop.SubscriptionNote = request.SubscriptionNote?.Trim();

            await _auditLogService.AddAsync(new AuditLogCreateDto
            {
                WorkshopId = workshop.Id,
                ActionType = AuditActionType.Update,
                EntityType = AuditEntityType.WorkshopSubscription,
                EntityId = workshop.Id,
                Description = $"Servis üyelik bilgisi güncellendi: {workshop.Name}",
                OldValues = oldValues,
                NewValues = new
                {
                    workshop.IsActive,
                    workshop.SubscriptionStatus,
                    workshop.SubscriptionEndDate,
                    workshop.SubscriptionNote
                }
            });

            await _context.SaveChangesAsync();

            return ServiceResult<bool>.Success(true);
        }

        public async Task<ServiceResult<List<AdminWorkshopUserDto>>> GetUsersAsync(int workshopId)
        {
            var workshopExists = await _context.Workshops
                .AnyAsync(x => x.Id == workshopId);

            if (!workshopExists)
                return ServiceResult<List<AdminWorkshopUserDto>>.Fail(
                    "Servis bulunamadı.");

            var users = await _context.WorkshopUsers
                .AsNoTracking()
                .Where(x => x.WorkshopId == workshopId)
                .Select(x => new AdminWorkshopUserDto
                {
                    UserId = x.UserId,
                    FullName = x.User.FullName,
                    UserName = x.User.UserName!,
                    Email = x.User.Email,
                    Role = x.Role,
                    IsActive = x.User.IsActive,
                    CreatedAt = x.CreatedAt
                })
                .OrderBy(x => x.FullName)
                .ToListAsync();

            return ServiceResult<List<AdminWorkshopUserDto>>.Success(users);
        }

        public async Task<ServiceResult<AdminWorkshopUserCreatedDto>> CreateUserAsync(
    int workshopId,
    CreateAdminWorkshopUserRequestDto request)
        {
            var workshopExists = await _context.Workshops
                .AnyAsync(x => x.Id == workshopId);

            if (!workshopExists)
                return ServiceResult<AdminWorkshopUserCreatedDto>.Fail(
                    "Servis bulunamadı.");

            var validationResult = await ValidateCreateUserRequestAsync(request);
            if (validationResult.IsFailure)
            {
                return ServiceResult<AdminWorkshopUserCreatedDto>.Fail(
                    validationResult.ErrorMessages,
                    (HttpStatusCode)validationResult.StatusCode);
            }

            var role = request.Role.Trim();

            await using var transaction = await _context.Database.BeginTransactionAsync();

            var user = new AppUser
            {
                FullName = request.FullName.Trim(),
                UserName = request.UserName.Trim(),
                Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim(),
                PhoneNumber = string.IsNullOrWhiteSpace(request.PhoneNumber) ? null : request.PhoneNumber.Trim(),
                IsActive = true,
                CreatedAt = _dateTimeProvider.Now
            };

            var createUserResult = await _userManager.CreateAsync(user);

            if (!createUserResult.Succeeded)
            {
                await transaction.RollbackAsync();

                var errors = createUserResult.Errors
                    .Select(x => x.Description)
                    .ToList();

                return ServiceResult<AdminWorkshopUserCreatedDto>.Fail(errors);
            }

            var addRoleResult = await _userManager.AddToRoleAsync(user, role);

            if (!addRoleResult.Succeeded)
            {
                await transaction.RollbackAsync();

                var errors = addRoleResult.Errors
                    .Select(x => x.Description)
                    .ToList();

                return ServiceResult<AdminWorkshopUserCreatedDto>.Fail(errors);
            }

            var workshopUser = new WorkshopUser
            {
                WorkshopId = workshopId,
                UserId = user.Id,
                Role = role
            };

            _context.WorkshopUsers.Add(workshopUser);

            await _context.SaveChangesAsync();

            var tokenResult = await _userSecurityTokenService.CreateAsync(
                new CreateUserSecurityTokenRequestDto
                {
                    UserId = user.Id,
                    Purpose = UserSecurityTokenPurpose.PasswordSetup,
                    DeliveryChannel = UserSecurityTokenDeliveryChannel.Manual,
                    ValidFor = TimeSpan.FromDays(7)
                });

            if (tokenResult.IsFailure)
            {
                await transaction.RollbackAsync();

                return ServiceResult<AdminWorkshopUserCreatedDto>.Fail(
    tokenResult.ErrorMessages,
    (HttpStatusCode)tokenResult.StatusCode);
            }

            await _auditLogService.AddAsync(new AuditLogCreateDto
            {
                WorkshopId = workshopId,
                ActionType = AuditActionType.Create,
                EntityType = AuditEntityType.WorkshopUser,
                EntityId = user.Id,
                Description = $"Servis kullanıcısı oluşturuldu: {user.FullName} / {role}",
                NewValues = new
                {
                    WorkshopId = workshopId,
                    UserId = user.Id,
                    user.FullName,
                    user.UserName,
                    Role = role,
                    user.Email,
                    user.PhoneNumber,
                    user.IsActive,
                    PasswordSetupLinkGenerated = true
                }
            });

            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            var response = new AdminWorkshopUserCreatedDto
            {
                UserId = user.Id,
                WorkshopId = workshopId,
                FullName = user.FullName,
                UserName = user.UserName!,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Role = role,
                PasswordSetupToken = tokenResult.Data!.Token,
                PasswordSetupCode = tokenResult.Data.Code,
                PasswordSetupExpiresAt = tokenResult.Data.ExpiresAt
            };

            return ServiceResult<AdminWorkshopUserCreatedDto>.Success(response);
        }

        public async Task<ServiceResult<bool>> UpdateUserStatusAsync(int workshopId, int userId, UpdateAdminWorkshopUserStatusRequestDto request)
        {
            var workshopUser = await _context.WorkshopUsers
                .Include(x => x.User)
                .FirstOrDefaultAsync(x =>
                    x.WorkshopId == workshopId &&
                    x.UserId == userId);

            if (workshopUser == null)
                return ServiceResult<bool>.Fail(
                    "Kullanıcı bu serviste bulunamadı.");

            var oldIsActive = workshopUser.User.IsActive;

            if (oldIsActive == request.IsActive)
                return ServiceResult<bool>.Success(true);

            workshopUser.User.IsActive = request.IsActive;

            await _auditLogService.AddAsync(new AuditLogCreateDto
            {
                WorkshopId = workshopId,
                ActionType = request.IsActive
                    ? AuditActionType.SetActive
                    : AuditActionType.SetPassive,
                EntityType = AuditEntityType.WorkshopUser,
                EntityId = userId,
                Description = request.IsActive
                    ? $"Servis kullanıcısı aktife alındı: {workshopUser.User.FullName}"
                    : $"Servis kullanıcısı pasife alındı: {workshopUser.User.FullName}",
                OldValues = new
                {
                    WorkshopId = workshopId,
                    UserId = userId,
                    IsActive = oldIsActive
                },
                NewValues = new
                {
                    WorkshopId = workshopId,
                    UserId = userId,
                    workshopUser.User.FullName,
                    workshopUser.User.UserName,
                    IsActive = workshopUser.User.IsActive
                }
            });

            await _context.SaveChangesAsync();

            return ServiceResult<bool>.Success(true);
        }

        public async Task<ServiceResult<AdminWorkshopUserPasswordResetLinkDto>> CreateUserPasswordResetLinkAsync(
    int workshopId,
    int userId)
        {
            var workshopUser = await _context.WorkshopUsers
                .Include(x => x.User)
                .FirstOrDefaultAsync(x =>
                    x.WorkshopId == workshopId &&
                    x.UserId == userId);

            if (workshopUser == null)
            {
                return ServiceResult<AdminWorkshopUserPasswordResetLinkDto>.Fail(
                    "Kullanıcı bu serviste bulunamadı.");
            }

            if (workshopUser.Role != AppRoles.Owner && workshopUser.Role != AppRoles.Staff)
            {
                return ServiceResult<AdminWorkshopUserPasswordResetLinkDto>.Fail(
                    "Sadece Owner veya Staff kullanıcıları için şifre sıfırlama bağlantısı oluşturulabilir.");
            }

            if (!workshopUser.User.IsActive)
            {
                return ServiceResult<AdminWorkshopUserPasswordResetLinkDto>.Fail(
                    "Pasif kullanıcı için şifre sıfırlama bağlantısı oluşturulamaz.");
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();

            await _userSecurityTokenService.RevokeActiveTokensAsync(
                userId,
                UserSecurityTokenPurpose.PasswordSetup);

            var tokenResult = await _userSecurityTokenService.CreateAsync(
                new CreateUserSecurityTokenRequestDto
                {
                    UserId = userId,
                    Purpose = UserSecurityTokenPurpose.PasswordReset,
                    DeliveryChannel = UserSecurityTokenDeliveryChannel.Manual,
                    ValidFor = TimeSpan.FromHours(24)
                });

            if (tokenResult.IsFailure)
            {
                await transaction.RollbackAsync();

                return ServiceResult<AdminWorkshopUserPasswordResetLinkDto>.Fail(
    tokenResult.ErrorMessages,
    (HttpStatusCode)tokenResult.StatusCode);
            }

            workshopUser.User.LastPasswordResetAt = _dateTimeProvider.Now;

            await _auditLogService.AddAsync(new AuditLogCreateDto
            {
                WorkshopId = workshopId,
                ActionType = AuditActionType.Update,
                EntityType = AuditEntityType.WorkshopUser,
                EntityId = userId,
                Description = "Servis kullanıcısı şifre sıfırlama bağlantısı oluşturuldu",
                NewValues = new
                {
                    WorkshopId = workshopId,
                    UserId = userId,
                    workshopUser.User.FullName,
                    workshopUser.User.UserName,
                    PasswordResetLinkGenerated = true,
                    workshopUser.User.LastPasswordResetAt
                }
            });

            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            var response = new AdminWorkshopUserPasswordResetLinkDto
            {
                UserId = workshopUser.User.Id,
                WorkshopId = workshopId,
                FullName = workshopUser.User.FullName,
                UserName = workshopUser.User.UserName!,
                Email = workshopUser.User.Email,
                PhoneNumber = workshopUser.User.PhoneNumber,
                PasswordResetToken = tokenResult.Data!.Token,
                PasswordResetCode = tokenResult.Data.Code,
                PasswordResetExpiresAt = tokenResult.Data.ExpiresAt
            };

            return ServiceResult<AdminWorkshopUserPasswordResetLinkDto>.Success(response);
        }

        public async Task<ServiceResult<bool>> UpdateProfileAsync(int id, UpdateAdminWorkshopProfileRequestDto request)
        {
            var workshop = await _context.Workshops
                .Include(x => x.Profile)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (workshop == null)
                return ServiceResult<bool>.Fail("Servis bulunamadı.");

            var oldValues = workshop.Profile == null
                ? null
                : GetWorkshopProfileAuditValues(workshop.Profile);

            if (workshop.Profile == null)
            {
                workshop.Profile = new WorkshopProfile
                {
                    WorkshopId = workshop.Id,
                    DisplayName = workshop.Name,
                    Country = "Türkiye"
                };
            }

            workshop.Profile.DisplayName = request.DisplayName?.Trim();
            workshop.Profile.LegalTitle = request.LegalTitle?.Trim();
            workshop.Profile.TaxOffice = request.TaxOffice?.Trim();
            workshop.Profile.TaxNumber = request.TaxNumber?.Trim();
            workshop.Profile.TradeRegistryNumber = request.TradeRegistryNumber?.Trim();
            workshop.Profile.MersisNumber = request.MersisNumber?.Trim();

            workshop.Profile.Email = request.Email?.Trim();
            workshop.Profile.PhoneNumber = request.PhoneNumber?.Trim();
            workshop.Profile.FaxNumber = request.FaxNumber?.Trim();
            workshop.Profile.Website = request.Website?.Trim();

            workshop.Profile.AddressLine = request.AddressLine?.Trim();
            workshop.Profile.City = request.City?.Trim();
            workshop.Profile.District = request.District?.Trim();
            workshop.Profile.PostalCode = request.PostalCode?.Trim();
            workshop.Profile.Country = string.IsNullOrWhiteSpace(request.Country)
                ? "Türkiye"
                : request.Country.Trim();

            workshop.Profile.UpdatedAt = _dateTimeProvider.Now;

            await _auditLogService.AddAsync(new AuditLogCreateDto
            {
                WorkshopId = workshop.Id,
                ActionType = AuditActionType.Update,
                EntityType = AuditEntityType.Workshop,
                EntityId = workshop.Id,
                Description = $"Servis profil bilgileri güncellendi: {workshop.Name}",
                OldValues = oldValues,
                NewValues = GetWorkshopProfileAuditValues(workshop.Profile)
            });

            await _context.SaveChangesAsync();

            return ServiceResult<bool>.Success(true);
        }
        public async Task<ServiceResult<List<AdminWorkshopPartnerDto>>> GetPartnersAsync(int workshopId)
        {
            var workshopExists = await _context.Workshops
                .AnyAsync(x => x.Id == workshopId);

            if (!workshopExists)
                return ServiceResult<List<AdminWorkshopPartnerDto>>.Fail(
                    "Servis bulunamadı.");

            var partners = await _context.WorkshopPartners
                .AsNoTracking()
                .Where(x => x.WorkshopId == workshopId)
                .Select(x => new AdminWorkshopPartnerDto
                {
                    Id = x.Id,
                    WorkshopId = x.WorkshopId,
                    FullName = x.FullName,
                    Title = x.Title,
                    PhoneNumber = x.PhoneNumber,
                    Email = x.Email,
                    IsPrimary = x.IsPrimary,
                    Note = x.Note,
                    CreatedAt = x.CreatedAt
                })
                .OrderByDescending(x => x.IsPrimary)
                .ThenBy(x => x.FullName)
                .ToListAsync();

            return ServiceResult<List<AdminWorkshopPartnerDto>>.Success(partners);
        }

        public async Task<ServiceResult<int>> CreatePartnerAsync(int workshopId, CreateAdminWorkshopPartnerRequestDto request)
        {
            var workshopExists = await _context.Workshops
                .AnyAsync(x => x.Id == workshopId);

            if (!workshopExists)
                return ServiceResult<int>.Fail(
                    "Servis bulunamadı.");

            if (string.IsNullOrWhiteSpace(request.FullName))
                return ServiceResult<int>.Fail("Yetkili/ortak adı zorunludur.");

            if (request.IsPrimary)
            {
                var currentPrimaryPartners = await _context.WorkshopPartners
                    .Where(x => x.WorkshopId == workshopId && x.IsPrimary)
                    .ToListAsync();

                foreach (var currentPrimary in currentPrimaryPartners)
                {
                    currentPrimary.IsPrimary = false;
                    currentPrimary.UpdatedAt = _dateTimeProvider.Now;
                }
            }

            var partner = new WorkshopPartner
            {
                WorkshopId = workshopId,
                FullName = request.FullName.Trim(),
                Title = request.Title?.Trim(),
                PhoneNumber = request.PhoneNumber?.Trim(),
                Email = request.Email?.Trim(),
                IsPrimary = request.IsPrimary,
                Note = request.Note?.Trim()
            };

            _context.WorkshopPartners.Add(partner);

            await _context.SaveChangesAsync();

            return ServiceResult<int>.Success(partner.Id);
        }

        public async Task<ServiceResult<bool>> UpdatePartnerAsync(int workshopId, int partnerId, UpdateAdminWorkshopPartnerRequestDto request)
        {
            var partner = await _context.WorkshopPartners
                .FirstOrDefaultAsync(x =>
                    x.Id == partnerId &&
                    x.WorkshopId == workshopId);

            if (partner == null)
                return ServiceResult<bool>.Fail(
                    "Yetkili/ortak kaydı bulunamadı.");

            if (string.IsNullOrWhiteSpace(request.FullName))
                return ServiceResult<bool>.Fail("Yetkili/ortak adı zorunludur.");

            if (request.IsPrimary)
            {
                var currentPrimaryPartners = await _context.WorkshopPartners
                    .Where(x =>
                        x.WorkshopId == workshopId &&
                        x.IsPrimary &&
                        x.Id != partnerId)
                    .ToListAsync();

                foreach (var currentPrimary in currentPrimaryPartners)
                {
                    currentPrimary.IsPrimary = false;
                    currentPrimary.UpdatedAt = _dateTimeProvider.Now;
                }
            }

            partner.FullName = request.FullName.Trim();
            partner.Title = request.Title?.Trim();
            partner.PhoneNumber = request.PhoneNumber?.Trim();
            partner.Email = request.Email?.Trim();
            partner.IsPrimary = request.IsPrimary;
            partner.Note = request.Note?.Trim();
            partner.UpdatedAt = _dateTimeProvider.Now;

            await _context.SaveChangesAsync();

            return ServiceResult<bool>.Success(true);
        }

        public async Task<ServiceResult<bool>> DeletePartnerAsync(int workshopId, int partnerId)
        {
            var partner = await _context.WorkshopPartners
                .FirstOrDefaultAsync(x =>
                    x.Id == partnerId &&
                    x.WorkshopId == workshopId);

            if (partner == null)
                return ServiceResult<bool>.Fail(
                    "Yetkili/ortak kaydı bulunamadı.");

            _context.WorkshopPartners.Remove(partner);

            await _context.SaveChangesAsync();

            return ServiceResult<bool>.Success(true);
        }

        public async Task<ServiceResult<SuggestedAdminWorkshopCredentialsDto>> SuggestUserCredentialsAsync(int workshopId, string fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName))
                return ServiceResult<SuggestedAdminWorkshopCredentialsDto>.Fail("Ad soyad zorunludur.");

            var workshopExists = await _context.Workshops
                .AsNoTracking()
                .AnyAsync(x => x.Id == workshopId);

            if (!workshopExists)
                return ServiceResult<SuggestedAdminWorkshopCredentialsDto>.Fail(
                    "Servis bulunamadı.");

            var baseUserName = CreateShortUserName(fullName);

            var userName = await GetAvailableShortUserNameAsync(baseUserName, fullName);

            var password = GenerateShortTemporaryPassword(fullName);

            var result = new SuggestedAdminWorkshopCredentialsDto
            {
                UserName = userName,
                Password = password
            };

            return ServiceResult<SuggestedAdminWorkshopCredentialsDto>.Success(result);
        }

        public async Task<ServiceResult<PagedResult<AdminWorkshopListItemDto>>> GetPagedAsync(AdminWorkshopListQueryDto query)
        {
            var now = _dateTimeProvider.Now;

            query ??= new AdminWorkshopListQueryDto();

            query.PageNumber = query.PageNumber <= 0 ? 1 : query.PageNumber;
            query.PageSize = query.PageSize <= 0 ? 10 : query.PageSize;
            query.PageSize = query.PageSize > 50 ? 50 : query.PageSize;

            var workshopsQuery = _context.Workshops
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var searchText = query.Search.Trim();
                var search = $"%{searchText}%";

                workshopsQuery = workshopsQuery.Where(x =>
                    EF.Functions.Like(x.Name ?? string.Empty, search) ||
                    (
                        x.Profile != null &&
                        (
                            EF.Functions.Like(x.Profile.DisplayName ?? string.Empty, search) ||
                            EF.Functions.Like(x.Profile.LegalTitle ?? string.Empty, search) ||
                            EF.Functions.Like(x.Profile.PhoneNumber ?? string.Empty, search) ||
                            EF.Functions.Like(x.Profile.Email ?? string.Empty, search)
                        )
                    ));
            }

            if (query.IsActive.HasValue)
            {
                workshopsQuery = workshopsQuery.Where(x =>
                    x.IsActive == query.IsActive.Value);
            }

            if (query.SubscriptionStatus.HasValue)
            {
                workshopsQuery = workshopsQuery.Where(x =>
                    x.SubscriptionStatus == query.SubscriptionStatus.Value);
            }

            var totalCount = await workshopsQuery.CountAsync();

            var items = await workshopsQuery
                .OrderByDescending(x => x.CreatedAt)
                .Skip((query.PageNumber - 1) * query.PageSize)
                .Take(query.PageSize)
                .Select(x => new AdminWorkshopListItemDto
                {
                    Id = x.Id,
                    Name = x.Name,
                    IsActive = x.IsActive,
                    SubscriptionStatus = x.SubscriptionStatus,
                    SubscriptionStartDate = x.SubscriptionStartDate,
                    SubscriptionEndDate = x.SubscriptionEndDate,
                    IsExpired = x.SubscriptionEndDate.HasValue &&
                                x.SubscriptionEndDate.Value <= now,
                    CreatedAt = x.CreatedAt,
                    UserCount = _context.WorkshopUsers.Count(u => u.WorkshopId == x.Id)
                })
                .ToListAsync();

            var pagedResult = new PagedResult<AdminWorkshopListItemDto>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = query.PageNumber,
                PageSize = query.PageSize
            };

            return ServiceResult<PagedResult<AdminWorkshopListItemDto>>.Success(pagedResult);
        }

        private async Task<string> GetAvailableShortUserNameAsync(string baseUserName, string fullName)
        {
            var normalizedFullName = NormalizeTurkish(fullName);

            var parts = normalizedFullName
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .ToList();

            var firstName = parts.FirstOrDefault() ?? "User";
            var lastName = parts.Count > 1 ? parts.Last() : "User";

            var candidate = baseUserName;

            if (await _userManager.FindByNameAsync(candidate) == null)
                return candidate;

            // Örn: OrhanG doluysa OrhanGa, OrhanGaz, OrhanGazi diye dener.
            for (var i = 2; i <= lastName.Length; i++)
            {
                candidate = ToPascalCase(firstName) + ToPascalCase(lastName[..i]);

                if (await _userManager.FindByNameAsync(candidate) == null)
                    return candidate;
            }

            // Hepsi doluysa OrhanG2, OrhanG3 diye gider.
            var counter = 2;

            while (true)
            {
                candidate = $"{baseUserName}{counter}";

                if (await _userManager.FindByNameAsync(candidate) == null)
                    return candidate;

                counter++;
            }
        }

        private static string CreateShortUserName(string fullName)
        {
            var normalizedFullName = NormalizeTurkish(fullName);

            var parts = normalizedFullName
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .ToList();

            if (!parts.Any())
                return "User1";

            var firstName = parts.First();

            if (parts.Count == 1)
                return ToPascalCase(firstName);

            var lastName = parts.Last();

            return ToPascalCase(firstName) + ToPascalCase(lastName[..1]);
        }

        private static string GenerateShortTemporaryPassword(string fullName)
        {
            var normalizedFullName = NormalizeTurkish(fullName);

            var firstName = normalizedFullName
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .FirstOrDefault() ?? "User";

            var passwordName = ToPascalCase(firstName);

            if (passwordName.Length < 4)
                passwordName = passwordName.PadRight(4, 'x');

            var number = RandomNumberGenerator.GetInt32(100, 1000);

            return $"{passwordName}{number}.";
        }

        private static string NormalizeTurkish(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            var normalized = value.Trim();

            normalized = normalized
                .Replace("ç", "c")
                .Replace("Ç", "C")
                .Replace("ğ", "g")
                .Replace("Ğ", "G")
                .Replace("ı", "i")
                .Replace("İ", "I")
                .Replace("ö", "o")
                .Replace("Ö", "O")
                .Replace("ş", "s")
                .Replace("Ş", "S")
                .Replace("ü", "u")
                .Replace("Ü", "U");

            normalized = Regex.Replace(normalized, @"[^a-zA-Z0-9\s]", " ");
            normalized = Regex.Replace(normalized, @"\s+", " ");

            return normalized.Trim();
        }

        private static string ToPascalCase(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            value = value.Trim().ToLowerInvariant();

            return char.ToUpperInvariant(value[0]) + value[1..];
        }

        private async Task<ServiceResult<int>> ValidateCreateUserRequestAsync(CreateAdminWorkshopUserRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request.FullName))
                return ServiceResult<int>.Fail("Ad soyad zorunludur.");

            if (string.IsNullOrWhiteSpace(request.UserName))
                return ServiceResult<int>.Fail("Kullanıcı adı zorunludur.");

            

            var role = request.Role?.Trim();

            if (role != AppRoles.Owner && role != AppRoles.Staff)
                return ServiceResult<int>.Fail("Kullanıcı rolü sadece Owner veya Staff olabilir.");

            var roleExists = await _roleManager.RoleExistsAsync(role);

            if (!roleExists)
                return ServiceResult<int>.Fail($"{role} rolü sistemde bulunamadı.");

            var userNameExists = await _userManager.FindByNameAsync(request.UserName.Trim());

            if (userNameExists is not null)
                return ServiceResult<int>.Fail("Bu kullanıcı adı zaten kullanılıyor.");

            if (!string.IsNullOrWhiteSpace(request.Email))
            {
                var emailExists = await _userManager.FindByEmailAsync(request.Email.Trim());

                if (emailExists is not null)
                    return ServiceResult<int>.Fail("Bu e-posta adresi zaten kullanılıyor.");
            }

            return ServiceResult<int>.Success(0);
        }

        private async Task<ServiceResult<int>> ValidateCreateRequestAsync(CreateAdminWorkshopRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request.WorkshopName))
                return ServiceResult<int>.Fail("Servis adı zorunludur.");

            if (string.IsNullOrWhiteSpace(request.FirstUserFullName))
                return ServiceResult<int>.Fail("İlk kullanıcı adı soyadı zorunludur.");

            if (string.IsNullOrWhiteSpace(request.FirstUserName))
                return ServiceResult<int>.Fail("İlk kullanıcı kullanıcı adı zorunludur.");

            if (string.IsNullOrWhiteSpace(request.FirstUserPassword))
                return ServiceResult<int>.Fail("İlk kullanıcı şifresi zorunludur.");

            var role = request.FirstUserRole?.Trim();

            if (role != AppRoles.Owner && role != AppRoles.Staff)
                return ServiceResult<int>.Fail("İlk kullanıcı rolü sadece Owner veya Staff olabilir.");

            var roleExists = await _roleManager.RoleExistsAsync(role);

            if (!roleExists)
                return ServiceResult<int>.Fail($"{role} rolü sistemde bulunamadı.");

            var userNameExists = await _userManager.FindByNameAsync(request.FirstUserName.Trim());

            if (userNameExists is not null)
                return ServiceResult<int>.Fail("Bu kullanıcı adı zaten kullanılıyor.");

            if (!string.IsNullOrWhiteSpace(request.FirstUserEmail))
            {
                var emailExists = await _userManager.FindByEmailAsync(request.FirstUserEmail.Trim());

                if (emailExists is not null)
                    return ServiceResult<int>.Fail("Bu e-posta adresi zaten kullanılıyor.");
            }

            if (request.TrialDays.HasValue && request.TrialDays.Value < 0)
                return ServiceResult<int>.Fail("Trial gün sayısı negatif olamaz.");

            if (request.SubscriptionEndDate.HasValue &&
                request.SubscriptionEndDate.Value <= _dateTimeProvider.Now)
                return ServiceResult<int>.Fail("Üyelik bitiş tarihi geçmişte olamaz.");

            return ServiceResult<int>.Success(0);
        }

        private static object GetWorkshopProfileAuditValues(WorkshopProfile profile)
        {
            return new
            {
                profile.WorkshopId,
                profile.DisplayName,
                profile.LegalTitle,
                profile.TaxOffice,
                profile.TaxNumber,
                profile.TradeRegistryNumber,
                profile.MersisNumber,
                profile.Email,
                profile.PhoneNumber,
                profile.FaxNumber,
                profile.Website,
                profile.AddressLine,
                profile.City,
                profile.District,
                profile.PostalCode,
                profile.Country
            };
        }
    }
}