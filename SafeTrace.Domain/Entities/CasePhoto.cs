using System;
using System.Collections.Generic;
using System.Text;

namespace SafeTrace.Domain.Entities
{
    public class CasePhoto
    {
        public long Id { get; set; }

        public long CaseId { get; set; }

        public string ImagePath { get; set; } = null!;

        public DateTime CreatedAt { get; set; }

        public BaseCase Case { get; set; } = null!;
    }
}
