using SafeTrace.Domain.Interfaces.IRepositories;
using SafeTrace.Infrastructure.DataAccess;
using System;
using System.Collections.Generic;
using System.Text;

namespace SafeTrace.Infrastructure.Repositories.Repository
{
    public class MessageRepository : Repository<Message>, IMessageRepository
    {
        private readonly ApplicationDbContext _context;
        public MessageRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<(IEnumerable<Message> Messages, int TotalCount)> GetPagedMessagesAsync(long chatId,string currentUserId, int page, int pageSize)
        {
            var query = _context.Messages
                .AsNoTracking()
                .Where(m =>
                    m.ChatId == chatId &&
                    (
                        (m.SenderId == currentUserId && !m.DeletedBySender)
                        ||
                        (m.ReceiverId == currentUserId && !m.DeletedByReceiver)
                    )
                )
                .OrderBy(m => m.SendAt);

            var totalCount = await query.CountAsync();

            var messages = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            return (messages, totalCount);
        }

        public async Task<int> MarkMessagesAsReadAsync(long chatId, string receiverId)
        {
            return await _context.Messages
                .Where(m => m.ChatId == chatId && m.ReceiverId == receiverId && !m.IsRead)
                .ExecuteUpdateAsync(setters => setters.SetProperty(m => m.IsRead, true));
        }
    }

}

