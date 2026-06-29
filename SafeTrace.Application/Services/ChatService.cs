using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SafeTrace.Application.DTOs.Chat;
using SafeTrace.Application.DTOs.Message;
using SafeTrace.Application.DTOs.Responses;
using SafeTrace.Application.Exceptions;
using SafeTrace.Application.Interfaces.IServices;
using SafeTrace.Domain.Common;
using SafeTrace.Domain.Entities;
using Chat = SafeTrace.Domain.Entities.Chat;
using SafeTrace.Domain.Interfaces.IUnitOfWork;
using Serilog.Core;
using System;
using System.Collections.Generic;
using System.Text;
using static SafeTrace.Application.Constants.Permissions;

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

            //var existing = await _unitOfWork.ChatRepository
            //    .GetExistingChatAsync(caseId, currentUserId,caseOwnerId);

            var existing = await _unitOfWork.Repository<Chat>()
            .GetOneAsync(
            c => c.CaseId == caseId &&
            (
                (c.SenderId == currentUserId && c.ReceiverId == caseOwnerId) ||
                (c.SenderId == caseOwnerId && c.ReceiverId == currentUserId)
            ),
            tracked: false);

            if (existing is not null)
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

            await _unitOfWork.Repository<Chat>().CreateAsync(chat);
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

            //var chats = await _unitOfWork.ChatRepository.GetUserChatsAsync(currentUserId);

            var chats = await _unitOfWork.Repository<Chat>()
                .Query(
                    tracked : false,
                    includes: c => c.Messages)
                    .Where(c =>
                (c.SenderId == currentUserId && !c.DeletedBySender) ||
                (c.ReceiverId == currentUserId && !c.DeletedByReceiver)).ToListAsync();
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
            })
            .OrderByDescending(x => x.LastMessageDate);

            _logger.LogInformation(
            "User {UserId} has {Count} chats.",
            currentUserId,
            chats.Count());

            return ApiResponse<IEnumerable<ChatSummaryDto>>.Ok(
                result, "user chats returned");
        }
        public async Task<ApiResponse<ChatDetailsDto>> GetChatDetailsAsync(long chatId, string currentUserId, bool isAdmin)
        {
            _logger.LogInformation(
            "User {UserId} requested chat details for Chat {ChatId}.",
            currentUserId,
            chatId);

            var chat = await _unitOfWork.Repository<Chat>()
                .GetOneAsync(
                    c => c.Id == chatId,
                    tracked: false,
                    c => c.Case,
                    c => c.Sender,
                    c => c.Receiver)
                ?? throw new NotFoundException($"Chat with id {chatId} was not found.");
            if (!isAdmin)
            {
                EnsureParticipant(chat, currentUserId);
            }

            var dto = new ChatDetailsDto
            {
                ChatId = chat.Id,
                CaseId = chat.CaseId,
                CaseTitle = chat.Case.CaseCode,

                CreatedAt = chat.CreatedAt
            };

            if (isAdmin)
            {
                dto.SenderId = chat.SenderId;
                dto.SenderName = $"{chat.Sender.FName} {chat.Sender.LName}";

                dto.ReceiverId = chat.ReceiverId;
                dto.ReceiverName = $"{chat.Receiver.FName} {chat.Receiver.LName}";

                dto.DeletedBySender = chat.DeletedBySender;
                dto.DeletedByReceiver = chat.DeletedByReceiver;
                dto.SenderDeletedAt = chat.SenderDeletedAt;
                dto.ReceiverDeletedAt = chat.ReceiverDeletedAt;
            }
            else
            {
                dto.OtherUserName = chat.SenderId == currentUserId
                    ? $"{chat.Receiver.FName} {chat.Receiver.LName}"
                : $"{chat.Sender.FName} {chat.Sender.LName}";
            }

            _logger.LogInformation(
            "Chat {ChatId} details returned for user {UserId}.",
            chatId,
            currentUserId);

            return ApiResponse<ChatDetailsDto>.Ok(
               dto,
                "Chat details returned successfully.");

        }
        public async Task <ApiResponse<PaginationResponseDto<MessageDto>>> GetPaginatedMessagesAsync(long chatId, string currentUserId,bool isAdmin, int page, int pageSize)
        {
            _logger.LogInformation(
            "User {UserId} requested messages for Chat {ChatId}. Page {Page}, PageSize {PageSize}.",
            currentUserId,
            chatId,
            page,
            pageSize);

            var chat = await _unitOfWork.Repository<Chat>()
                .GetOneAsync(
                    c => c.Id == chatId,
                    tracked: false,
                    c => c.Case,
                    chatId => chatId.Messages)
               ?? throw new NotFoundException($"Chat with id {chatId} was not found.");

            if (!isAdmin)
            {
                EnsureParticipant(chat, currentUserId);
            }
            //var (messages, totalCount) = await _unitOfWork.MessageRepository
            //    .GetPagedMessagesAsync(chatId,currentUserId, page, pageSize);

            IQueryable<Message> query;
            if (isAdmin)
            {
                 query = _unitOfWork.Repository<Message>()
                .Query(false)
                .Where(m => m.ChatId == chatId);
            }
            else
            {
                 query = _unitOfWork.Repository<Message>()
                    .Query(tracked: false)
                    .Where(m => m.ChatId == chatId &&
                        (
                            (m.SenderId == currentUserId && !m.DeletedBySender)
                            ||
                            (m.ReceiverId == currentUserId && !m.DeletedByReceiver)
                        ));
            }
            var totalCount = await query.CountAsync();

            var messages = await query
            .OrderBy(m => m.SendAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();


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

        public async Task<ApiResponse<ChatDetailsDto>> DeleteChatAsync(long chatId, string userId)
        {
            var chat = await _unitOfWork.Repository<Chat>()
                .GetOneAsync(
                c=> c.Id == chatId,
                true,
                c=> c.Sender,
                chatId => chatId.Receiver)
                ?? throw new NotFoundException($"Chat with id {chatId} was not found.");

            EnsureParticipant(chat, userId);

            if(chat.SenderId == userId)
            {
                chat.DeletedBySender = true;
                chat.SenderDeletedAt = DateTime.UtcNow;
            }

            if (chat.ReceiverId == userId) 
            {
                chat.DeletedByReceiver = true;
                chat.ReceiverDeletedAt = DateTime.UtcNow;
            }

            _unitOfWork.Repository<Chat>().Update(chat);
            await _unitOfWork.SaveAsync();

            var name = chat.SenderId == userId
                ? chat.Sender.FName 
                : chat.Receiver.FName;

            _logger.LogInformation(
            "user {name} delete {chatId} chat.",
            name,
            chatId
            );


            return ApiResponse<ChatDetailsDto>.Ok(
               _mapper.Map<ChatDetailsDto>(chat),
               $"{name} delete chat successfully");


        }

        public async Task<ApiResponse<ChatDetailsDto>> DeleteChatByAdminAsync(long chatId)
        {
            var chat = await _unitOfWork.Repository<Chat>()
                .GetOneAsync(
                c=> c.Id == chatId,
                includes: c => c.Messages
                )
                ?? throw new NotFoundException(
            $"Chat with id {chatId} was not found.");

            _unitOfWork.Repository<Chat>().Remove( chat );
            
            await _unitOfWork.SaveAsync();

            return ApiResponse<ChatDetailsDto>.Ok(
               _mapper.Map<ChatDetailsDto>(chat),
               "chat hard deleted successfully");


        }

        public async Task<ApiResponse<PaginationResponseDto<AdminChatsDto>>> GetAllChatsAsync(int page, int pageSize, ChatFilterDto filter)
        {
            var baseQuery = _unitOfWork.Repository<Chat>()
                .Query(
                 false,
                 c => c.CreatedAt,
                OrderBy.Descending,
                null,
                null,
                c => c.Messages,
                c => c.Case);

            if (!string.IsNullOrWhiteSpace(filter.UserId))
                baseQuery = baseQuery.Where(c => c.SenderId == filter.UserId || c.ReceiverId == filter.UserId);

            if (filter.FromDate.HasValue)
                baseQuery = baseQuery.Where(c => c.CreatedAt >= filter.FromDate);

            if (filter.ToDate.HasValue)
                baseQuery = baseQuery.Where(c => c.CreatedAt <= filter.ToDate);

            if (filter.IsDeletedBySender.HasValue)
                baseQuery = baseQuery.Where(c => c.DeletedBySender == filter.IsDeletedBySender);

            if (filter.IsDeletedByReceiver.HasValue)
                baseQuery = baseQuery.Where(c => c.DeletedByReceiver == filter.IsDeletedByReceiver);

            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                baseQuery = baseQuery.Where(c =>
                    c.Messages.Any(m => m.Content.Contains(filter.Search)));
            }

            var totalCount = await baseQuery.CountAsync();

            var chats = await baseQuery
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var items = chats.Select(c => new AdminChatsDto
            {
                ChatId = c.Id,
                CaseId = c.CaseId,
                SenderId = c.SenderId,
                ReceiverId = c.ReceiverId,

                MessagesCount = c.Messages.Count,
                UnreadMessagesCount = c.Messages.Count(m => !m.IsRead),

                CreatedAt = c.CreatedAt,

                LastMessage = c.Messages
                .OrderByDescending(m => m.SendAt)
                .Select(m => m.Content)
                .FirstOrDefault(),

                IsDeletedBySender = c.DeletedBySender,
                IsDeletedByReceiver = c.DeletedByReceiver,

                SenderDeletedAt = c.SenderDeletedAt,
                ReceiverDeletedAt = c.ReceiverDeletedAt,
            }).ToList();

            var result = new PaginationResponseDto<AdminChatsDto>
            {
                Items = items,
                PageNumber = page,
                PageSize = pageSize,
                TotalCount = totalCount
            };

            return ApiResponse<PaginationResponseDto<AdminChatsDto>>
            .Ok(result, "Filtered chats returned");
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
