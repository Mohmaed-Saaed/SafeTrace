using System;
using SafeTrace.Domain.Entities;
using SafeTrace.Domain.Specifications;

namespace SafeTrace.Application.Specifications.CasesSpecifications.UrgentCaseSpecifications;

public class GetMenSpecification : BaseSpecification<UrgentCase>
{   
    public GetMenSpecification()
    {
        AddCriteria(x => x.Gender == Gender.Female);
    }
}
