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
        Task<ApiResponse<PaginationResponseDto<GetUnknownDto>>>
    GetAllApprovedAsync(int pageNumber = 1, int pageSize = 10);
        Task<ApiResponse<string>> RejectAsync(long id);
        Task<ApiResponse<PaginationResponseDto<GetUnknownDto>>>
            GetCasesAsync(UnknownFilterUsingbyUserDto filter);
        Task<ApiResponse<string>> UpdateUnknownCaseAsync(long id, UpdateUnkownCaseDto dto, string userId);
        Task<ApiResponse<GetUnknownDto>> GetDetailsAsync(long id);
        Task<ApiResponse<string>> DeleteUnKnownCase(long id, string userId);
        Task<ApiResponse<string>> FoundUnKnownCase(long id, string userId);
      
        Task<ApiResponse<PaginationResponseDto<UnKnownCaseFilterDto>>>
            GetAllWithFilteration(UnKnownCaseFilterStatusDto filter);

        Task<ApiResponse<List<GetMyUnknownnCasesDto>>> GetMyCasesAsync(string userId);

        Task<ApiResponse<string>> HardDeleteUnknownCase(long id);

    }
}
