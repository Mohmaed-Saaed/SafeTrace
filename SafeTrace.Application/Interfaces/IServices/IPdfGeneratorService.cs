using SafeTrace.Application.DTOs.Cases.Request;
using SafeTrace.Application.DTOs.Complaints.Request;
using SafeTrace.Application.DTOs.Complaints.Response;
using SafeTrace.Application.DTOs.Dashboard.Request;
using SafeTrace.Application.DTOs.Dashboard.Response;
using SafeTrace.Application.DTOs.Payment.Request;
using SafeTrace.Application.DTOs.Payment.Response;
using SafeTrace.Application.DTOs.User.Request;
using SafeTrace.Application.DTOs.User.Response;
using System;
using System.Collections.Generic;
using System.Text;

namespace SafeTrace.Application.Interfaces.IServices
{
    public interface IPdfGeneratorService
    {
        byte[] GenerateComplaintsPdf(
        List<ComplaintResponseDto> complaints,
        ComplaintStatisticsDto statistics,
        ComplaintFilterDto filter);

        byte[] GenerateDonationsPdf(
       List<DonationAdminListDto> donations,
       AdminDonationStatisticsDto statistics,
       DonationAdminQueryDto filter);

        byte[] GenerateCasesPdf(
        List<CaseReportDto> cases,
        CasesStatisticsDto statistics,
        CasesReportFilterDto filter);

        byte[] GenerateUsersPdf(
        List<GetUserDto> users,
        UserStatisticsDto statistics,
        UserFilterDto filter, string? roleName);
    }
}
