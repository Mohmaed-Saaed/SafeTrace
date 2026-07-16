using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SafeTrace.Application.DTOs.Dashboard.Response;
using SafeTrace.Application.DTOs.Responses;
using SafeTrace.Application.Interfaces.IServices;
using SafeTrace.Domain.Entities;
using SafeTrace.Domain.Enums;
using SafeTrace.Domain.Interfaces.IUnitOfWork;
using System;
using System.Collections.Generic;
using System.Text;

namespace SafeTrace.Application.Services
{
    public class DashboardService : IDashboardService
    {

        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<ApplicationUser> _userManager;
        public DashboardService(IUnitOfWork unitOfWork
            , UserManager<ApplicationUser> userManager) { 
            _unitOfWork = unitOfWork;
            _userManager = userManager;

        }
        async Task<ApiResponse<DashboardDto>> IDashboardService.GetDashboardAsync()
        {
            var casesQuery = _unitOfWork.Repository<Case>()
                .Query(tracked: false);

            var DonationQuery = _unitOfWork.Repository<Donation>()
                .Query(tracked: false);

            var TotalSumDonations = await DonationQuery.Where(x => x.PaymentStatus == PaymentStatus.Succeeded)
                                                    .SumAsync(x => x.Amount);

            var TotalCountFailedDonations = await DonationQuery.Where(x => x.PaymentStatus == PaymentStatus.Failed)
                                                    .Count();

            var TotalCountSucceededDonations = await DonationQuery.Where(x => x.PaymentStatus == PaymentStatus.Succeeded)
                                                    .Count();

            var users = await _userManager.GetUsersInRoleAsync("User");

            var totalCases = await casesQuery.CountAsync();
            var totalFounded = await casesQuery.CountAsync(x => x.Status == CaseStatus.Found);
            var totalActive = await casesQuery.CountAsync(x => x.Status == CaseStatus.Active);
            var totalDeleted = await casesQuery.CountAsync(x => x.Status == CaseStatus.Deleted);
            var totalPending = await casesQuery.CountAsync(x => x.Status == CaseStatus.Pending);

            var caseTypes = await casesQuery
                .GroupBy(x => x.CaseType)
                .Select(g => new CaseTypeStatsDto
                {
                    CaseType = g.Key.ToString(),
                    Total = g.Count(),
                    Active = g.Count(x => x.Status == CaseStatus.Active),
                    Pending = g.Count(x => x.Status == CaseStatus.Pending),
                    Deleted = g.Count(x => x.Status == CaseStatus.Deleted),
                    Closed = g.Count(x => x.Status == CaseStatus.Closed)
                })
                .ToListAsync();

            return ApiResponse<DashboardDto>.Ok(
                new DashboardDto
                {
                    TotalUsers = users.Count,
                    TotalCases = totalCases,
                    TotalFoundedCases = totalFounded,
                    TotalActiveCases = totalActive,
                    TotalDeletedCases = totalDeleted,
                    TotalPendingCases = totalPending,
                    TotalSumDonations = TotalSumDonations,
                    TotalCountFailedDonations = TotalCountFailedDonations,
                    TotalCountSucceededDonations = TotalCountSucceededDonations,
                    CaseTypes = caseTypes
                });
        }
    }
}
