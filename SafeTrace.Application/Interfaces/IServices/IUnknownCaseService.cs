using SafeTrace.Application.DTOs.Responses;
using SafeTrace.Application.DTOs.UnKnownDtos;
using SafeTrace.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace SafeTrace.Application.Interfaces.IServices
{
    public interface IUnknownCaseService
    {
        Task<ApiResponse<string>> CreateUnknownCaseAsync(CreateUnknownDto dto,string userId);
        //Task<ApiResponse<List<GetUnknownDto>>> GetApprovedCasesAsync();
        Task<ApiResponse<string>> ApproveAsync(long id);
        Task<ApiResponse<IEnumerable<GetUnknownDto>>> GetAllApprovedAsync();
        Task<ApiResponse<string>> RejectAsync(long id);
        Task<ApiResponse<PagedResponse<GetUnknownDto>>> GetCasesAsync(UnKnownCaseFilterDto filter);
    }
}
