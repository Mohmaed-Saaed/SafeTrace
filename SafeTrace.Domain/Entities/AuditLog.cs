using System;

namespace SafeTrace.Domain.Entities
{
    public class AuditLog
    {
        public long Id { get; set; }
        public string? UserId { get; set; }
        public string Type { get; set; } = default!;
        public string TableName { get; set; } = default!;
        public DateTime DateTime { get; set; }
        public string? OldValues { get; set; }
        public string? NewValues { get; set; }
        public string? AffectedColumns { get; set; }
        public string PrimaryKey { get; set; } = default!;
    }
}
