using System;
using System.Collections.Generic;
using System.Text;
using SafeTrace.Application.DTOs.Complaints;
using SafeTrace.Application.DTOs.Complaints.Request;
using SafeTrace.Application.DTOs.Complaints.Response;
using SafeTrace.Application.DTOs.Responses;
using SafeTrace.Domain.Enums;

namespace SafeTrace.Application.Interfaces.IServices
{
    public interface IComplaintService
    {
        Task<PaginationResponseDto<ComplaintResponseDto>> GetAllAsync(ComplaintFilterDto filter);
        Task<ComplaintResponseDto> GetByIdAsync(long id);
        Task<ComplaintResponseDto> CreateAsync(string userId, CreateComplaintDto dto);
        Task DeleteAsync(long id);
        Task<ComplaintStatisticsDto> GetStatisticsAsync();
        Task ResolveAsync(long id, ResolveComplaintDto dto);
        Task<List<ComplaintResponseDto>> GetAllForReportAsync(ComplaintFilterDto filter);
        Task<byte[]> GeneratePdfReportAsync(ComplaintFilterDto filter);
        Task<byte[]> GenerateExcelReportAsync(
        ComplaintFilterDto filter);
    }
}