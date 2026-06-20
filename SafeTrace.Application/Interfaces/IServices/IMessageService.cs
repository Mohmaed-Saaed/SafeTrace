using SafeTrace.Application.DTOs.Message;
using SafeTrace.Application.DTOs.Responses;
using System;
using System.Collections.Generic;
using System.Text;

namespace SafeTrace.Application.Interfaces.IServices
{
    public interface IMessageService
    {
        Task <ApiResponse<MessageDto>> SendMessageAsync(SendMessageRequest request, string senderId);
        Task<ApiResponse<int>> MarkMessagesAsReadAsync(long chatId, string userId);
    }
}
