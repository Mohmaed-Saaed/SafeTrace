using System;
using System.Collections.Generic;
using System.Text;

namespace SafeTrace.Application.DTOs.Message
{
    public class PaginatedMessagesDto
    {
        public IEnumerable<MessageDto> Messages { get; set; } = [];
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    }
}
