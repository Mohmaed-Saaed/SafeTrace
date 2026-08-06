using Microsoft.AspNetCore.Identity;
using SafeTrace.Application.Common.Enums;
using SafeTrace.Application.DTOs.Cases.Request;
using SafeTrace.Application.DTOs.Dashboard.Request;
using SafeTrace.Application.DTOs.Dashboard.Response;


namespace SafeTrace.Application.Services
{
    public class DashboardService : IDashboardService
    {

        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IPdfGeneratorService _pdfGenerator;
        public DashboardService(IUnitOfWork unitOfWork
            , UserManager<ApplicationUser> userManager,
            IPdfGeneratorService pdfGenerator) { 
            _unitOfWork = unitOfWork;
            _userManager = userManager;
            _pdfGenerator = pdfGenerator;

        }
        async Task<ApiResponse<DashboardDto>> IDashboardService.GetDashboardAsync()
        {
            var casesQuery = _unitOfWork.Repository<Case>()
                .Query(tracked: false);

            var donationQuery =  _unitOfWork.Repository<Donation>()
                .Query(tracked: false);

            var complaints = _unitOfWork.Repository<Complaint>()
                .Query(tracked: false);

            var aiSearchUsage = _unitOfWork.Repository<AiSearchUsage>()
                .Query(tracked: false);

            decimal totalSumDonations = await donationQuery.Where(x => x.PaymentStatus == PaymentStatus.Succeeded).SumAsync(x => x.Amount);

            int totalCountFailedDonations = await donationQuery.Where(x => x.PaymentStatus == PaymentStatus.Failed).CountAsync();

            int totalCountSucceededDonations = await donationQuery.Where(x => x.PaymentStatus == PaymentStatus.Succeeded).CountAsync();

            int totalCountPendingDonations = await donationQuery.Where(x => x.PaymentStatus == PaymentStatus.Pending).CountAsync();

            int totalSolvedComplaints = await complaints.Where(x => x.ComplaintStatus == ComplaintStatus.Solved).CountAsync();

            int totalUnSolvedComplaints = await complaints.Where(x => x.ComplaintStatus == ComplaintStatus.UnSolved).CountAsync();

            int totalDailyAISearch = await aiSearchUsage.CountAsync(x => x.CreatedAt.Date == DateTime.Now.Date);


            var users = await _userManager.GetUsersInRoleAsync("User");

            var totalCases = await casesQuery.CountAsync();
            var totalFounded = await casesQuery.CountAsync(x => x.Status == CaseStatus.Found);
            var totalActive = await casesQuery.CountAsync(x => x.Status == CaseStatus.Active);
            var totalDeleted = await casesQuery.CountAsync(x => x.Status == CaseStatus.Deleted);
            var totalPending = await casesQuery.CountAsync(x => x.Status == CaseStatus.Pending);
            var totalExpired = await casesQuery.CountAsync(x => x.Status == CaseStatus.Expired);
            var totalRejected = await casesQuery.CountAsync(x => x.Status == CaseStatus.Rejected);

            var caseTypes = await casesQuery
                .GroupBy(x => x.CaseType)
                .Select(g => new CaseTypeStatsDto
                {
                    CaseType = g.Key.ToString(),
                    Total = g.Count(),
                    Pending = g.Count(x => x.Status == CaseStatus.Pending),
                    Active = g.Count(x => x.Status == CaseStatus.Active),
                    Deleted = g.Count(x => x.Status == CaseStatus.Deleted),
                    Found = g.Count(x => x.Status == CaseStatus.Found),
                    Rejected = g.Count(x => x.Status == CaseStatus.Rejected),
                    Expired = g.Count(x => x.Status == CaseStatus.Expired)
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
                    TotalExpiredCases = totalExpired,
                    TotalRejectedCases = totalRejected,
                    TotalSumDonations = totalSumDonations,
                    TotalCountFailedDonations = totalCountFailedDonations,
                    TotalCountSucceededDonations = totalCountSucceededDonations,
                    TotalCountPendingDonations = totalCountPendingDonations,
                    TotalDailyAISearch = totalDailyAISearch,
                    TotalSolvedComplaints = totalSolvedComplaints,
                    TotalUnSolvedComplaints = totalUnSolvedComplaints,
                    CaseTypes = caseTypes
                });
        }

        async Task<ApiResponse<CasesStatisticsDto>> IDashboardService.GetCasesStatisticsAsync()
        {
            var casesQuery = _unitOfWork.Repository<Case>().Query(tracked: false);

            var stats = await casesQuery
                .GroupBy(x => 1)
                .Select(g => new CasesStatisticsDto
                {
                    Total = g.Count(),
                    Urgent = g.Count(x => x.CaseType == CaseType.Urgent),
                    LongTerm = g.Count(x => x.CaseType == CaseType.LongTerm),
                    Unknown = g.Count(x => x.CaseType == CaseType.Unknown),
                    Active = g.Count(x => x.Status == CaseStatus.Active),
                    Found = g.Count(x => x.Status == CaseStatus.Found)
                })
                .FirstOrDefaultAsync();

            return ApiResponse<CasesStatisticsDto>.Ok(stats ?? new CasesStatisticsDto(), "تم جلب إحصائيات الحالات بنجاح.");
        }

        public async Task<ApiResponse<PaginationResponseDto<AuditLogDto>>> GetAuditLogsAsync(AuditLogQueryDto query)
        {
            var auditQuery = _unitOfWork.Repository<AuditLog>().Query(tracked: false);

            if (!string.IsNullOrEmpty(query.SearchTable))
            {
                auditQuery = auditQuery.Where(a => a.TableName.Contains(query.SearchTable));
            }

            if (!string.IsNullOrEmpty(query.SearchType))
            {
                auditQuery = auditQuery.Where(a => a.Type == query.SearchType);
            }

            var queryResult = auditQuery.Join(
                _userManager.Users,
                a => a.UserId,
                u => u.Id,
                (a, u) => new { Audit = a, User = u }
            );

            if (!string.IsNullOrEmpty(query.SearchEmail))
            {
                queryResult = queryResult.Where(x => x.User.Email.Contains(query.SearchEmail));
            }

            int totalCount = await queryResult.CountAsync();

            var logs = await queryResult
                .OrderByDescending(x => x.Audit.DateTime)
                .Skip((query.PageNumber - 1) * query.PageSize)
                .Take(query.PageSize)
                .Select(x => new AuditLogDto
                {
                    Id = x.Audit.Id,
                    UserEmail = x.User.Email,
                    Type = x.Audit.Type,
                    TableName = x.Audit.TableName,
                    DateTime = x.Audit.DateTime,
                    OldValues = x.Audit.OldValues,
                    NewValues = x.Audit.NewValues,
                    AffectedColumns = x.Audit.AffectedColumns,
                    PrimaryKey = x.Audit.PrimaryKey
                })
                .ToListAsync();

            var response = new PaginationResponseDto<AuditLogDto>
            {
                Items = logs,
                TotalCount = totalCount,
                PageNumber = query.PageNumber,
                PageSize = query.PageSize
            };

            return ApiResponse<PaginationResponseDto<AuditLogDto>>.Ok(response);
        }

        public async Task<List<CaseReportDto>> GetAllForReportAsync(
        CasesReportFilterDto filter)
        {
            var query = _unitOfWork.Repository<Case>()
                .Query(tracked: false);
                



            query = ApplyFilter(query, filter);


            query = ApplySorting(query, filter);



            return await query
                .Select(c => new CaseReportDto
                {
                    Id = c.Id,

                    CaseCode = c.CaseCode,

                    CaseType = c.CaseType,

                    Status = c.Status,

                    FullName =
                        string.Join(" ",
                        new[]
                        {
                        c.FName,
                        c.SName,
                        c.TName,
                        c.LName
                        }
                        .Where(x => !string.IsNullOrEmpty(x))),


                    Gender = c.Gender,

                    Age = c.Age,

                    City = c.City,

                    Government = c.Government,

                    CreatedAt = c.CreatedAt
                })
                .ToListAsync();
        }

        public async Task<byte[]> GeneratePdfReportAsync(
            CasesReportFilterDto filter)
        {
            var cases = await GetAllForReportAsync(filter);


            var statistics = new CasesStatisticsDto
            {
                Total = cases.Count,

                Urgent = cases.Count(c =>
                    c.CaseType == CaseType.Urgent),

                LongTerm = cases.Count(c =>
                    c.CaseType == CaseType.LongTerm),

                Unknown = cases.Count(c =>
                    c.CaseType == CaseType.Unknown),

                Active = cases.Count(c =>
                    c.Status == CaseStatus.Active),

                Found = cases.Count(c =>
                    c.Status == CaseStatus.Found)
            };


            return _pdfGenerator.GenerateCasesPdf(
                cases,
                statistics,
                filter);
        }

    //    public async Task<byte[]> GenerateExcelReportAsync(
    //CasesFilterBaseDto filter)
    //    {
    //        var cases = await GetAllForReportAsync(filter);


    //        var statistics = new CasesStatisticsDto
    //        {
    //            Total = cases.Count,

    //            Urgent = cases.Count(c =>
    //                c.CaseType == CaseType.Urgent),

    //            LongTerm = cases.Count(c =>
    //                c.CaseType == CaseType.LongTerm),

    //            Unknown = cases.Count(c =>
    //                c.CaseType == CaseType.Unknown),

    //            Active = cases.Count(c =>
    //                c.Status == CaseStatus.Active),

    //            Found = cases.Count(c =>
    //                c.Status == CaseStatus.Found)
    //        };


    //        return _excelGenerator.GenerateCasesExcel(
    //            cases,
    //            statistics,
    //            filter);
    //    }
        private IQueryable<Case> ApplyFilter(
            IQueryable<Case> query,
            CasesReportFilterDto filter)
        {

            if (filter.Status.HasValue)
            {
                query = query.Where(c =>
                    c.Status == filter.Status.Value);
            }

            if (filter.CaseType.HasValue) 
            {
                query = query.Where(c =>
                c.CaseType == filter.CaseType.Value);
            }

            if (filter.Gender.HasValue)
            {
                query = query.Where(c =>
                    c.Gender == filter.Gender.Value);
            }



            if (!string.IsNullOrWhiteSpace(filter.CaseCode))
            {
                query = query.Where(c =>
                    c.CaseCode.Contains(filter.CaseCode));
            }


            if (!string.IsNullOrWhiteSpace(filter.Government))
            {
                query = query.Where(c =>
                    c.Government.Contains(filter.Government));
            }


            if (!string.IsNullOrWhiteSpace(filter.City))
            {
                query = query.Where(c =>
                    c.City.Contains(filter.City));
            }


            if (filter.MinAge.HasValue)
            {
                query = query.Where(c =>
                    c.Age >= filter.MinAge.Value);
            }

            if (filter.MaxAge.HasValue)
            {
                query = query.Where(c =>
                    c.Age <= filter.MaxAge.Value);
            }


            if (filter.FromDate.HasValue)
            {
                query = query.Where(c =>
                    c.CreatedAt >= filter.FromDate.Value);
            }


            if (filter.ToDate.HasValue)
            {
                query = query.Where(c =>
                    c.CreatedAt <= filter.ToDate.Value);
            }

            if (!string.IsNullOrWhiteSpace(filter.FullName))
            {
                var name = filter.FullName.Trim();


                query = query.Where(c =>
                    (
                        c.FName +
                        " " +
                        c.SName +
                        " " +
                        c.TName +
                        " " +
                        c.LName
                    )
                    .Contains(name));
            }


            return query;
        }

        private IQueryable<Case> ApplySorting(
            IQueryable<Case> query,
            CasesReportFilterDto filter)
        {

            if (filter.AgeSort.HasValue)
            {
                query = filter.AgeSort.Value switch
                {
                    AgeSort.Asc =>
                        query.OrderBy(c => c.Age),

                    AgeSort.Desc =>
                        query.OrderByDescending(c => c.Age),

                    _ => query
                };
            }

            if (filter.DateSort.HasValue)
            {
                query = filter.DateSort.Value switch
                {
                    DateSort.Newest =>
                        query.OrderByDescending(c => c.CreatedAt),

                    DateSort.Oldest =>
                        query.OrderBy(c => c.CreatedAt),

                    _ => query
                };
            }


            return query;
        }
    }
}
