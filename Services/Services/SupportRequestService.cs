using AutoStock.Repositories.Entities;
using AutoStock.Repositories.Enums;
using AutoStock.Repositories.Interfaces;
using AutoStock.Services.Constants;
using AutoStock.Services.Dtos.AuditLogs;
using AutoStock.Services.Dtos.Common;
using AutoStock.Services.Dtos.Notifications;
using AutoStock.Services.Dtos.SupportRequests;
using AutoStock.Services.Interfaces;
using System.Net;

namespace AutoStock.Services.Services
{
    public class SupportRequestService : ISupportRequestService
    {
        private readonly ISupportRequestRepository _supportRequestRepository;
        private readonly ISupportRequestMessageRepository _supportRequestMessageRepository;
        private readonly IAuditLogService _auditLogService;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly INotificationService _notificationService;

        public SupportRequestService(
            ISupportRequestRepository supportRequestRepository,
            ISupportRequestMessageRepository supportRequestMessageRepository,
            IAuditLogService auditLogService,
            IDateTimeProvider dateTimeProvider,
            INotificationService notificationService)
        {
            _supportRequestRepository = supportRequestRepository;
            _supportRequestMessageRepository = supportRequestMessageRepository;
            _auditLogService = auditLogService;
            _dateTimeProvider = dateTimeProvider;
            _notificationService = notificationService;
        }

        public async Task<ServiceResult<PagedResult<SupportRequestListItemDto>>> GetPagedForWorkshopAsync(
            SupportRequestListQueryDto query,
            int workshopId,
            int currentUserId,
            string? currentUserRole)
        {
            query ??= new SupportRequestListQueryDto();
            query.Normalize();

            var createdByUserId = currentUserRole == AppRoles.Staff
                ? currentUserId
                : (int?)null;

            var excludeClosedAndCancelled = !query.Status.HasValue;

            var totalCount = await _supportRequestRepository.GetCountForWorkshopAsync(
                workshopId,
                query.Status,
                query.RequestType,
                query.Search,
                query.StartDate,
                query.EndDate,
                createdByUserId,
                excludeClosedAndCancelled);

            var items = await _supportRequestRepository.GetListForWorkshopAsync(
                workshopId,
                query.Status,
                query.RequestType,
                query.Search,
                query.StartDate,
                query.EndDate,
                query.PageNumber,
                query.PageSize,
                createdByUserId,
                excludeClosedAndCancelled);

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
            int workshopId,
            int currentUserId,
            string? currentUserRole)
        {
            var createdByUserId = currentUserRole == AppRoles.Staff
                ? currentUserId
                : (int?)null;

            var supportRequest = await _supportRequestRepository.GetByIdForWorkshopAsync(
                id,
                workshopId,
                createdByUserId);

            if (supportRequest == null)
                return ServiceResult<SupportRequestDetailDto>.Fail("Destek talebi bulunamadı.");

            var messages = await _supportRequestMessageRepository.GetBySupportRequestIdAsync(supportRequest.Id);

            return ServiceResult<SupportRequestDetailDto>.Success(MapToDetailDto(supportRequest, messages));
        }

        public async Task<ServiceResult<int>> CreateIssueAsync(
            CreateIssueSupportRequestDto request,
            int workshopId,
            int createdByUserId)
        {
            if (string.IsNullOrWhiteSpace(request.Subject))
                return ServiceResult<int>.Fail("Konu zorunludur.", HttpStatusCode.BadRequest);

            if (string.IsNullOrWhiteSpace(request.Description))
                return ServiceResult<int>.Fail("Açıklama zorunludur.", HttpStatusCode.BadRequest);

            var now = _dateTimeProvider.Now;

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

            await _supportRequestRepository.AddAsync(supportRequest);
            await _supportRequestRepository.SaveChangesAsync();

            await _supportRequestMessageRepository.AddAsync(new SupportRequestMessage
            {
                SupportRequestId = supportRequest.Id,
                SenderUserId = createdByUserId,
                IsAdminMessage = false,
                Message = supportRequest.Description,
                CreatedAt = now
            });

            await _auditLogService.AddAsync(new AuditLogCreateDto
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

            await _notificationService.CreateForAdminsAsync(new CreateNotificationDto
            {
                WorkshopId = workshopId,
                Type = NotificationType.SupportRequestCreated,
                Title = "Yeni destek talebi",
                Message = supportRequest.Subject,
                RelatedEntityType = NotificationRelatedEntityType.SupportRequest,
                RelatedEntityId = supportRequest.Id,
                ActionUrl = $"/AdminSupportRequests/Detail/{supportRequest.Id}",
                CreatedByUserId = createdByUserId
            });

            await _supportRequestRepository.SaveChangesAsync();

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

            if (string.IsNullOrWhiteSpace(request.RequestedUserFullName))
                return ServiceResult<int>.Fail("Talep edilen kullanıcı adı zorunludur.", HttpStatusCode.BadRequest);

            var now = _dateTimeProvider.Now;
            var initialMessage = string.IsNullOrWhiteSpace(request.Note)
                ? "Servis kullanıcısı ekleme talebi oluşturuldu."
                : request.Note.Trim();

            var supportRequest = new SupportRequest
            {
                WorkshopId = workshopId,
                CreatedByUserId = createdByUserId,
                RequestType = SupportRequestType.UserCreateRequest,
                Status = SupportRequestStatus.Open,
                Priority = request.Priority,
                Subject = "Kullanıcı ekleme talebi",
                Description = initialMessage,
                RequestedUserFullName = request.RequestedUserFullName.Trim(),
                RequestedUserPhone = request.RequestedUserPhone?.Trim(),
                RequestedUserEmail = request.RequestedUserEmail?.Trim(),
                RequestedUserRole = request.RequestedUserRole,
                CreatedAt = now
            };

            await _supportRequestRepository.AddAsync(supportRequest);
            await _supportRequestRepository.SaveChangesAsync();

            await _supportRequestMessageRepository.AddAsync(new SupportRequestMessage
            {
                SupportRequestId = supportRequest.Id,
                SenderUserId = createdByUserId,
                IsAdminMessage = false,
                Message = initialMessage,
                CreatedAt = now
            });

            await _auditLogService.AddAsync(new AuditLogCreateDto
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

            await _notificationService.CreateForAdminsAsync(new CreateNotificationDto
            {
                WorkshopId = workshopId,
                Type = NotificationType.SupportRequestCreated,
                Title = "Yeni kullanıcı ekleme talebi",
                Message = $"{supportRequest.RequestedUserFullName} için kullanıcı ekleme talebi oluşturuldu.",
                RelatedEntityType = NotificationRelatedEntityType.SupportRequest,
                RelatedEntityId = supportRequest.Id,
                ActionUrl = $"/AdminSupportRequests/Detail/{supportRequest.Id}",
                CreatedByUserId = createdByUserId
            });

            await _supportRequestRepository.SaveChangesAsync();

            return ServiceResult<int>.Success(supportRequest.Id);
        }

        public async Task<ServiceResult<int>> AddWorkshopMessageAsync(
            CreateSupportRequestMessageDto request,
            int workshopId,
            int currentUserId,
            string? currentUserRole)
        {
            if (string.IsNullOrWhiteSpace(request.Message))
                return ServiceResult<int>.Fail("Mesaj boş olamaz.", HttpStatusCode.BadRequest);

            var createdByUserId = currentUserRole == AppRoles.Staff
                ? currentUserId
                : (int?)null;

            var supportRequest = await _supportRequestRepository.GetByIdForWorkshopAsync(
                request.Id,
                workshopId,
                createdByUserId);

            if (supportRequest == null)
                return ServiceResult<int>.Fail("Destek talebi bulunamadı.");

            if (supportRequest.Status == SupportRequestStatus.Cancelled)
                return ServiceResult<int>.Fail("İptal edilmiş destek talebine mesaj yazılamaz.");

            var oldStatus = supportRequest.Status;
            var now = _dateTimeProvider.Now;

            await _supportRequestMessageRepository.AddAsync(new SupportRequestMessage
            {
                SupportRequestId = supportRequest.Id,
                SenderUserId = currentUserId,
                IsAdminMessage = false,
                Message = request.Message.Trim(),
                CreatedAt = now
            });

            // Kullanıcı kapatılmış kayda cevap yazarsa talep otomatik yeniden açılır.
            if (supportRequest.Status == SupportRequestStatus.Closed)
            {
                supportRequest.Status = SupportRequestStatus.Open;
                supportRequest.ClosedAt = null;
            }

            supportRequest.UpdatedAt = now;
            _supportRequestRepository.Update(supportRequest);

            await _auditLogService.AddAsync(new AuditLogCreateDto
            {
                WorkshopId = supportRequest.WorkshopId,
                ActionType = AuditActionType.Update,
                EntityType = AuditEntityType.SupportRequest,
                EntityId = supportRequest.Id,
                Description = $"Destek talebine kullanıcı mesajı eklendi: {supportRequest.Subject}",
                OldValues = new { Status = oldStatus },
                NewValues = new { supportRequest.Status }
            });

            await _notificationService.CreateForAdminsAsync(new CreateNotificationDto
            {
                WorkshopId = supportRequest.WorkshopId,
                Type = NotificationType.SupportRequestAnswered,
                Title = oldStatus == SupportRequestStatus.Closed
                    ? "Destek talebi yeniden açıldı"
                    : "Destek talebine yeni mesaj geldi",
                Message = supportRequest.Subject,
                RelatedEntityType = NotificationRelatedEntityType.SupportRequest,
                RelatedEntityId = supportRequest.Id,
                ActionUrl = $"/AdminSupportRequests/Detail/{supportRequest.Id}",
                CreatedByUserId = currentUserId
            });

            await _supportRequestRepository.SaveChangesAsync();

            return ServiceResult<int>.Success(supportRequest.Id);
        }

        public async Task<ServiceResult<int>> CancelForWorkshopAsync(
            int id,
            int workshopId,
            int currentUserId)
        {
            var supportRequest = await _supportRequestRepository.GetByIdForWorkshopAsync(id, workshopId);

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
            var now = _dateTimeProvider.Now;

            supportRequest.Status = SupportRequestStatus.Cancelled;
            supportRequest.UpdatedAt = now;
            supportRequest.ClosedAt = now;

            _supportRequestRepository.Update(supportRequest);

            await _auditLogService.AddAsync(new AuditLogCreateDto
            {
                WorkshopId = workshopId,
                ActionType = AuditActionType.Cancel,
                EntityType = AuditEntityType.SupportRequest,
                EntityId = supportRequest.Id,
                Description = $"Destek talebi iptal edildi: {supportRequest.Subject}",
                OldValues = new { Status = oldStatus },
                NewValues = new { supportRequest.Status }
            });

            await _supportRequestRepository.SaveChangesAsync();

            return ServiceResult<int>.Success(supportRequest.Id);
        }

        public async Task<ServiceResult<PagedResult<SupportRequestListItemDto>>> GetPagedForAdminAsync(
            AdminSupportRequestListQueryDto query)
        {
            query ??= new AdminSupportRequestListQueryDto();
            query.Normalize();

            var totalCount = await _supportRequestRepository.GetCountForAdminAsync(
                query.WorkshopId,
                query.Status,
                query.RequestType,
                query.Search,
                query.StartDate,
                query.EndDate);

            var items = await _supportRequestRepository.GetListForAdminAsync(
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
            var supportRequest = await _supportRequestRepository.GetByIdAsync(id);

            if (supportRequest == null)
                return ServiceResult<SupportRequestDetailDto>.Fail("Destek talebi bulunamadı.");

            var messages = await _supportRequestMessageRepository.GetBySupportRequestIdAsync(supportRequest.Id);

            return ServiceResult<SupportRequestDetailDto>.Success(MapToDetailDto(supportRequest, messages));
        }

        public async Task<ServiceResult<int>> AnswerAsync(
            AdminAnswerSupportRequestDto request,
            int respondedByUserId)
        {
            var supportRequest = await _supportRequestRepository.GetByIdAsync(request.Id);

            if (supportRequest == null)
                return ServiceResult<int>.Fail("Destek talebi bulunamadı.");

            if (supportRequest.Status == SupportRequestStatus.Cancelled)
                return ServiceResult<int>.Fail("İptal edilmiş talebe cevap yazılamaz.");

            if (string.IsNullOrWhiteSpace(request.AdminResponse))
                return ServiceResult<int>.Fail("Cevap boş olamaz.", HttpStatusCode.BadRequest);

            var oldValues = new
            {
                supportRequest.Status,
                supportRequest.AdminResponse
            };

            var now = _dateTimeProvider.Now;

            supportRequest.AdminResponse = request.AdminResponse.Trim(); // Eski alan geriye uyumluluk için son admin cevabı olarak tutulur.
            supportRequest.RespondedByUserId = respondedByUserId;
            supportRequest.RespondedAt = now;
            supportRequest.UpdatedAt = now;
            supportRequest.Status = request.CloseAfterAnswer
                ? SupportRequestStatus.Closed
                : SupportRequestStatus.InProgress;

            if (supportRequest.Status == SupportRequestStatus.Closed)
                supportRequest.ClosedAt = now;
            else
                supportRequest.ClosedAt = null;

            await _supportRequestMessageRepository.AddAsync(new SupportRequestMessage
            {
                SupportRequestId = supportRequest.Id,
                SenderUserId = respondedByUserId,
                IsAdminMessage = true,
                Message = request.AdminResponse.Trim(),
                CreatedAt = now
            });

            _supportRequestRepository.Update(supportRequest);

            await _auditLogService.AddAsync(new AuditLogCreateDto
            {
                WorkshopId = supportRequest.WorkshopId,
                ActionType = AuditActionType.Update,
                EntityType = AuditEntityType.SupportRequest,
                EntityId = supportRequest.Id,
                Description = request.CloseAfterAnswer
                    ? $"Destek talebi cevaplandı ve kapatıldı: {supportRequest.Subject}"
                    : $"Destek talebine admin cevabı eklendi: {supportRequest.Subject}",
                OldValues = oldValues,
                NewValues = new
                {
                    supportRequest.Status,
                    HasAdminResponse = !string.IsNullOrWhiteSpace(supportRequest.AdminResponse),
                    supportRequest.RespondedByUserId,
                    supportRequest.RespondedAt
                }
            });

            await _notificationService.CreateForWorkshopOwnersAndUsersAsync(
                supportRequest.WorkshopId,
                new[] { supportRequest.CreatedByUserId },
                new CreateNotificationDto
                {
                    WorkshopId = supportRequest.WorkshopId,
                    Type = NotificationType.SupportRequestAnswered,
                    Title = request.CloseAfterAnswer
                        ? "Destek talebiniz kapatıldı"
                        : "Destek talebinize cevap geldi",
                    Message = supportRequest.Subject,
                    RelatedEntityType = NotificationRelatedEntityType.SupportRequest,
                    RelatedEntityId = supportRequest.Id,
                    ActionUrl = $"/SupportRequests/Detail/{supportRequest.Id}",
                    CreatedByUserId = respondedByUserId
                });

            await _supportRequestRepository.SaveChangesAsync();

            return ServiceResult<int>.Success(supportRequest.Id);
        }

        public async Task<ServiceResult<int>> UpdateStatusAsync(
            AdminUpdateSupportRequestStatusDto request,
            int updatedByUserId)
        {
            var supportRequest = await _supportRequestRepository.GetByIdAsync(request.Id);

            if (supportRequest == null)
                return ServiceResult<int>.Fail("Destek talebi bulunamadı.");

            if (request.Status is SupportRequestStatus.Answered)
                return ServiceResult<int>.Fail("Yanıtlandı durumu artık kullanılmıyor. Açık, İşlemde, Kapandı veya İptal seçiniz.", HttpStatusCode.BadRequest);

            var oldStatus = supportRequest.Status;
            var now = _dateTimeProvider.Now;

            supportRequest.Status = request.Status;
            supportRequest.UpdatedAt = now;

            if (request.Status is SupportRequestStatus.Closed or SupportRequestStatus.Cancelled)
                supportRequest.ClosedAt = now;

            if (request.Status is SupportRequestStatus.Open or SupportRequestStatus.InProgress)
                supportRequest.ClosedAt = null;

            _supportRequestRepository.Update(supportRequest);

            await _auditLogService.AddAsync(new AuditLogCreateDto
            {
                WorkshopId = supportRequest.WorkshopId,
                ActionType = AuditActionType.Update,
                EntityType = AuditEntityType.SupportRequest,
                EntityId = supportRequest.Id,
                Description = $"Destek talebi durumu güncellendi: {supportRequest.Subject}",
                OldValues = new { Status = oldStatus },
                NewValues = new
                {
                    supportRequest.Status,
                    UpdatedByUserId = updatedByUserId
                }
            });

            if (oldStatus != supportRequest.Status)
            {
                await _notificationService.CreateForWorkshopOwnersAndUsersAsync(
                    supportRequest.WorkshopId,
                    new[] { supportRequest.CreatedByUserId },
                    new CreateNotificationDto
                    {
                        WorkshopId = supportRequest.WorkshopId,
                        Type = NotificationType.SupportRequestStatusChanged,
                        Title = "Destek talebi durumu güncellendi",
                        Message = $"{supportRequest.Subject} - {GetStatusText(supportRequest.Status)}",
                        RelatedEntityType = NotificationRelatedEntityType.SupportRequest,
                        RelatedEntityId = supportRequest.Id,
                        ActionUrl = $"/SupportRequests/Detail/{supportRequest.Id}",
                        CreatedByUserId = updatedByUserId
                    });
            }

            await _supportRequestRepository.SaveChangesAsync();

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

        private static SupportRequestDetailDto MapToDetailDto(
            SupportRequest supportRequest,
            IReadOnlyCollection<SupportRequestMessage>? messages = null)
        {
            var messageDtos = (messages ?? Array.Empty<SupportRequestMessage>())
                .OrderBy(x => x.CreatedAt)
                .Select(MapToMessageDto)
                .ToList();

            // Eski kayıtlar için geriye uyumluluk: mesaj tablosunda kayıt yoksa Description/AdminResponse'tan göster.
            if (!messageDtos.Any())
            {
                messageDtos.Add(new SupportRequestMessageDto
                {
                    Id = 0,
                    SupportRequestId = supportRequest.Id,
                    SenderUserId = supportRequest.CreatedByUserId,
                    SenderUserName = supportRequest.CreatedByUser?.FullName ?? supportRequest.CreatedByUser?.UserName ?? "Kullanıcı",
                    IsAdminMessage = false,
                    Message = supportRequest.Description,
                    CreatedAt = supportRequest.CreatedAt
                });

                if (!string.IsNullOrWhiteSpace(supportRequest.AdminResponse))
                {
                    messageDtos.Add(new SupportRequestMessageDto
                    {
                        Id = 0,
                        SupportRequestId = supportRequest.Id,
                        SenderUserId = supportRequest.RespondedByUserId ?? 0,
                        SenderUserName = supportRequest.RespondedByUser?.FullName ?? supportRequest.RespondedByUser?.UserName ?? "Admin",
                        IsAdminMessage = true,
                        Message = supportRequest.AdminResponse,
                        CreatedAt = supportRequest.RespondedAt ?? supportRequest.UpdatedAt ?? supportRequest.CreatedAt
                    });
                }
            }

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
                ClosedAt = supportRequest.ClosedAt,
                Messages = messageDtos
            };
        }

        private static SupportRequestMessageDto MapToMessageDto(SupportRequestMessage message)
        {
            return new SupportRequestMessageDto
            {
                Id = message.Id,
                SupportRequestId = message.SupportRequestId,
                SenderUserId = message.SenderUserId,
                SenderUserName = message.SenderUser?.FullName ?? message.SenderUser?.UserName ?? "-",
                IsAdminMessage = message.IsAdminMessage,
                Message = message.Message,
                CreatedAt = message.CreatedAt
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
                SupportRequestStatus.Answered => "Açık", // Eski kayıtlar için ekranda Açık gibi gösterilir.
                SupportRequestStatus.Closed => "Kapatıldı",
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
