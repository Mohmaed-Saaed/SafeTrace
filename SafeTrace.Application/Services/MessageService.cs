using Microsoft.AspNetCore.SignalR;
using SafeTrace.Application.DTOs.Message;
using SafeTrace.Application.DTOs.Responses;
using SafeTrace.Application.Exceptions;
using SafeTrace.Application.Interfaces.IServices;
using SafeTrace.Domain.Entities;
using SafeTrace.Domain.Interfaces.IUnitOfWork;


namespace SafeTrace.Application.Services
{
    public class MessageService : IMessageService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IChatNotifier _chatNotifier;
        public MessageService (IUnitOfWork unitOfWork, IMapper mapper, IChatNotifier chatNotifier)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _chatNotifier = chatNotifier;
        }

        public async Task<ApiResponse<MessageDto>> SendMessageAsync(SendMessageRequest request, string senderId)
        {
            if(string.IsNullOrWhiteSpace(request.Content) && request.FileType is null)
            {
                throw new BadRequestException("A message must contain either text content or a file attachment.");
            }
            var chat = await _unitOfWork.ChatRepository
                .GetChatWithDetailsAsync(request.ChatId)
                ?? throw new NotFoundException($"Chat with id {request.ChatId} was not found.");
            if(chat.SenderId!= senderId && chat.ReceiverId!= senderId)
            {
                throw new ForbiddenException("You are not a participant of this conversation.");
            }
            var receiverId = chat.SenderId == senderId ? chat.ReceiverId : chat.SenderId;

            var message = new Message
            {
                ChatId = request.ChatId,
                SenderId = senderId,
                ReceiverId = receiverId,
                Content = request.Content,
                FileType = request.FileType,
                FilePath = request.FilePath,
                IsRead = false,
                SendAt = DateTime.UtcNow,
            };

            await _unitOfWork.MessageRepository.CreateAsync(message);
            await _unitOfWork.SaveAsync();

            var messageDto = _mapper.Map<MessageDto>(message);
            await _chatNotifier.SendMessageAsync(receiverId, messageDto);

            return ApiResponse<MessageDto>.Ok(
                messageDto, "message sended succesfully");

        }
        public async Task<ApiResponse<int>> MarkMessagesAsReadAsync(long chatId, string userId)
        {
            var chat = await _unitOfWork.ChatRepository
                .GetChatWithDetailsAsync(chatId)
                ?? throw new NotFoundException($"Chat with id {chatId} was not found.");
            if(chat.SenderId != userId && chat.ReceiverId != userId)
            {
                throw new ForbiddenException(
            "You are not a participant of this conversation.");

            }

            var updatedCount = await _unitOfWork.MessageRepository
                .MarkMessagesAsReadAsync(chatId, userId);

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
