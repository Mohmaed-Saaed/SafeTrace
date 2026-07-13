using System;
using System.Collections.Generic;
using System.Text;

namespace SafeTrace.Application.DTOs
{

    public class UnknownCaseMatchDto
    {
        public long CaseId { get; set; }
        public string CaseCode { get; set; } = null!;
        public string MainPhotoPath { get; set; } = null!;
        public float Similarity { get; set; }
        public string Government { get; set; } = null!;
        public string City { get; set; } = null!;

    }
}
