using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using SafeTrace.Application.DTOs.Message;
using SafeTrace.Application.DTOs.Responses;
using SafeTrace.Application.Exceptions;
using SafeTrace.Application.Interfaces.IServices;
using SafeTrace.Domain.Entities;
using SafeTrace.Domain.Enums;
using SafeTrace.Domain.Interfaces.IUnitOfWork;


namespace SafeTrace.Application.Services
{
    public class MessageService : IMessageService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IChatNotifier _chatNotifier;
        private readonly ILogger<MessageService> _logger;
        private readonly IFileStorageService _fileStorageService;
        public MessageService (IUnitOfWork unitOfWork, IMapper mapper,
            IChatNotifier chatNotifier, ILogger<MessageService> logger,
            IFileStorageService fileStorageService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _chatNotifier = chatNotifier;
            _logger = logger;
            _fileStorageService = fileStorageService;
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
                throw new BadRequestException("A message must contain either text content or a file attachment.");
            }

            var chat = await _unitOfWork.ChatRepository
                .GetChatWithDetailsAsync(request.ChatId)
                ?? throw new NotFoundException($"Chat with id {request.ChatId} was not found.");

            if(chat.SenderId!= senderId && chat.ReceiverId!= senderId)
            {
                _logger.LogWarning(
                "Unauthorized message attempt by User {SenderId} on Chat {ChatId}.",
                senderId,
                request.ChatId);

                throw new ForbiddenException("You are not a participant of this conversation.");
            }
            var receiverId = chat.SenderId == senderId ? chat.ReceiverId : chat.SenderId;
            string? filePath = null;
            FileType? fileType=null;

            if (request.File != null && request.File.Length>0)

            {
                if (string.IsNullOrEmpty(request.File.FileName))
                    throw new BadRequestException("Invalid file.");

                var extension = Path.GetExtension(request.File.FileName).ToLower();

                if (extension is not (".jpg" or ".jpeg" or ".png" or ".webp"))
                {
                    throw new BadRequestException("Only image files are allowed.");
                }
                filePath = await _fileStorageService
                    .SaveFileAsync(request.File, "chat");
                fileType = FileType.Image;
                
            }

            var message = new Message
            {
                ChatId = request.ChatId,
                SenderId = senderId,
                ReceiverId = receiverId,
                Content = request.Content,
                FileType =fileType,
                FilePath = filePath,
                IsRead = false,
                SendAt = DateTime.UtcNow,
            };

            await _unitOfWork.MessageRepository.CreateAsync(message);
            await _unitOfWork.SaveAsync();

            _logger.LogInformation(
            "Message {MessageId} sent from {SenderId} to {ReceiverId} in Chat {ChatId}.",
            message.Id,
            senderId,
            receiverId,
            request.ChatId);

            var messageDto = _mapper.Map<MessageDto>(message);
            await _chatNotifier.SendMessageAsync(receiverId, messageDto);

            return ApiResponse<MessageDto>.Ok(
                messageDto, "message sended succesfully");

        }
        public async Task<ApiResponse<int>> MarkMessagesAsReadAsync(long chatId, string userId)
        {
            _logger.LogInformation(
            "User {UserId} is marking messages as read in Chat {ChatId}.",
            userId,
            chatId);

            var chat = await _unitOfWork.ChatRepository
                .GetChatWithDetailsAsync(chatId)
                ?? throw new NotFoundException($"Chat with id {chatId} was not found.");

            if(chat.SenderId != userId && chat.ReceiverId != userId)
            {
                _logger.LogWarning(
                 "Unauthorized read attempt by User {UserId} on Chat {ChatId}.",
                userId,
                chatId);

                throw new ForbiddenException(
                "You are not a participant of this conversation.");

            }

            var updatedCount = await _unitOfWork.MessageRepository
                .MarkMessagesAsReadAsync(chatId, userId);

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
                ? $"{updatedCount} messages marked as read"
                : "No unread messages found"
            };
        }
    }
}
