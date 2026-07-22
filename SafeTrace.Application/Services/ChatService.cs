using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SafeTrace.Application.DTOs.Chat;
using SafeTrace.Application.DTOs.Message;
using SafeTrace.Application.DTOs.Responses;
using SafeTrace.Application.Exceptions;
using SafeTrace.Application.Interfaces.IServices;
using SafeTrace.Domain.Common;
using SafeTrace.Domain.Entities;
using SafeTrace.Domain.Interfaces.IUnitOfWork;
using Serilog.Core;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using static SafeTrace.Application.Constants.Permissions;
using Chat = SafeTrace.Domain.Entities.Chat;

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

        public async Task<ApiResponse<StartChatContextDto>> GetStartChatContextAsync(long caseId, string currentUserId)
        {
            var baseCase = await _unitOfWork.Repository<Case>()
                .Query(tracked: false)
                .Include(c => c.User)
                .Include(c => c.CaseFiles)
                .FirstOrDefaultAsync(c => c.Id == caseId)
                ?? throw new NotFoundException("الحاله غير موجودة");

            if (baseCase.UserId == currentUserId)
                throw new BadRequestException("لا يمكنك بدء محادثة على حالتك.");

            var existingChat = await _unitOfWork.Repository<Chat>()
        .GetOneAsync(c =>
            c.CaseId == caseId &&
            (
                (c.SenderId == currentUserId && c.ReceiverId == baseCase.UserId) ||
                (c.SenderId == baseCase.UserId && c.ReceiverId == currentUserId)
            ),
            tracked: false);

            var primaryImage = baseCase.CaseFiles.FirstOrDefault(f => f.IsPrimary)?.ImagePath;

            var dto = new StartChatContextDto
            {
                CaseId = baseCase.Id,
                CaseTitle = $"{baseCase.FName} {baseCase.SName} {baseCase.TName} {baseCase.LName}",
                CaseType = baseCase.CaseType,

                CaseImage = primaryImage,

                ParticipantName = $"{baseCase.User.FName} {baseCase.User.LName}",
                ParticipantImage = baseCase.User.ProfileImage,

                ChatExists = existingChat !=null,
                ChatId = existingChat?.Id
            };

            return ApiResponse<StartChatContextDto>.Ok(dto);

        }
        public async Task<ApiResponse<ChatDetailsDto>> StartOrGetChatAsync(long caseId, string currentUserId)
        {
            _logger.LogInformation(
        "User {UserId} is starting or retrieving a chat for case {CaseId}.",
        currentUserId,
        caseId);

            var baseCase = await _unitOfWork.Repository<Case>()
                .GetOneAsync(c => c.Id == caseId, tracked: false)
                ?? throw new NotFoundException($"{caseId} لم يتم العثور على الحالة المطلوبة.");

            var caseOwnerId = baseCase.UserId;

            if (caseOwnerId == currentUserId)
            {
                _logger.LogWarning(
                    "User {UserId} attempted to start a chat on their own case {CaseId}.",
                    currentUserId,
                    caseId);

                throw new BadRequestException("لا يمكنك بدء محادثة على حالتك الخاصة.");
            }

            bool isNewChat = false;

            var chat = await _unitOfWork.Repository<Chat>()
                .Query(tracked: false)
                .Include(c => c.Case)
                .Include(c => c.Sender)
                .Include(c => c.Receiver)
                .FirstOrDefaultAsync(c =>
                    c.CaseId == caseId &&
                    (
                        (c.SenderId == currentUserId && c.ReceiverId == caseOwnerId) ||
                        (c.SenderId == caseOwnerId && c.ReceiverId == currentUserId)
                    ));

            if (chat == null)
            {
                isNewChat = true;

                var newChat = new Chat
                {
                    CaseId = caseId,
                    SenderId = currentUserId,
                    ReceiverId = caseOwnerId,
                    CreatedAt = DateTime.UtcNow
                };

                await _unitOfWork.Repository<Chat>().CreateAsync(newChat);
                await _unitOfWork.SaveAsync();

                chat = await _unitOfWork.Repository<Chat>()
                    .Query(tracked: false)
                    .Include(c => c.Case)
                    .Include(c => c.Sender)
                    .Include(c => c.Receiver)
                    .FirstAsync(c => c.Id == newChat.Id);

                _logger.LogInformation(
                    "New chat {ChatId} created between {SenderId} and {ReceiverId}.",
                    chat.Id,
                    chat.SenderId,
                    chat.ReceiverId);
            }
            else
            {
                _logger.LogInformation(
                    "Existing chat {ChatId} returned for user {UserId}.",
                    chat.Id,
                    currentUserId);
            }

            var dto = _mapper.Map<ChatDetailsDto>(chat);

            dto.OtherUserName = chat.SenderId == currentUserId
                ? dto.ReceiverName
                : dto.SenderName;

            return ApiResponse<ChatDetailsDto>.Ok(
                dto,
                isNewChat
                    ? "تم إنشاء المحادثة بنجاح."
                    : "تم استرجاع المحادثة الموجودة.");

        }
        public async Task<ApiResponse<IEnumerable<ChatSummaryDto>>> GetUserChatsAsync(string currentUserId)
        {
            _logger.LogInformation(
            "Fetching chats for user {UserId}.",
            currentUserId);

            //var chats = await _unitOfWork.ChatRepository.GetUserChatsAsync (currentUserId);

            var result = await _unitOfWork.Repository<Chat>()
                .Query(tracked: false)
                .Where(c =>
                (c.SenderId == currentUserId && !c.DeletedBySender) ||
                (c.ReceiverId == currentUserId && !c.DeletedByReceiver))
                .Select(c => new ChatSummaryDto
                {
                    ChatId = c.Id,
                    CaseId = c.CaseId,
                    CaseTitle = $"{c.Case.FName} {c.Case.SName} {c.Case.TName} {c.Case.LName}",

                    OtherUserId = c.SenderId == currentUserId ? c.ReceiverId : c.SenderId,

                    OtherUserName = c.SenderId == currentUserId
                    ? $"{c.Receiver.FName} {c.Receiver.LName}"
                    : $"{c.Sender.FName} {c.Sender.LName}",

                    OtherUserImage = c.SenderId == currentUserId
                    ? c.Receiver.ProfileImage
                    :c.Sender.ProfileImage,

                    LastMessage = c.Messages
                    .OrderByDescending(m => m.SendAt)
                    .Select(m => m.IsDeletedForEveryone
                    ? "تم حذف هذه الرسالة"
                    : m.Content)
                .FirstOrDefault(),

                    LastMessageDate = c.Messages
                    .OrderByDescending(m => m.SendAt)
                    .Select(m => m.SendAt)
                    .FirstOrDefault(),


                    UnreadCount = c.Messages.Count(m =>
                    !m.IsRead && m.ReceiverId == currentUserId)
                })
                .OrderByDescending(x => x.LastMessageDate)
                .ToListAsync();

            foreach (var chat in result)
            {
                if (chat.LastMessageDate.HasValue)
                {
                    chat.LastMessageDate = DateTime.SpecifyKind(
                        chat.LastMessageDate.Value,
                        DateTimeKind.Utc);
                }
            }

            _logger.LogInformation(
            "User {UserId} has {Count} chats.",
            currentUserId,
            result.Count());

            return ApiResponse<IEnumerable<ChatSummaryDto>>.Ok(
                result, "تم جلب المحادثات بنجاح.");
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
                    c =>c.Case.CaseFiles,
                    c => c.Sender,
                    c => c.Receiver)
                ?? throw new NotFoundException($"{chatId}لم يتم العثور على المحادثة.");
            if (!isAdmin)
            {
                EnsureParticipant(chat, currentUserId);
            }
            foreach (var file in chat.Case.CaseFiles)
            {
                _logger.LogInformation(
                    "Image={Image}, IsPrimary={Primary}",
                    file.ImagePath,
                    file.IsPrimary
                );
            }
            var primaryImage = chat.Case.CaseFiles.FirstOrDefault(f => f.IsPrimary)?.ImagePath;

            var dto = new ChatDetailsDto
            {
                ChatId = chat.Id,
                CaseId = chat.CaseId,
                CaseTitle = $"{chat.Case.FName} {chat.Case.SName} {chat.Case.TName} {chat.Case.LName}",
                CaseImage = primaryImage,
                CaseType = chat.Case.CaseType,

                CreatedAt = chat.CreatedAt
            };

            if (isAdmin)
            {
                dto.SenderId = chat.SenderId;
                dto.SenderName = $"{chat.Sender.FName} {chat.Sender.LName}";
                dto.SenderImage = chat.Sender.ProfileImage;

                dto.ReceiverId = chat.ReceiverId;
                dto.ReceiverName = $"{chat.Receiver.FName} {chat.Receiver.LName}";
                dto.ReceiverImage = chat.Receiver.ProfileImage;

                dto.DeletedBySender = chat.DeletedBySender;
                dto.DeletedByReceiver = chat.DeletedByReceiver;

                dto.SenderDeletedAt = chat.SenderDeletedAt.HasValue
                ? DateTime.SpecifyKind(chat.SenderDeletedAt.Value, DateTimeKind.Utc)
                : null;

                dto.ReceiverDeletedAt = chat.ReceiverDeletedAt.HasValue
                    ? DateTime.SpecifyKind(chat.ReceiverDeletedAt.Value, DateTimeKind.Utc)
                    : null;

                dto.OtherUserId = chat.SenderId == currentUserId
                    ? chat.Receiver.Id
                    : chat.Sender.Id;

                dto.OtherUserName = chat.SenderId == currentUserId
                    ? $"{chat.Receiver.FName} {chat.Receiver.LName}"
                : $"{chat.Sender.FName} {chat.Sender.LName}";

                dto.OtherUserImage = chat.SenderId == currentUserId
                     ? chat.Receiver.ProfileImage
                    : chat.Sender.ProfileImage;
            }
            else
            {
                dto.OtherUserId = chat.SenderId == currentUserId
                    ? chat.Receiver.Id
                    : chat.Sender.Id;

                dto.OtherUserName = chat.SenderId == currentUserId
                    ? $"{chat.Receiver.FName} {chat.Receiver.LName}"
                : $"{chat.Sender.FName} {chat.Sender.LName}";

                dto.OtherUserImage = chat.SenderId == currentUserId
                     ? chat.Receiver.ProfileImage
                    : chat.Sender.ProfileImage;
            }

            _logger.LogInformation(
            "Chat {ChatId} details returned for user {UserId}.",
            chatId,
            currentUserId);

            return ApiResponse<ChatDetailsDto>.Ok(
               dto,
                "تم جلب تفاصيل المحادثة بنجاح.");

        }
        public async Task <ApiResponse<List<MessageDto>>> GetPaginatedMessagesAsync(long chatId, string currentUserId,bool isAdmin)
        {
            _logger.LogInformation(
            "User {UserId} requested messages for Chat {ChatId}.",
            currentUserId,
            chatId);

            var chat = await _unitOfWork.Repository<Chat>()
                .GetOneAsync(
                    c => c.Id == chatId,
                    tracked: false,
                    c => c.Case,
                    chatId => chatId.Messages)
               ?? throw new NotFoundException($"{chatId}لم يتم العثور على المحادثة.");

            if (!isAdmin)
            {
                EnsureParticipant(chat, currentUserId);
            }
            
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
            .ToListAsync();


            _logger.LogInformation(
            "Returned {Count} messages out of {Total} for Chat {ChatId}.",
             messages.Count(),
             totalCount,
             chatId);

            var messageDtos = _mapper.Map<List<MessageDto>>(messages);

            if (!isAdmin)
            {
                foreach (var message in messages)
                {
                    if (message.IsDeletedForEveryone)
                    {
                        message.Content = "تم حذف هذه الرسالة";
                    }
                }
            }
            foreach (var message in messageDtos)
            {
                message.IsMine = message.SenderId == currentUserId;
            }

            
            return ApiResponse<List<MessageDto>>.Ok(messageDtos,
                "تم جلب الرسائل بنجاح.");


        }

        public async Task<ApiResponse<ChatDetailsDto>> DeleteChatAsync(long chatId, string userId)
        {
            var chat = await _unitOfWork.Repository<Chat>()
                .GetOneAsync(
                c=> c.Id == chatId,
                true,
                c=> c.Sender,
                chatId => chatId.Receiver)
                ?? throw new NotFoundException($"{chatId} لم يتم العثور على المحادثة.");

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
               $"تم حذف المحادثة بنجاح.");


        }

        public async Task<ApiResponse<ChatDetailsDto>> DeleteChatByAdminAsync(long chatId)
        {
            var chat = await _unitOfWork.Repository<Chat>()
                .GetOneAsync(
                c=> c.Id == chatId,
                includes: c => c.Messages
                )
                ?? throw new NotFoundException(
            $"{chatId} لم يتم العثور على المحادثة.");

            _unitOfWork.Repository<Chat>().Remove( chat );
            
            await _unitOfWork.SaveAsync();

            return ApiResponse<ChatDetailsDto>.Ok(
               _mapper.Map<ChatDetailsDto>(chat),
               "تم حذف المحادثة نهائيًا.");


        }

        public async Task<ApiResponse<PaginationResponseDto<AdminChatsDto>>> GetAllChatsAsync(int page, int pageSize, ChatFilterDto filter)
        {
            var baseQuery = _unitOfWork.Repository<Chat>()
                .Query(
                 false,
                 null,
                OrderBy.Descending,
                null,
                null,
                c => c.Messages,
                c => c.Case,
                c => c.Sender,
                c => c.Receiver);



            if (filter.FromDate.HasValue)
                baseQuery = baseQuery.Where(c => c.CreatedAt >= filter.FromDate);

            if (filter.ToDate.HasValue)
                baseQuery = baseQuery.Where(c => c.CreatedAt <= filter.ToDate.Value.Date.AddDays(1).AddTicks(-1));

            if (filter.IsDeletedBySender.HasValue)
                baseQuery = baseQuery.Where(c => c.DeletedBySender == filter.IsDeletedBySender);

            if (filter.IsDeletedByReceiver.HasValue)
                baseQuery = baseQuery.Where(c => c.DeletedByReceiver == filter.IsDeletedByReceiver);

            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var search = filter.Search.Trim();

                var searchWords = search.Split(
                    ' ',
                    StringSplitOptions.RemoveEmptyEntries
                );

                baseQuery = baseQuery.Where(c =>

                    c.Messages.Any(m => m.Content.Contains(search)) ||

                    // Sender
                    searchWords.All(word =>
                        (c.Sender.FName + " " + c.Sender.LName)
                            .Contains(word)
                    ) ||

                    // Receiver
                    searchWords.All(word =>
                        (c.Receiver.FName + " " + c.Receiver.LName)
                            .Contains(word)
                    ) ||

                    // Case Title
                    searchWords.All(word =>
                        (c.Case.FName + " " +
                         c.Case.SName + " " +
                         c.Case.TName + " " +
                         c.Case.LName)
                        .Contains(word)
                    )
                );
            }
            baseQuery = baseQuery.OrderByDescending(c =>
            c.Messages
                .Select(m => (DateTime?)m.SendAt)
                .Max() ?? c.CreatedAt);

            var totalCount = await baseQuery.CountAsync();

            var chats = await baseQuery
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var items = chats.Select(c => new AdminChatsDto
            {
                ChatId = c.Id,
                CaseId = c.CaseId,
                CaseTitle = $"{c.Case.FName} {c.Case.SName} {c.Case.TName} {c.Case.LName}",
                CaseType = c.Case.CaseType,
                SenderId = c.SenderId,
                ReceiverId = c.ReceiverId,

                SenderName = $"{c.Sender.FName} {c.Sender.LName}",

                ReceiverName = $"{c.Receiver.FName} {c.Receiver.LName}",

                MessagesCount = c.Messages.Count,
                UnreadMessagesCount = c.Messages.Count(m => !m.IsRead),

                CreatedAt = c.CreatedAt,

                LastMessage = c.Messages
                .OrderByDescending(m => m.SendAt)
                .Select(m => m.Content)
                .FirstOrDefault(),

                LastMessageAt = c.Messages
                .OrderByDescending(m => m.SendAt)
                .Select(m => (DateTime?)DateTime.SpecifyKind(m.SendAt, DateTimeKind.Utc))
                .FirstOrDefault(),

                IsDeletedBySender = c.DeletedBySender,
                IsDeletedByReceiver = c.DeletedByReceiver,

                SenderDeletedAt = c.SenderDeletedAt.HasValue
                ? (DateTime?)DateTime.SpecifyKind(c.SenderDeletedAt.Value, DateTimeKind.Utc)
                : null,

                ReceiverDeletedAt = c.ReceiverDeletedAt.HasValue
                ? (DateTime?)DateTime.SpecifyKind(c.ReceiverDeletedAt.Value, DateTimeKind.Utc)
                : null,
            }).ToList();

            var result = new PaginationResponseDto<AdminChatsDto>
            {
                Items = items,
                PageNumber = page,
                PageSize = pageSize,
                TotalCount = totalCount
            };

            return ApiResponse<PaginationResponseDto<AdminChatsDto>>
            .Ok(result, "تم جلب المحادثات بنجاح.");
        }

        public async Task<ApiResponse<AdminChatStatisticsDto>> GetChatStatisticsAsync()
        {
            var chatsStats = await _unitOfWork.Repository<Chat>().Query(tracked: false)
                .GroupBy(c => new { c.DeletedBySender, c.DeletedByReceiver })
                .Select(g => new { g.Key.DeletedBySender, g.Key.DeletedByReceiver, Count = g.Count() })
                .ToListAsync();

            var totalChats = chatsStats.Sum(x => x.Count);
            var activeChats = chatsStats.Where(x => !x.DeletedBySender && !x.DeletedByReceiver).Sum(x => x.Count);
            var deletedBySenderOnly = chatsStats.Where(x => x.DeletedBySender && !x.DeletedByReceiver).Sum(x => x.Count);
            var deletedByReceiverOnly = chatsStats.Where(x => !x.DeletedBySender && x.DeletedByReceiver).Sum(x => x.Count);
            var deletedByBoth = chatsStats.Where(x => x.DeletedBySender && x.DeletedByReceiver).Sum(x => x.Count);

            var stats = new AdminChatStatisticsDto
            {
                TotalChats = totalChats,
                ActiveChats = activeChats,
                DeletedBySenderOnly = deletedBySenderOnly,
                DeletedByReceiverOnly = deletedByReceiverOnly,
                DeletedByBoth = deletedByBoth
            };

            return ApiResponse<AdminChatStatisticsDto>.Ok(stats, "تم جلب الإحصائيات بنجاح.");
        }

        private void EnsureParticipant(Chat chat, string userId)
        {
            if (chat.SenderId != userId && chat.ReceiverId != userId) {
                _logger.LogWarning(
            "Unauthorized access attempt. User {UserId} tried to access Chat {ChatId}",
            userId,
            chat.Id);

                throw new ForbiddenException("ليس لديك صلاحية للوصول إلى هذه المحادثة.");
            }
        }

    }
}
