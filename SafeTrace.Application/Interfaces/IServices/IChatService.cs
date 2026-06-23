using SafeTrace.Application.DTOs.Chat;
using SafeTrace.Application.DTOs.Message;
using SafeTrace.Application.DTOs.Responses;
using System;
using System.Collections.Generic;
using System.Text;

namespace SafeTrace.Application.Interfaces.IServices
{
    public interface IChatService
    {
        Task<ApiResponse<ChatDetailsDto>> StartOrGetChatAsync(long caseId, string currentUserId);
        Task<ApiResponse<IEnumerable<ChatSummaryDto>>> GetUserChatsAsync (string currentUserId);
        Task<ApiResponse<ChatDetailsDto>> GetChatDetailsAsync(long chatId, string currentUserId);

        Task <ApiResponse<PaginationResponseDto<MessageDto>>> GetPaginatedMessagesAsync (long chatId, string currentUserId,int page, int pageSize);
        Task<ApiResponse<ChatDetailsDto>> DeleteChatAsync(long chatId, string userId);
        Task<ApiResponse<ChatDetailsDto>> DeleteChatByAdminAsync(long chatId);

        Task<ApiResponse<PaginationResponseDto<AdminChatsDto>>> GetAllChatsAsync(int page , int pageSize, ChatFilterDto filter);
    }
}
