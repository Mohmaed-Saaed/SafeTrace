using SafeTrace.Domain.Interfaces.IRepositories;
using SafeTrace.Infrastructure.DataAccess;
using System;
using System.Collections.Generic;
using System.Text;

namespace SafeTrace.Infrastructure.Repositories.Repository
{
    public class ChatRepository : Repository<Chat>, IChatRepository
    {
        private readonly ApplicationDbContext _context;
        public ChatRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<Chat?> GetExistingChatAsync(long caseId, string userId1, string userId2)
        {
            return await _context.Chats.AsNoTracking()
                .FirstOrDefaultAsync(c =>
                c.CaseId == caseId &&
                ((c.SenderId == userId1 && c.ReceiverId == userId2) ||
                     (c.SenderId == userId2 && c.ReceiverId == userId1)));
        }

        public async Task<IEnumerable<Chat>> GetUserChatsAsync(string userId)
        {
            return await _context.Chats.AsNoTracking()
                .Where(c => (c.SenderId == userId && !c.DeletedBySender)
                || c.ReceiverId == userId && !c.DeletedByReceiver)
                .Include(c => c.Messages)
                .OrderByDescending(c => c.Messages
                .OrderByDescending(m => m.SendAt)
                .Select(m => m.SendAt)
                .FirstOrDefault()).ToListAsync();
        }
        public async Task<Chat?> GetChatWithDetailsAsync(long chatId)
        {
            return await _context.Chats.AsNoTracking()
                .Include(c => c.Case)
                .FirstOrDefaultAsync(c => c.Id == chatId);
        }

    }
}

