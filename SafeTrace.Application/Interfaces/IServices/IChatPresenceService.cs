using System;
using System.Collections.Generic;
using System.Text;

namespace SafeTrace.Application.Interfaces.IServices
{
    public interface IChatPresenceService
    {
        void Join(string userId, long chatId);
        void Leave(string userId);

        bool IsUserInChat(string userId, long chatId);

        bool ShouldSendEmail(string userId, long chatId);

        void MarkEmailAsSent(string userId, long chatId);

        void ResetEmail(string userId);
    }
}
