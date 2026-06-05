using System;
using System.Collections.Generic;
using System.Text;

namespace SafeTrace.Domain.Entities
{
    public class FoundPersonInfo
    {
        public long Id { get; set; }

        public string Description { get; set; } = null!;

        public string Government { get; set; } = null!;

        public string City { get; set; } = null!;

        public string Street { get; set; } = null!;

        public long CaseId { get; set; }

        public DateTime FoundedAt { get; set; }

        public string FoundedUserId { get; set; } = null!;

        public ApplicationUser FoundedUser { get; set; } = null!;

        public BaseCase Case { get; set; } = null!;
    }
}
