using SafeTrace.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace SafeTrace.Domain.Entities
{
    public class DuplicateGroup
    {
        public long Id { get; set; }
        public DuplicateGroupStatus GroupStatus { get; set; } 
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public ICollection<DuplicateGroupCase> DuplicateCases { get; set; }= new List<DuplicateGroupCase>();
    }
}
