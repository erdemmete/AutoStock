using AutoStock.Repositories.Entities;
using AutoStock.Repositories.Enums;
using AutoStock.Repositories.Interfaces;
using AutoStock.Services.Constants;
using AutoStock.Services.Dtos.AuditLogs;
using AutoStock.Services.Dtos.Common;
using AutoStock.Services.Dtos.SupportRequests;
using AutoStock.Services.Interfaces;
using System.Net;

namespace AutoStock.Services.Services
{
    public class SupportRequestService(
        ISupportRequestRepository supportRequestRepository,
        IAuditLogService auditLogService,
        IDateTimeProvider dateTimeProvider) : ISupportRequestService
    {
        public async Task<ServiceResult<PagedResult<SupportRequestListItemDto>>> GetPagedForWorkshopAsync(
            SupportRequestListQueryDto query,
            int workshopId)
        {
            query ??= new SupportRequestListQueryDto();
            query.Normalize();

            var totalCount = await supportRequestRepository.GetCountForWorkshopAsync(
                workshopId,
                query.Status,
                query.RequestType,
                query.Search,
                query.StartDate,
                query.EndDate);

            var items = await supportRequestRepository.GetListForWorkshopAsync(
                workshopId,
                query.Status,
                query.RequestType,
                query.Search,
                query.StartDate,
                query.EndDate,
                query.PageNumber,
                query.PageSize);

            var result = new PagedResult<SupportRequestListItemDto>
            {
                Items = items.Select(MapToListItemDto).ToList(),
                PageNumber = query.PageNumber,
                PageSize = query.PageSize,
                TotalCount = totalCount
            };

            return ServiceResult<PagedResult<SupportRequestListItemDto>>.Success(result);
        }

        public async Task<ServiceResult<SupportRequestDetailDto>> GetByIdForWorkshopAsync(
            int id,
            int workshopId)
        {
            var supportRequest = await supportRequestRepository.GetByIdForWorkshopAsync(id, workshopId);

            if (supportRequest == null)
                return ServiceResult<SupportRequestDetailDto>.Fail("Destek talebi bulunamadı.");

            return ServiceResult<SupportRequestDetailDto>.Success(MapToDetailDto(supportRequest));
        }

        public async Task<ServiceResult<int>> CreateIssueAsync(
            CreateIssueSupportRequestDto request,
            int workshopId,
            int createdByUserId)
        {
            var now = dateTimeProvider.Now;

            var supportRequest = new SupportRequest
            {
                WorkshopId = workshopId,
                CreatedByUserId = createdByUserId,
                RequestType = SupportRequestType.Issue,
                Status = SupportRequestStatus.Open,
                Priority = request.Priority,
                Subject = request.Subject.Trim(),
                Description = request.Description.Trim(),
                CreatedAt = now
            };

            await supportRequestRepository.AddAsync(supportRequest);
            await supportRequestRepository.SaveChangesAsync();

            await auditLogService.AddAsync(new AuditLogCreateDto
            {
                WorkshopId = workshopId,
                ActionType = AuditActionType.Create,
                EntityType = AuditEntityType.SupportRequest,
                EntityId = supportRequest.Id,
                Description = $"Destek talebi oluşturuldu: {supportRequest.Subject}",
                NewValues = new
                {
                    supportRequest.RequestType,
                    supportRequest.Status,
                    supportRequest.Priority,
                    supportRequest.Subject
                }
            });

            await supportRequestRepository.SaveChangesAsync();

            return ServiceResult<int>.Success(supportRequest.Id);
        }

        public async Task<ServiceResult<int>> CreateUserCreateRequestAsync(
            CreateUserSupportRequestDto request,
            int workshopId,
            int createdByUserId,
            string? currentUserRole)
        {
            if (currentUserRole != AppRoles.Owner)
            {
                return ServiceResult<int>.Fail(
                    "Kullanıcı ekleme talebini sadece servis sahibi oluşturabilir.",
                    HttpStatusCode.Forbidden);
            }

            var now = dateTimeProvider.Now;

            var supportRequest = new SupportRequest
            {
                WorkshopId = workshopId,
                CreatedByUserId = createdByUserId,
                RequestType = SupportRequestType.UserCreateRequest,
                Status = SupportRequestStatus.Open,
                Priority = request.Priority,
                Subject = "Kullanıcı ekleme talebi",
                Description = string.IsNullOrWhiteSpace(request.Note)
                    ? "Servis kullanıcısı ekleme talebi oluşturuldu."
                    : request.Note.Trim(),

                RequestedUserFullName = request.RequestedUserFullName.Trim(),
                RequestedUserPhone = request.RequestedUserPhone?.Trim(),
                RequestedUserEmail = request.RequestedUserEmail?.Trim(),
                RequestedUserRole = request.RequestedUserRole,

                CreatedAt = now
            };

            await supportRequestRepository.AddAsync(supportRequest);
            await supportRequestRepository.SaveChangesAsync();

            await auditLogService.AddAsync(new AuditLogCreateDto
            {
                WorkshopId = workshopId,
                ActionType = AuditActionType.Create,
                EntityType = AuditEntityType.SupportRequest,
                EntityId = supportRequest.Id,
                Description = $"Kullanıcı ekleme talebi oluşturuldu: {supportRequest.RequestedUserFullName}",
                NewValues = new
                {
                    supportRequest.RequestType,
                    supportRequest.Status,
                    supportRequest.Priority,
                    supportRequest.RequestedUserFullName,
                    supportRequest.RequestedUserPhone,
                    supportRequest.RequestedUserEmail,
                    supportRequest.RequestedUserRole
                }
            });

            await supportRequestRepository.SaveChangesAsync();

            return ServiceResult<int>.Success(supportRequest.Id);
        }

        public async Task<ServiceResult<int>> CancelForWorkshopAsync(
            int id,
            int workshopId,
            int currentUserId)
        {
            var supportRequest = await supportRequestRepository.GetByIdForWorkshopAsync(id, workshopId);

            if (supportRequest == null)
                return ServiceResult<int>.Fail("Destek talebi bulunamadı.");

            if (supportRequest.CreatedByUserId != currentUserId)
            {
                return ServiceResult<int>.Fail(
                    "Sadece kendi oluşturduğunuz destek talebini iptal edebilirsiniz.",
                    HttpStatusCode.Forbidden);
            }

            if (supportRequest.Status is SupportRequestStatus.Closed or SupportRequestStatus.Cancelled)
                return ServiceResult<int>.Fail("Bu destek talebi artık iptal edilemez.");

            var oldStatus = supportRequest.Status;
            var now = dateTimeProvider.Now;

            supportRequest.Status = SupportRequestStatus.Cancelled;
            supportRequest.UpdatedAt = now;
            supportRequest.ClosedAt = now;

            supportRequestRepository.Update(supportRequest);

            await auditLogService.AddAsync(new AuditLogCreateDto
            {
                WorkshopId = workshopId,
                ActionType = AuditActionType.Cancel,
                EntityType = AuditEntityType.SupportRequest,
                EntityId = supportRequest.Id,
                Description = $"Destek talebi iptal edildi: {supportRequest.Subject}",
                OldValues = new
                {
                    Status = oldStatus
                },
                NewValues = new
                {
                    supportRequest.Status
                }
            });

            await supportRequestRepository.SaveChangesAsync();

            return ServiceResult<int>.Success(supportRequest.Id);
        }

        public async Task<ServiceResult<PagedResult<SupportRequestListItemDto>>> GetPagedForAdminAsync(
            AdminSupportRequestListQueryDto query)
        {
            query ??= new AdminSupportRequestListQueryDto();
            query.Normalize();

            var totalCount = await supportRequestRepository.GetCountForAdminAsync(
                query.WorkshopId,
                query.Status,
                query.RequestType,
                query.Search,
                query.StartDate,
                query.EndDate);

            var items = await supportRequestRepository.GetListForAdminAsync(
                query.WorkshopId,
                query.Status,
                query.RequestType,
                query.Search,
                query.StartDate,
                query.EndDate,
                query.PageNumber,
                query.PageSize);

            var result = new PagedResult<SupportRequestListItemDto>
            {
                Items = items.Select(MapToListItemDto).ToList(),
                PageNumber = query.PageNumber,
                PageSize = query.PageSize,
                TotalCount = totalCount
            };

            return ServiceResult<PagedResult<SupportRequestListItemDto>>.Success(result);
        }

        public async Task<ServiceResult<SupportRequestDetailDto>> GetByIdForAdminAsync(int id)
        {
            var supportRequest = await supportRequestRepository.GetByIdAsync(id);

            if (supportRequest == null)
                return ServiceResult<SupportRequestDetailDto>.Fail("Destek talebi bulunamadı.");

            return ServiceResult<SupportRequestDetailDto>.Success(MapToDetailDto(supportRequest));
        }

        public async Task<ServiceResult<int>> AnswerAsync(
            AdminAnswerSupportRequestDto request,
            int respondedByUserId)
        {
            var supportRequest = await supportRequestRepository.GetByIdAsync(request.Id);

            if (supportRequest == null)
                return ServiceResult<int>.Fail("Destek talebi bulunamadı.");

            if (supportRequest.Status is SupportRequestStatus.Closed or SupportRequestStatus.Cancelled)
                return ServiceResult<int>.Fail("Kapalı veya iptal edilmiş talebe cevap yazılamaz.");

            if (request.Status is SupportRequestStatus.Open or SupportRequestStatus.Cancelled)
                return ServiceResult<int>.Fail("Cevap sonrası durum Yanıtlandı, İşlemde veya Kapandı olabilir.");

            var oldValues = new
            {
                supportRequest.Status,
                supportRequest.AdminResponse
            };

            var now = dateTimeProvider.Now;

            supportRequest.AdminResponse = request.AdminResponse.Trim();
            supportRequest.RespondedByUserId = respondedByUserId;
            supportRequest.RespondedAt = now;
            supportRequest.UpdatedAt = now;
            supportRequest.Status = request.Status;

            if (request.Status == SupportRequestStatus.Closed)
                supportRequest.ClosedAt = now;

            supportRequestRepository.Update(supportRequest);

            await auditLogService.AddAsync(new AuditLogCreateDto
            {
                WorkshopId = supportRequest.WorkshopId,
                ActionType = AuditActionType.Update,
                EntityType = AuditEntityType.SupportRequest,
                EntityId = supportRequest.Id,
                Description = $"Destek talebi yanıtlandı: {supportRequest.Subject}",
                OldValues = oldValues,
                NewValues = new
                {
                    supportRequest.Status,
                    HasAdminResponse = !string.IsNullOrWhiteSpace(supportRequest.AdminResponse),
                    supportRequest.RespondedByUserId,
                    supportRequest.RespondedAt
                }
            });

            await supportRequestRepository.SaveChangesAsync();

            return ServiceResult<int>.Success(supportRequest.Id);
        }

        public async Task<ServiceResult<int>> UpdateStatusAsync(
            AdminUpdateSupportRequestStatusDto request,
            int updatedByUserId)
        {
            var supportRequest = await supportRequestRepository.GetByIdAsync(request.Id);

            if (supportRequest == null)
                return ServiceResult<int>.Fail("Destek talebi bulunamadı.");

            var oldStatus = supportRequest.Status;
            var now = dateTimeProvider.Now;

            supportRequest.Status = request.Status;
            supportRequest.UpdatedAt = now;

            if (request.Status is SupportRequestStatus.Closed or SupportRequestStatus.Cancelled)
                supportRequest.ClosedAt = now;

            if (request.Status is SupportRequestStatus.Open or SupportRequestStatus.InProgress or SupportRequestStatus.Answered)
                supportRequest.ClosedAt = null;

            supportRequestRepository.Update(supportRequest);

            await auditLogService.AddAsync(new AuditLogCreateDto
            {
                WorkshopId = supportRequest.WorkshopId,
                ActionType = AuditActionType.Update,
                EntityType = AuditEntityType.SupportRequest,
                EntityId = supportRequest.Id,
                Description = $"Destek talebi durumu güncellendi: {supportRequest.Subject}",
                OldValues = new
                {
                    Status = oldStatus
                },
                NewValues = new
                {
                    supportRequest.Status,
                    UpdatedByUserId = updatedByUserId
                }
            });

            await supportRequestRepository.SaveChangesAsync();

            return ServiceResult<int>.Success(supportRequest.Id);
        }

        private static SupportRequestListItemDto MapToListItemDto(SupportRequest supportRequest)
        {
            return new SupportRequestListItemDto
            {
                Id = supportRequest.Id,
                WorkshopId = supportRequest.WorkshopId,
                WorkshopName = supportRequest.Workshop?.Name,

                RequestType = supportRequest.RequestType,
                RequestTypeText = GetRequestTypeText(supportRequest.RequestType),

                Status = supportRequest.Status,
                StatusText = GetStatusText(supportRequest.Status),

                Priority = supportRequest.Priority,
                PriorityText = GetPriorityText(supportRequest.Priority),

                Subject = supportRequest.Subject,
                CreatedByUserName = supportRequest.CreatedByUser?.FullName ?? supportRequest.CreatedByUser?.UserName ?? "-",

                CreatedAt = supportRequest.CreatedAt,
                RespondedAt = supportRequest.RespondedAt,
                ClosedAt = supportRequest.ClosedAt
            };
        }

        private static SupportRequestDetailDto MapToDetailDto(SupportRequest supportRequest)
        {
            return new SupportRequestDetailDto
            {
                Id = supportRequest.Id,
                WorkshopId = supportRequest.WorkshopId,
                WorkshopName = supportRequest.Workshop?.Name,

                CreatedByUserId = supportRequest.CreatedByUserId,
                CreatedByUserName = supportRequest.CreatedByUser?.FullName ?? supportRequest.CreatedByUser?.UserName ?? "-",

                RequestType = supportRequest.RequestType,
                RequestTypeText = GetRequestTypeText(supportRequest.RequestType),

                Status = supportRequest.Status,
                StatusText = GetStatusText(supportRequest.Status),

                Priority = supportRequest.Priority,
                PriorityText = GetPriorityText(supportRequest.Priority),

                Subject = supportRequest.Subject,
                Description = supportRequest.Description,

                RequestedUserFullName = supportRequest.RequestedUserFullName,
                RequestedUserPhone = supportRequest.RequestedUserPhone,
                RequestedUserEmail = supportRequest.RequestedUserEmail,

                RequestedUserRole = supportRequest.RequestedUserRole,
                RequestedUserRoleText = supportRequest.RequestedUserRole.HasValue
                    ? GetRequestedUserRoleText(supportRequest.RequestedUserRole.Value)
                    : null,

                AdminResponse = supportRequest.AdminResponse,

                RespondedByUserId = supportRequest.RespondedByUserId,
                RespondedByUserName = supportRequest.RespondedByUser?.FullName ?? supportRequest.RespondedByUser?.UserName,

                RespondedAt = supportRequest.RespondedAt,
                CreatedAt = supportRequest.CreatedAt,
                UpdatedAt = supportRequest.UpdatedAt,
                ClosedAt = supportRequest.ClosedAt
            };
        }

        private static string GetRequestTypeText(SupportRequestType requestType)
        {
            return requestType switch
            {
                SupportRequestType.Issue => "Sorun Bildirimi",
                SupportRequestType.UserCreateRequest => "Kullanıcı Ekleme Talebi",
                _ => "Bilinmeyen"
            };
        }

        private static string GetStatusText(SupportRequestStatus status)
        {
            return status switch
            {
                SupportRequestStatus.Open => "Açık",
                SupportRequestStatus.InProgress => "İşlemde",
                SupportRequestStatus.Answered => "Yanıtlandı",
                SupportRequestStatus.Closed => "Kapandı",
                SupportRequestStatus.Cancelled => "İptal Edildi",
                _ => "Bilinmeyen"
            };
        }

        private static string GetPriorityText(SupportRequestPriority priority)
        {
            return priority switch
            {
                SupportRequestPriority.Low => "Düşük",
                SupportRequestPriority.Normal => "Normal",
                SupportRequestPriority.High => "Yüksek",
                SupportRequestPriority.Critical => "Kritik",
                _ => "Bilinmeyen"
            };
        }

        private static string GetRequestedUserRoleText(SupportRequestedUserRole role)
        {
            return role switch
            {
                SupportRequestedUserRole.Owner => "Servis Sahibi",
                SupportRequestedUserRole.Staff => "Personel",
                _ => "Bilinmeyen"
            };
        }
    }
}