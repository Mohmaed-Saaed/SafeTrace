using SafeTrace.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace SafeTrace.Domain.Entities
{
    public class DuplicateGroup
    {
        public long Id { get; set; }
        //Master relation only not for cases
        public long? MasterCaseId { get; set; }
        public DuplicateGroupStatus GroupStatus { get; set; } 
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public Case? MasterCase { get; set; }
        public ICollection<DuplicateGroupCase> DuplicateCases { get; set; }= new List<DuplicateGroupCase>();
    }
}
