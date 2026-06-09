using AutoMapper;
using SafeTrace.Application.DTOs.Founded;
using SafeTrace.Application.DTOs.Responses;
using SafeTrace.Application.Interfaces.Services;
using SafeTrace.Domain.Common;
using SafeTrace.Domain.Enums;
using SafeTrace.Domain.Interfaces.IUnitOfWork;
using System;
using System.Collections.Generic;
using System.Text;

namespace SafeTrace.Infrastructure.Service.Founded
{
    public class FoundedService : IFoundedService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        public FoundedService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }
        public async Task<ApiResponse<List<DTOFoundedIndexResponse>>> GetAllAsync(string? search, Gender? gender, int page = 1, int pageSize = 10)
        {


            var foundedPersons = await _unitOfWork.FoundPersonInfoRepository.GetAllAsync(
                    f => string.IsNullOrEmpty(search) || f.Case.FName!.Contains(search)|| f.Case.SName!.Contains(search)
                    && f.Case.Gender == gender,
                        false,f => f.Case.CreatedAt, OrderBy.Descending, page, f => f.Case);


            var result = _mapper.Map<List<DTOFoundedIndexResponse>>(foundedPersons);

            var response = new ApiResponse<List<DTOFoundedIndexResponse>>
            {
                Success = true,
                Message = "Founded persons retrieved successfully",
                Data = result
            };
            return response;

        }
    }
}
