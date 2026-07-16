using SafeTrace.Application.DTOs.Complaints;
using SafeTrace.Application.DTOs.Responses;
using SafeTrace.Application.Exceptions;
using SafeTrace.Application.Interfaces.IServices;
using SafeTrace.Domain.Entities;
using SafeTrace.Domain.Interfaces.IUnitOfWork;

namespace SafeTrace.Infrastructure.Services
{
    public class ComplaintService : IComplaintService
    {
        private readonly IUnitOfWork _unitOfWork;

        public ComplaintService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<PaginationResponseDto<ComplaintResponseDto>> GetAllAsync(ComplaintFilterDto filter)
        {
            var query = _unitOfWork.Repository<Complaint>()
                .Query(tracked: false, includes: c => c.User);

            // Filtration
            if (!string.IsNullOrEmpty(filter.UserId))
                query = query.Where(c => c.UserId == filter.UserId);

            if (!string.IsNullOrEmpty(filter.CaseCode))
                query = query.Where(c => c.CaseCode == filter.CaseCode);

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(c => c.CreatedAt)
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(c => new ComplaintResponseDto
                {
                    Id = c.Id,
                    UserId = c.UserId,
                    UserName = c.User.UserName ?? c.User.Email!,
                    CaseCode = c.CaseCode,
                    Message = c.Message,
                    CreatedAt = c.CreatedAt
                })
                .ToListAsync();

            return new PaginationResponseDto<ComplaintResponseDto>
            {
                Items = items,
                PageNumber = filter.PageNumber,
                PageSize = filter.PageSize,
                TotalCount = totalCount
            };
        }

        public async Task<ComplaintResponseDto> GetByIdAsync(long id)
        {
            var complaint = await _unitOfWork.Repository<Complaint>()
                .GetOneAsync(c => c.Id == id, tracked: false, c => c.User);

            if (complaint is null)
                throw new NotFoundException($"Complaint with id {id} not found.");

            return new ComplaintResponseDto
            {
                Id = complaint.Id,
                UserId = complaint.UserId,
                UserName = complaint.User.UserName ?? complaint.User.Email!,
                CaseCode = complaint.CaseCode,
                Message = complaint.Message,
                CreatedAt = complaint.CreatedAt
            };
        }

        public async Task<ComplaintResponseDto> CreateAsync(string userId, CreateComplaintDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Message))
                throw new BadRequestException("Message is required.");

            var complaint = new Complaint
            {
                UserId = userId,
                CaseCode = dto.CaseCode,
                Message = dto.Message,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Repository<Complaint>().CreateAsync(complaint);
            await _unitOfWork.SaveAsync();

            // Reload with User
            return await GetByIdAsync(complaint.Id);
        }

        public async Task DeleteAsync(long id)
        {
            var complaint = await _unitOfWork.Repository<Complaint>().GetByIdAsync(id);

            if (complaint is null)
                throw new NotFoundException($"Complaint with id {id} not found.");

            _unitOfWork.Repository<Complaint>().Remove(complaint);
            await _unitOfWork.SaveAsync();
        }
    }
}