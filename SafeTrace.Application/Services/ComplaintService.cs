using SafeTrace.Application.Constants;
using SafeTrace.Application.DTOs.Complaints;
using SafeTrace.Application.DTOs.Complaints.Request;
using SafeTrace.Application.DTOs.Complaints.Response;
using SafeTrace.Application.DTOs.NotificationDTOS;
using SafeTrace.Application.DTOs.Responses;
using SafeTrace.Application.Exceptions;
using SafeTrace.Application.Interfaces.IServices.INotificationSewrvice;
using SafeTrace.Domain.Enums;


namespace SafeTrace.Application.Services
{
    public class ComplaintService : IComplaintService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly INotificationServices _notificationService;
        private readonly IEmailService _emailService;

        public ComplaintService(
            IUnitOfWork unitOfWork,
            INotificationServices notificationService,
            IEmailService emailService)
        {
            _unitOfWork = unitOfWork;
            _notificationService = notificationService;
            _emailService = emailService;
        }

        public async Task<PaginationResponseDto<ComplaintResponseDto>> GetAllAsync(ComplaintFilterDto filter)
        {
            var query = _unitOfWork.Repository<Complaint>()
                .Query(tracked: false, includes: c => c.User);

            if (!string.IsNullOrEmpty(filter.CaseCode))
                query = query.Where(c => c.CaseCode == filter.CaseCode);

            if (filter.Status.HasValue)
                query = query.Where(c => c.ComplaintStatus == filter.Status.Value);

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(c => c.CreatedAt)
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(c => new ComplaintResponseDto
                {
                    Id = c.Id,
                    UserId = c.UserId,
                    UserEmail = c.User.Email!,
                    CaseCode = c.CaseCode,
                    Message = c.Message,
                    SolutionMessage = c.SolutionMessage,
                    ComplaintStatus = c.ComplaintStatus,
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
                UserEmail = complaint.User.Email!,
                CaseCode = complaint.CaseCode,
                Message = complaint.Message,
                SolutionMessage = complaint.SolutionMessage,
                ComplaintStatus = complaint.ComplaintStatus,
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

        public async Task ResolveAsync(long id, ResolveComplaintDto dto)
        {
            var complaint = await _unitOfWork.Repository<Complaint>()
                .GetOneAsync(c => c.Id == id, tracked: true, c => c.User);

            if (complaint is null)
                throw new NotFoundException($"Complaint with id {id} not found.");

            if (complaint.ComplaintStatus == ComplaintStatus.Solved)
                throw new BadRequestException("This complaint is already solved.");

            if (string.IsNullOrWhiteSpace(dto.SolutionMessage))
                throw new BadRequestException("Solution message is required.");

            complaint.SolutionMessage = dto.SolutionMessage;
            complaint.ComplaintStatus = ComplaintStatus.Solved;
            await _unitOfWork.SaveAsync();

            // 1 — In-app Notification
            await _notificationService.SendNotificationAsync(new SendNotificationDTO
            {
                UserId = complaint.UserId,
                Content = $"تم حل شكواك: {dto.SolutionMessage}",
                Type = NotificationType.System
            });

            // 2 — Email
            var userEmail = complaint.User.Email!;
            var userName = $"{complaint.User.FName} {complaint.User.LName}";
            var subject = "تم حل شكواك - منصة لقاء";
            var body = EmailTemplates.BuildComplaintResolvedTemplate(userName, dto.SolutionMessage);
            await _emailService.SendEmailAsync(userEmail, subject, body);
        }

        public async Task<ComplaintStatisticsDto> GetStatisticsAsync()
        {
            var query = _unitOfWork.Repository<Complaint>().Query(tracked: false);

            var stats = await query
                .GroupBy(x => 1)
                .Select(g => new ComplaintStatisticsDto
                {
                    Total = g.Count(),
                    Solved = g.Count(c => c.ComplaintStatus == Domain.Enums.ComplaintStatus.Solved),
                    UnSolved = g.Count(c => c.ComplaintStatus == Domain.Enums.ComplaintStatus.UnSolved)
                })
                .FirstOrDefaultAsync();

            return stats ?? new ComplaintStatisticsDto();
        }
    
        
    }
}
