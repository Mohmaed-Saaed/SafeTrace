using SafeTrace.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Text;

namespace SafeTrace.Application.Exceptions
{
    public class UnknownCaseMatchException : Exception
    {
        public List<UnknownCaseMatchDto> Matches { get; }

        public UnknownCaseMatchException(List<UnknownCaseMatchDto> matches)
            : base("تم العثور على حالات مجهولة مشابهة للصور المرفوعة.")
        {
            Matches = matches;
        }
    }
}
