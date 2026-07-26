using SafeTrace.Application.DTOs.Cases.Request;
using SafeTrace.Application.DTOs.Dashboard.Request;
using SafeTrace.Application.DTOs.Dashboard.Response;
using SafeTrace.Application.DTOs.Responses;
using System;
using System.Collections.Generic;
using System.Text;

namespace SafeTrace.Application.Interfaces.IServices
{
    public interface IDashboardService
    {
        Task<ApiResponse<DashboardDto>> GetDashboardAsync();
        Task<ApiResponse<CasesStatisticsDto>> GetCasesStatisticsAsync();
        Task<ApiResponse<PaginationResponseDto<AuditLogDto>>> GetAuditLogsAsync(AuditLogQueryDto query);
        Task<List<CaseReportDto>> GetAllForReportAsync(
        CasesReportFilterDto filter);


        Task<byte[]> GeneratePdfReportAsync(
            CasesReportFilterDto filter);


        //Task<byte[]> GenerateExcelReportAsync(
        //    CasesFilterBaseDto filter);
    }
}
