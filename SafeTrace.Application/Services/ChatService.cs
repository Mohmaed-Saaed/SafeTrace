using Microsoft.Extensions.Logging;
using SafeTrace.Application.DTOs.Chat;
using SafeTrace.Application.DTOs.Message;
using SafeTrace.Application.DTOs.Responses;
using SafeTrace.Application.Exceptions;
using SafeTrace.Application.Interfaces.IServices;
using SafeTrace.Domain.Entities;
using SafeTrace.Domain.Interfaces.IUnitOfWork;
using Serilog.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace SafeTrace.Application.Services
{
    public class ChatService : IChatService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<ChatService> _logger;

        public ChatService(IUnitOfWork unitOfWork, IMapper mapper, ILogger<ChatService> logger)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<ApiResponse<ChatDetailsDto>> StartOrGetChatAsync(long caseId, string currentUserId)
        {
            _logger.LogInformation(
        "User {UserId} is starting or retrieving a chat for case {CaseId}.",
        currentUserId,
        caseId);

            var baseCase = await _unitOfWork.Repository<Case>()
                .GetOneAsync(c => c.Id == caseId, tracked: false)
                ?? throw new NotFoundException($"Case with id {caseId} was not found.");

            var caseOwnerId = baseCase.UserId;

            if(caseOwnerId == currentUserId)
            {
                _logger.LogWarning(
            "User {UserId} attempted to start a chat on their own case {CaseId}.",
            currentUserId,
            caseId);


                throw new BadRequestException("You cannot start a conversation on your own case.");
            }

            var existing = await _unitOfWork.ChatRepository
                .GetExistingChatAsync(caseId, currentUserId,caseOwnerId);

            if(existing is not null)
            {
                _logger.LogInformation(
            "Existing chat {ChatId} returned for user {UserId}.",
            existing.Id,
            currentUserId);

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

            _logger.LogInformation(
            "New chat {ChatId} created between {SenderId} and {ReceiverId}.",
            chat.Id,
            chat.SenderId,
            chat.ReceiverId);

            return ApiResponse<ChatDetailsDto>.Ok(
                _mapper.Map<ChatDetailsDto>(chat),
                "Conversation created successfully.");

        }
        public async Task<ApiResponse<IEnumerable<ChatSummaryDto>>> GetUserChatsAsync(string currentUserId)
        {
            _logger.LogInformation(
            "Fetching chats for user {UserId}.",
            currentUserId);

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

            _logger.LogInformation(
            "User {UserId} has {Count} chats.",
            currentUserId,
            chats.Count());

            return ApiResponse<IEnumerable<ChatSummaryDto>>.Ok(
                result, "user chats returned");
        }
        public async Task<ApiResponse<ChatDetailsDto>> GetChatDetailsAsync(long chatId, string currentUserId)
        {
            _logger.LogInformation(
            "User {UserId} requested chat details for Chat {ChatId}.",
            currentUserId,
            chatId);

            var chat = await _unitOfWork.ChatRepository.GetChatWithDetailsAsync(chatId)
                ?? throw new NotFoundException($"Chat with id {chatId} was not found.");
            EnsureParticipant(chat, currentUserId);

            _logger.LogInformation(
            "Chat {ChatId} details returned for user {UserId}.",
            chatId,
            currentUserId);

            return ApiResponse<ChatDetailsDto>.Ok(
                _mapper.Map<ChatDetailsDto>(chat),
                "chat detailes returned");

        }
        public async Task <ApiResponse<PaginationResponseDto<MessageDto>>> GetPaginatedMessagesAsync(long chatId, string currentUserId, int page, int pageSize)
        {
            _logger.LogInformation(
            "User {UserId} requested messages for Chat {ChatId}. Page {Page}, PageSize {PageSize}.",
            currentUserId,
            chatId,
            page,
            pageSize);

            var chat = await _unitOfWork.ChatRepository.GetChatWithDetailsAsync(chatId)
               ?? throw new NotFoundException($"Chat with id {chatId} was not found.");
            EnsureParticipant(chat, currentUserId);

            var (messages, totalCount) = await _unitOfWork.MessageRepository
                .GetPagedMessagesAsync(chatId, page, pageSize);

            _logger.LogInformation(
            "Returned {Count} messages out of {Total} for Chat {ChatId}.",
             messages.Count(),
             totalCount,
             chatId);

            var result = new PaginationResponseDto<MessageDto>
            {
                Items = _mapper.Map<List<MessageDto>>(messages),
                TotalCount = totalCount,
                PageNumber = page,
                PageSize = pageSize
            };
            return ApiResponse <PaginationResponseDto<MessageDto>>.Ok(result,
                "paged messages returned");


        }

        private void EnsureParticipant(Chat chat, string userId)
        {
            if (chat.SenderId != userId && chat.ReceiverId != userId) {
                _logger.LogWarning(
            "Unauthorized access attempt. User {UserId} tried to access Chat {ChatId}",
            userId,
            chat.Id);

                throw new ForbiddenException("You are not a participant of this conversation.");
            }
        }

    }
}
