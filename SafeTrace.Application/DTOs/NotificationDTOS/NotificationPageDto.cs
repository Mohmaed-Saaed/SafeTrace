using System;
using System.Collections.Generic;
using System.Text;

namespace SafeTrace.Application.DTOs.NotificationDTOS
{
    public class NotificationPageDto
    {
        public IEnumerable<GetUserNotificationsDTO> Items { get; set; } = [];

        public int Page { get; set; }

        public int PageSize { get; set; }

        public int TotalCount { get; set; }
        public int TotalPages { get; set; }

        public bool HasMore { get; set; }
    }
}
