using AutoMapper;
using Microsoft.Extensions.Logging;
using SafeTrace.Application.DTOs.Founded.Request;
using SafeTrace.Application.DTOs.Founded.Response;
using SafeTrace.Application.DTOs.Responses;
using SafeTrace.Application.Interfaces;
using SafeTrace.Domain.Common;
using SafeTrace.Domain.Entities;
using SafeTrace.Domain.Enums;
using SafeTrace.Domain.Interfaces.IUnitOfWork;
using Serilog.Core;
using System;
using System.Collections.Generic;
using System.Text;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace SafeTrace.Infrastructure.Service.Founded
{
    public class FoundedService : IFoundedService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<FoundedService> _logger;
        public FoundedService(IUnitOfWork unitOfWork, IMapper mapper, ILogger<FoundedService> logger)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
        }
        public async Task<ApiResponse<List<FoundPersonListItemDto>>> GetAllAsync(FoundedHeaderQueryDTO query)
        {
            query.Search = string.IsNullOrWhiteSpace(query.Search) ? null : query.Search.Trim();

            var foundedPersons = await _unitOfWork.FoundPersonInfoRepository.GetAllAsync(
                    f => (string.IsNullOrEmpty(query.Search) || f.Case.FName!.Contains(query.Search)|| f.Case.SName!.Contains(query.Search))
                        && ( !query.Gender.HasValue || f.Case.Gender == query.Gender)
                        && (query.AgeCategory == 0 || f.Case.AgeCategory.Id == query.AgeCategory),
                            false,f => f.Case.CreatedAt, OrderBy.Descending, 
                            query.Page, query.PageSize, f => f.Case , f => f.Case.Photos  , f => f.Case.AgeCategory);

            return new ApiResponse<List<FoundPersonListItemDto>>
            {
                Success = true,
                StatusCode = 200,
                Message = "Founded persons retrieved successfully",
                Data = _mapper.Map<List<FoundPersonListItemDto>>(foundedPersons)
            };
        }
    }
}
