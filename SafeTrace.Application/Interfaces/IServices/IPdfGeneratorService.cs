using SafeTrace.Application.DTOs.Complaints.Request;
using SafeTrace.Application.DTOs.Complaints.Response;
using SafeTrace.Application.DTOs.Payment.Request;
using SafeTrace.Application.DTOs.Payment.Response;
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
    }
}
