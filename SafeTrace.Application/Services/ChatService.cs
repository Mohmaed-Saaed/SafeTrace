using SafeTrace.Application.DTOs.Chat;
using SafeTrace.Application.DTOs.Message;
using SafeTrace.Application.DTOs.Responses;
using SafeTrace.Application.Exceptions;
using SafeTrace.Application.Interfaces.IServices;
using SafeTrace.Domain.Entities;
using SafeTrace.Domain.Interfaces.IUnitOfWork;
using System;
using System.Collections.Generic;
using System.Text;

namespace SafeTrace.Application.Services
{
    public class ChatService : IChatService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public ChatService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<ApiResponse<ChatDetailsDto>> StartOrGetChatAsync(long caseId, string currentUserId)
        {
            var baseCase = await _unitOfWork.BaseCaseRepository
                .GetOneAsync(c => c.Id == caseId, tracked: false)
                ?? throw new NotFoundException($"Case with id {caseId} was not found.");

            var caseOwnerId = baseCase.UserId;

            if(caseOwnerId == currentUserId)
            {
                throw new BadRequestException("You cannot start a conversation on your own case.");
            }

            var existing = await _unitOfWork.ChatRepository
                .GetExistingChatAsync(caseId, currentUserId,caseOwnerId);

            if(existing is not null)
            {
                return ApiResponse<ChatDetailsDto>.Ok(
                    _mapper.Map<ChatDetailsDto>(existing),
                    "Existing conversation returned.");
            }
            var chat = new Chat
            {
                CaseId = caseId,
                SenderId = currentUserId,
                ReceiverId = caseOwnerId,
                CreatedAt = DateTime.UtcNow,
            };

            await _unitOfWork.ChatRepository.CreateAsync(chat);
            await _unitOfWork.SaveAsync();

            return ApiResponse<ChatDetailsDto>.Ok(
                _mapper.Map<ChatDetailsDto>(chat),
                "Conversation created successfully.");

        }
        public async Task<ApiResponse<IEnumerable<ChatSummaryDto>>> GetUserChatsAsync(string currentUserId)
        {
            var chats = await _unitOfWork.ChatRepository.GetUserChatsAsync(currentUserId);
            var result = chats.Select(c => new ChatSummaryDto
            {
                ChatId = c.Id,
                CaseId = c.CaseId,
                OtherUserId = c.SenderId == currentUserId ? c.ReceiverId : c.SenderId,
                LastMessage = c.Messages
                .OrderByDescending(m => m.SendAt)
                .Select(m => m.Content)
                .FirstOrDefault(),

                LastMessageDate = c.Messages
                .OrderByDescending(m => m.SendAt)
                .Select(m => (DateTime?)m.SendAt)
                .FirstOrDefault(),

                UnreadCount = c.Messages.Count(m =>
                !m.IsRead && m.ReceiverId == currentUserId)
            });
            return ApiResponse<IEnumerable<ChatSummaryDto>>.Ok(
                result, "user chats returned");
        }
        public async Task<ApiResponse<ChatDetailsDto>> GetChatDetailsAsync(long chatId, string currentUserId)
        {
            var chat = await _unitOfWork.ChatRepository.GetChatWithDetailsAsync(chatId)
                ?? throw new NotFoundException($"Chat with id {chatId} was not found.");
            EnsureParticipant(chat, currentUserId);

            return ApiResponse<ChatDetailsDto>.Ok(
                _mapper.Map<ChatDetailsDto>(chat),
                "chat detailes returned");

        }
        public async Task<ApiResponse<PaginatedMessagesDto>> GetPaginatedMessagesAsync(long chatId, string currentUserId, int page, int pageSize)
        {
            var chat = await _unitOfWork.ChatRepository.GetChatWithDetailsAsync(chatId)
               ?? throw new NotFoundException($"Chat with id {chatId} was not found.");
            EnsureParticipant(chat, currentUserId);

            var (messages, totalCount) = await _unitOfWork.MessageRepository
                .GetPagedMessagesAsync(chatId, page, pageSize);
            var result = new PaginatedMessagesDto
            {
                Messages = _mapper.Map<IEnumerable<MessageDto>>(messages),
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
            return ApiResponse<PaginatedMessagesDto>.Ok(result,
                "paged messages returned");


        }

        private static void EnsureParticipant(Chat chat, string userId)
        {
            if (chat.SenderId != userId && chat.ReceiverId != userId) { 
                throw new ForbiddenException("You are not a participant of this conversation.");
            }
        }

    }
}
