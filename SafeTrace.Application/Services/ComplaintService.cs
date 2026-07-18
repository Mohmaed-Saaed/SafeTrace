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
                Content = $"Your complaint has been resolved: {dto.SolutionMessage}",
                Type = NotificationType.ComplaintResolved
            });

            // 2 — Email
            var userEmail = complaint.User.Email!;
            var userName = $"{complaint.User.FName} {complaint.User.LName}";
            var subject = "Your Complaint Has Been Resolved - SafeTrace";
            var body = BuildComplaintResolvedEmailTemplate(userName, dto.SolutionMessage);
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
    
        private static string BuildComplaintResolvedEmailTemplate(string fullName, string solutionMessage)
        {
            return $@"
            <div dir='ltr' style='font-family: ""Segoe UI"", Tahoma, Geneva, Verdana, sans-serif; max-width: 550px; margin: 0 auto; padding: 30px; border: 1px solid #eef2f5; border-radius: 12px; background-color: #ffffff;'>
                <div style='text-align: center; margin-bottom: 25px;'>
                    <h1 style='color: #2b5a8f; font-size: 28px; margin: 0; font-weight: 700;'>SafeTrace</h1>
                    <p style='color: #8c9ba5; font-size: 13px; margin: 5px 0 0 0;'>Smart Missing Persons Tracking System</p>
                </div>
                <hr style='border: 0; border-top: 1px solid #f0f4f8; margin-bottom: 25px;' />
                <h2 style='color: #1e293b; font-size: 20px; margin-top: 0;'>Hello, {fullName}</h2>
                <p style='color: #475569; font-size: 15px; line-height: 1.6;'>We are pleased to inform you that your complaint has been reviewed and resolved by our team.</p>
                <div style='background: #f0fdf4; border: 1px solid #bbf7d0; padding: 20px; border-radius: 10px; margin: 20px 0; color: #166534;'>
                    <strong>Resolution Message:</strong><br/><br/>
                    {solutionMessage}
                </div>
                <p style='color: #475569; font-size: 14px;'>If you have any further questions, feel free to submit a new complaint through the app.</p>
                <hr style='border: 0; border-top: 1px solid #f0f4f8; margin: 20px 0;' />
                <p style='color: #475569; font-size: 14px; margin: 0; font-weight: 600;'>Best regards,<br/><span style='color: #2b5a8f;'>SafeTrace Support Team</span></p>
            </div>";
        }
    }
}
