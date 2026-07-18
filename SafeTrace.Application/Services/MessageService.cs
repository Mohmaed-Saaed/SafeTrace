using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SafeTrace.Application.DTOs.Message;
using SafeTrace.Application.DTOs.Responses;
using SafeTrace.Application.Exceptions;
using SafeTrace.Application.Interfaces.IServices;
using SafeTrace.Domain.Entities;
using Chat = SafeTrace.Domain.Entities.Chat;
using SafeTrace.Domain.Enums;
using SafeTrace.Domain.Interfaces.IUnitOfWork;
using static SafeTrace.Application.Constants.Permissions;
using SafeTrace.Application.Interfaces.IServices.INotificationSewrvice;
using SafeTrace.Application.DTOs.NotificationDTOS;
using SafeTrace.Application.Constants;


namespace SafeTrace.Application.Services
{
    public class MessageService : IMessageService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IChatNotifier _chatNotifier;
        private readonly ILogger<MessageService> _logger;
        private readonly IFileStorageService _fileStorageService;
        private readonly INotificationServices _notificationServices;
        private readonly IEmailService _emailService;

        public MessageService(IUnitOfWork unitOfWork, IMapper mapper,
            IChatNotifier chatNotifier, ILogger<MessageService> logger,
            IFileStorageService fileStorageService, INotificationServices notificationServices,
            IEmailService emailService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _chatNotifier = chatNotifier;
            _logger = logger;
            _fileStorageService = fileStorageService;
            _notificationServices = notificationServices;
            _emailService = emailService;
        }


        public async Task<ApiResponse<MessageDto>> SendMessageAsync(SendMessageRequest request, string senderId)
        {
            _logger.LogInformation(
            "User {SenderId} is sending a message to Chat {ChatId}.",
            senderId,
            request.ChatId);

            if (string.IsNullOrWhiteSpace(request.Content) && request.File is null)
            {
                _logger.LogWarning(
                "Invalid message attempt by User {SenderId} in Chat {ChatId}. No content or file.",
                senderId,
                request.ChatId);
                throw new BadRequestException("يجب أن تحتوي الرسالة على نص أو ملف مرفق.");
            }

            var chat = await _unitOfWork.Repository<Chat>()
                .GetOneAsync(
                    c => c.Id == request.ChatId,
                    tracked: false,
                    c => c.Case,
                    chatId => chatId.Messages,
                    c => c.Sender,
                    c => c.Receiver)
                ?? throw new NotFoundException($"لم يتم العثور على المحادثة.");

            if (chat.SenderId != senderId && chat.ReceiverId != senderId)
            {
                _logger.LogWarning(
                "Unauthorized message attempt by User {SenderId} on Chat {ChatId}.",
                senderId,
                request.ChatId);

                throw new ForbiddenException("ليس لديك صلاحية لإرسال رسائل في هذه المحادثة.");
            }
            var receiverId = chat.SenderId == senderId ? chat.ReceiverId : chat.SenderId;
            string? filePath = null;
            FileType? fileType = null;

            if (request.File != null && request.File.Length > 0)

            {
                if (string.IsNullOrEmpty(request.File.FileName))
                    throw new BadRequestException("الملف المرفق غير صالح.");

                var extension = Path.GetExtension(request.File.FileName).ToLowerInvariant();

                fileType = extension switch
                {
                    ".jpg" or ".jpeg" or ".png" or ".webp" => FileType.Image,
                    ".mp4" or ".mov" or ".webm" => FileType.Video,
                    _ => throw new BadRequestException("نوع الملف غير مدعوم.")
                };

                filePath = await _fileStorageService
                    .SaveFileAsync(request.File, "chat");
            }

            var message = new Message
            {
                ChatId = request.ChatId,
                SenderId = senderId,
                ReceiverId = receiverId,
                Content = request.Content,
                FileType = fileType,
                FilePath = filePath,
                IsRead = false,
                SendAt = DateTime.UtcNow,
            };

            await _unitOfWork.Repository<Message>().CreateAsync(message);
            await _unitOfWork.SaveAsync();

            _logger.LogInformation(
            "Message {MessageId} sent from {SenderId} to {ReceiverId} in Chat {ChatId}.",
            message.Id,
            senderId,
            receiverId,
            request.ChatId);

            var messageDto = _mapper.Map<MessageDto>(message);

            _logger.LogInformation(
                "API SendMessage => SendAt={SendAt}, Kind={Kind}",
             messageDto.SendAt,
                 messageDto.SendAt.Kind);

            await _chatNotifier.SendMessageAsync(messageDto);

            string notificationContent;

            var sender = chat.SenderId == senderId
             ? chat.Sender
            : chat.Receiver;

            var senderName = $"{sender.FName} {sender.LName}";

            if (!string.IsNullOrWhiteSpace(request.Content))
            {
                notificationContent = $"{senderName}: {request.Content}";
            }
            else if (fileType == FileType.Image)
            {
                notificationContent = $"{senderName} أرسل إليك صورة.";
            }
            else if (fileType == FileType.Video)
            {
                notificationContent = $"{senderName} أرسل إليك فيديو.";
            }
            else
            {
                notificationContent = "لديك رسالة جديدة.";
            }

            await _notificationServices.SendNotificationAsync(new SendNotificationDTO
            {
                UserId = receiverId,
                Content = notificationContent,
                Type = NotificationType.Message,
                NotificationDirectLink = $"/chat/chat/{request.ChatId}"

            });
            var receiver = chat.SenderId == senderId
                ? chat.Receiver
                : chat.Sender;
            var receiverEmail = receiver.Email;

            var emailBody = EmailTemplates.BuildArabicNewMessageEmailTemplate(
                receiverName: receiver.FName,
                senderName: senderName,
                messagePreview: string.IsNullOrWhiteSpace(request.Content)
                ? "📎 ملف مرفق"
                : request.Content,
                chatLink: $"https://leqaaweb.runasp.net/chat/chat/{request.ChatId}");

            await _emailService.SendEmailAsync(receiverEmail, "رسالة جديدة من SafeTrace", emailBody);

            return ApiResponse<MessageDto>.Ok(
            messageDto, "تم إرسال الرسالة بنجاح.");

        }
        public async Task<ApiResponse<int>> MarkMessagesAsReadAsync(long chatId, string userId)
        {
            _logger.LogInformation(
            "User {UserId} is marking messages as read in Chat {ChatId}.",
            userId,
            chatId);

            var chat = await _unitOfWork.Repository<Chat>()
                .GetOneAsync(
                    c => c.Id == chatId,
                    tracked: false,
                    c => c.Case,
                    chatId => chatId.Messages)
                ?? throw new NotFoundException("لم يتم العثور على المحادثة.");

            if (chat.SenderId != userId && chat.ReceiverId != userId)
            {
                _logger.LogWarning(
                 "Unauthorized read attempt by User {UserId} on Chat {ChatId}.",
                userId,
                chatId);

                throw new ForbiddenException("ليس لديك صلاحية للوصول إلى هذه المحادثة.");

            }

            var messages = await _unitOfWork.Repository<Message>()
                .Query()
                .Where(m =>
                 m.ChatId == chatId &&
                m.ReceiverId == userId &&
                !m.IsRead)
                .ToListAsync();

            foreach (var message in messages)
            {
                message.IsRead = true;
            }

            await _unitOfWork.SaveAsync();

            var updatedCount = messages.Count;

            _logger.LogInformation(
            "User {UserId} marked {Count} messages as read in Chat {ChatId}.",
            userId,
            updatedCount,
            chatId);

            return new ApiResponse<int>
            {
                Success = true,
                Data = updatedCount,
                Message = updatedCount > 0
                ? $"تم تحديد {updatedCount} رسالة كمقروءة."
                : "لا توجد رسائل غير مقروءة."
            };
        }

        public async Task<ApiResponse<MessageDto>> DeleteMessageAsync(long messageId, string userId)
        {
            var message = await _unitOfWork.Repository<Message>()
                .GetByIdAsync(messageId)
                ?? throw new NotFoundException("لم يتم العثور على الرسالة.");

            EnsureParticipant(message, userId);

            if (message.SenderId == userId)
            {
                message.DeletedBySender = true;
                message.SenderDeletedAt = DateTime.UtcNow;
            }

            if (message.ReceiverId == userId)
            {
                message.DeletedByReceiver = true;
                message.ReceiverDeletedAt = DateTime.UtcNow;
            }

            _unitOfWork.Repository<Message>().Update(message);

            await _unitOfWork.SaveAsync();

            return ApiResponse<MessageDto>.Ok(
                _mapper.Map<MessageDto>(message),
                "تم حذف الرسالة بنجاح.");
        }

        public async Task<ApiResponse<MessageDto>> DeleteMessageForEveryoneAsync(long messageId, string userId)
        {
            _logger.LogInformation("delete for every one started");
            var message = await _unitOfWork.Repository<Message>()
                .GetByIdAsync(messageId)
                ?? throw new NotFoundException("لم يتم العثور على الرسالة.");
            _logger.LogInformation(
            "Delete For Everyone => Message SenderId: {SenderId}, Current UserId: {UserId}",
            message.SenderId,
            userId
                );

            if (message.SenderId != userId)
            {
                throw new ForbiddenException(
                "يمكن لمرسل الرسالة فقط حذفها لدى الجميع.");
            }

            message.IsDeletedForEveryone = true;
            message.ForEveryoneDeletedAt = DateTime.UtcNow;

            _unitOfWork.Repository<Message>().Update(message);
            await _unitOfWork.SaveAsync();

            await _chatNotifier.NotifyMessageDeletedForEveryone(message.ChatId, message.Id);

            return ApiResponse<MessageDto>.Ok(
                _mapper.Map<MessageDto>(message),
                "تم حذف الرسالة لدى الجميع بنجاح.");
        }

        private void EnsureParticipant(Message message, string userId)
        {
            if (message.SenderId != userId && message.ReceiverId != userId)
            {
                _logger.LogWarning(
            "Unauthorized access attempt. User {UserId} tried to send message {MessageId}",
            userId,
            message.Id);

                throw new ForbiddenException("ليس لديك صلاحية لحذف هذه الرسالة.");
            }
        }
    }
}
