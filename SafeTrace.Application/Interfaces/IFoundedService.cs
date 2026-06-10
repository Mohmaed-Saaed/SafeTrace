using SafeTrace.Application.DTOs.Founded;
using SafeTrace.Application.DTOs.Responses;
using SafeTrace.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace SafeTrace.Application.Interfaces
{
    public interface IFoundedService
    {
            
        public Task<ApiResponse<List<FoundPersonListItemDto>>> GetAllAsync(string? search,Gender? gender,int page = 1,int pageSize = 10);


    }
}
