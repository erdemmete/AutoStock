using AutoStock.Services.Dtos.Common;
using AutoStock.Services.Dtos.ServiceRecords;
using System;
using System.Collections.Generic;
using System.Text;

namespace AutoStock.Services.Interfaces
{
    public interface IServiceRecordService
    {
        Task<ServiceResult<CreateServiceRecordResponse>> CreateAsync(CreateServiceRecordRequest request, int workshopId);
        Task<ServiceResult<ServiceRecordDetailDto>> GetDetailAsync(int serviceRecordId, int workshopId);
        Task<ServiceResult<List<ServiceRecordListItemDto>>> GetListAsync(int workshopId);
        Task<ServiceResult<bool>> UpdateRequestItemAsync(int requestItemId, UpdateServiceRequestItemRequest request, int workshopId);
        Task<ServiceResult<int>> AddRequestItemAsync(int serviceRecordId, CreateServiceRequestItemDto request, int workshopId);
        Task<ServiceResult<bool>> AddOperationAsync(int serviceRecordId, AddServiceOperationRequest request, int workshopId);
        Task<ServiceResult<bool>> CompleteAsync(int serviceRecordId, int workshopId);
        Task<ServiceResult<bool>> UpdateStatusAsync(int serviceRecordId, UpdateServiceRecordStatusRequest request, int workshopId);
    }
}
