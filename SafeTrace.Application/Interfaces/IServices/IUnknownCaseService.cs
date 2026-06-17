using SafeTrace.Application.DTOs;
using SafeTrace.Application.DTOs.Responses;
using System;
using System.Collections.Generic;
using System.Text;

namespace SafeTrace.Application.Interfaces.IServices
{
    public interface IUnknownCaseService
    {
        Task<ApiResponse<string>> CreateUnknownCaseAsync(CreateUnknownDto dto); //, string userId
    }
}
