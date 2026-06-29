using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SafeTrace.Application.DTOs.LongTermCases
{
    public class CasePhotoDto
    {
        public long Id { get; set; }
        public string ImagePath { get; set; } = null!;
    }
}
