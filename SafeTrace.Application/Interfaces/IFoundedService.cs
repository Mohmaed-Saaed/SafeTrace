using SafeTrace.Application.DTOs.Founded.Request;
using SafeTrace.Application.DTOs.Founded.Response;
using SafeTrace.Application.DTOs.Responses;
using SafeTrace.Domain.Entities;
using SafeTrace.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace SafeTrace.Application.Interfaces
{
    public interface IFoundedService
    {
            
        public Task<ApiResponse<List<FoundPersonListItemDto>>> GetAllAsync(FoundedHeaderQueryDTO query);


    }
}
