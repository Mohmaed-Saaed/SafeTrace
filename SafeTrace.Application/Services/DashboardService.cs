using Microsoft.AspNetCore.Identity;
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

            var cases = await _unitOfWork.CaseRepository.GetAllAsync();

            var urgentCases = cases.Where(e => e.CaseType == CaseType.Urgent);
            var longTermCases = cases.Where(e => e.CaseType == CaseType.LongTerm);
            var unknownCases = cases.Where(e => e.CaseType == CaseType.Unknown);
            //var urgentCases = await _unitOfWork.UrgentCaseRepository.GetAllAsync();
            
            //var longTermCases = await _unitOfWork.LongTermMissingCaseRepository.GetAllAsync();
            //var unknownCases = await _unitOfWork.UnknownCaseRepository.GetAllAsync();

            //var cases = urgentCases.Cast<BaseCase>()
            //    .Concat(longTermCases)
            //    .Concat(unknownCases)
            //    .ToList();

            var users = await _userManager.GetUsersInRoleAsync("User");

                var totalFounded = cases.Count(x => x.Status == CaseStatus.Found);
                var totalActive = cases.Count(x => x.Status == CaseStatus.Active);
                var totalDeleted = cases.Count(x => x.Status == CaseStatus.Deleted);
                var totalPending = cases.Count(x => x.Status == CaseStatus.Pending);

            //var totalFounded = cases.Count(x => x.Status == CaseStatus.Found);
            //var totalActive = cases.Count(x => x.Status == CaseStatus.Active);
            //var totalDeleted = cases.Count(x => x.Status == CaseStatus.Deleted);
            //var totalPending = cases.Count(x => x.Status == CaseStatus.Pending);

            var caseTypes = cases
                .GroupBy(x => x.CaseType)
                .Select(g => new CaseTypeStatsDto
                {
                    CaseType = g.Key.ToString(),
                    Total = g.Count(),
                    Active = g.Count(x => x.Status == CaseStatus.Active),
                    Founded = g.Count(x => x.Status == CaseStatus.Found),
                    Closed = g.Count(x => x.Status == CaseStatus.Deleted)
                })
                .ToList();

            return ApiResponse<DashboardDto>.Ok(
                new DashboardDto
                {
                    TotalUsers = users.Count,
                    TotalCases = cases.Count(),
                    TotalFoundedCases = totalFounded,
                    TotalActiveCases = totalActive,
                    TotalDeletedCases = totalDeleted,
                    TotalPendingCases = totalPending, 
                    CaseTypes = caseTypes
                });
        }
    }
}
