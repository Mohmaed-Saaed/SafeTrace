using SafeTrace.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace SafeTrace.Domain.Entities
{
    public abstract class BaseCase
    {
        public long Id { get; set; }
        public Gender Gender { get; set; }
        public string? FName { get; set; }
        public string? SName { get; set; }
        public string? TName { get; set; }
        public string? LName { get; set; }
        public int Age { get; set; }
        public string UserId { get; set; } = null!;
        public ApplicationUser User { get; set; } = null!;
        public string? communicationphone{ get; set; }
        public CaseStatus Status { get; set; }

        public double LocationLatitude { get; set; } 
        public double LocationLongitude { get; set; }
        public string CaseCode { get; set; } = null!;
        public RelationType Relation { get; set; }
        public DateTime CreatedAt { get; set; }

        public DateTime LostDate { get; set; }

        public string? Description { get; set; }
        public CaseType CaseType { get; set; }
        public int   AgeCategoryId { get; set; }

        public FoundPersonInfo? FoundPersonInfo { get; set; }

        public AgeCategory AgeCategory { get; set; } = new AgeCategory();
        public ICollection<CasePhoto> Photos { get; set; } = new List<CasePhoto>();
        public ICollection<Chat> Chats { get; set; } = new List<Chat>();
    }
}
