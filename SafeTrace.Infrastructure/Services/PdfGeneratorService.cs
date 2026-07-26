using Amazon.Runtime.Internal.Transform;
using Microsoft.AspNetCore.Hosting;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SafeTrace.Application.DTOs.Cases.Request;
using SafeTrace.Application.DTOs.Complaints.Request;
using SafeTrace.Application.DTOs.Complaints.Response;
using SafeTrace.Application.DTOs.Dashboard.Request;
using SafeTrace.Application.DTOs.Dashboard.Response;
using SafeTrace.Application.DTOs.Payment.Request;
using SafeTrace.Application.DTOs.Payment.Response;
using SafeTrace.Application.DTOs.User.Request;
using SafeTrace.Application.DTOs.User.Response;
using SafeTrace.Domain.Enums;
using SafeTrace.Infrastructure.Reports;

namespace SafeTrace.Infrastructure.Services
{
    public class PdfGeneratorService : IPdfGeneratorService
    {
        // ضعي مسار اللوجو هنا، أو مرريه كـ byte[] لو محمّل من الداتابيز/الملفات
        private readonly string _logoPath;

        public PdfGeneratorService(IWebHostEnvironment webHostEnvironment)
        {
            // بيجيب مسار wwwroot الفعلي في أي بيئة (Development / Production)
            _logoPath = Path.Combine(webHostEnvironment.WebRootPath, "Images", "logo.jpg");
        }

        public byte[] GenerateComplaintsPdf(
        List<ComplaintResponseDto> complaints,
        ComplaintStatisticsDto statistics,
        ComplaintFilterDto filter)
        {
            return Document.Create(document =>
            {
                document.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(25);
                    page.PageColor(Colors.White);

                    page.DefaultTextStyle(style => style
                        .FontFamily("Cairo")
                        .FontSize(12));

                    page.ContentFromRightToLeft();

                    page.Header().Column(column =>
                        ReportTemplate.Header(column, "تقرير الشكاوى", _logoPath));

                    page.Content().PaddingTop(15).Column(column =>
                    {
                        column.Spacing(20);

                        ReportTemplate.Filters(column, new Dictionary<string, string>
                        {
                            ["رقم الحالة"] = string.IsNullOrWhiteSpace(filter.CaseCode) ? "الكل" : filter.CaseCode,
                            ["البحث"] = string.IsNullOrWhiteSpace(filter.Search) ? "لا يوجد" : filter.Search,
                            ["الحالة"] = filter.Status.HasValue ? filter.Status.ToString()! : "الكل"
                        });

                        column.Item().Row(row =>
                        {
                            row.Spacing(10);

                            ReportTemplate.Statistics(column,
                                ("إجمالي الشكاوى", $"{statistics.Total}", Colors.Blue.Darken2),
                                ("تم الحل", $"{statistics.Solved}", Colors.Green.Darken2),
                                ("غير محلولة", $"{statistics.UnSolved}", Colors.Red.Darken2)
                            );
                        });

                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(30); // #
                                columns.RelativeColumn(2);   // البريد الإلكتروني
                                columns.RelativeColumn(1);   // رقم الحالة
                                columns.RelativeColumn(1);   // الحالة
                                columns.RelativeColumn(1);   // تاريخ الإنشاء
                            });

                            ReportTemplate.TableHeader(table,
                                "#", "البريد الإلكتروني", "رقم الحالة", "الحالة", "تاريخ الإنشاء");

                            for (int i = 0; i < complaints.Count; i++)
                            {
                                var complaint = complaints[i];

                                table.Cell().Element(c => ReportTemplate.BodyCellStyle(c, i))
                                    .Text($"{i + 1}");

                                table.Cell().Element(c => ReportTemplate.BodyCellStyle(c, i))
                                    .Text(complaint.UserEmail)
                                    .FontSize(9);

                                table.Cell().Element(c => ReportTemplate.BodyCellStyle(c, i))
                                    .Text(complaint.CaseCode ?? "-");

                                table.Cell().Element(c => ReportTemplate.BodyCellStyle(c, i))
                                    .Text(GetStatusName(complaint.ComplaintStatus))
                                    .FontColor(complaint.ComplaintStatus == ComplaintStatus.Solved
                                        ? Colors.Green.Darken2
                                        : Colors.Red.Darken2);

                                table.Cell().Element(c => ReportTemplate.BodyCellStyle(c, i))
                                    .Text(complaint.CreatedAt.ToString("yyyy/MM/dd"));
                            }
                        });
                    });

                    ReportTemplate.Footer(page);
                });
            }).GeneratePdf();
        }

        private static string GetStatusName(ComplaintStatus status)
        {
            return status switch
            {
                ComplaintStatus.Solved => "تم الحل",
                ComplaintStatus.UnSolved => "غير محلولة",
                _ => status.ToString()
            };
        }

        public byte[] GenerateDonationsPdf(
    List<DonationAdminListDto> donations,
    AdminDonationStatisticsDto statistics,
    DonationAdminQueryDto filter)
        {
            return Document.Create(document =>
            {
                document.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(25);
                    page.PageColor(Colors.White);

                    page.DefaultTextStyle(style => style
                        .FontFamily("Cairo")
                        .FontSize(12));


                    page.ContentFromRightToLeft();


                    page.Header()
                        .Column(column =>
                        {
                            ReportTemplate.Header(
                                column,
                                "تقرير التبرعات",
                                _logoPath);
                        });


                    page.Content()
                        .PaddingTop(15)
                        .Column(column =>
                        {
                            column.Spacing(20);


                            #region Filters

                            ReportTemplate.Filters(
                                column,
                                new Dictionary<string, string>
                                {
                            {
                                "حالة الدفع",
                                filter.Status.HasValue
                                    ? GetPaymentStatusName(filter.Status.Value)
                                    : "الكل"
                            },
                            {
                                "البريد الإلكتروني",
                                string.IsNullOrWhiteSpace(filter.userEmail)
                                    ? "الكل"
                                    : filter.userEmail
                            }
                                });


                            #endregion


                            #region Statistics

                            column.Item()
                                .Row(row =>
                                {
                                    row.Spacing(10);

                                    ReportTemplate.Statistics(
                                        column,
                                        (
                                            "إجمالي التبرعات",
                                            statistics.TotalCount.ToString(),
                                            Colors.Blue.Darken2
                                        ),
                                        (
                                            "إجمالي المبلغ",
                                            $"{statistics.TotalAmount} EGP",
                                            Colors.Green.Darken2
                                        ),
                                        (
                                            "تم الدفع",
                                            statistics.SucceededCount.ToString(),
                                            Colors.Green.Darken2
                                        ),
                                        (
                                            "قيد الانتظار",
                                            statistics.PendingCount.ToString(),
                                            Colors.Orange.Darken2
                                        ),
                                        (
                                            "فشل",
                                            statistics.FailedCount.ToString(),
                                            Colors.Red.Darken2
                                        )
                                    );
                                });

                            #endregion



                            #region Table

                            column.Item()
                                .Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.ConstantColumn(30); // #
                                        columns.RelativeColumn(2);  // Email
                                        columns.RelativeColumn();   // Amount
                                        columns.RelativeColumn();   // Status
                                        columns.RelativeColumn();   // Created
                                        columns.RelativeColumn();   // Paid
                                    });


                                    ReportTemplate.TableHeader(
                                        table,
                                        "#",
                                        "البريد الإلكتروني",
                                        "المبلغ",
                                        "الحالة",
                                        "تاريخ الإنشاء",
                                        "تاريخ الدفع"
                                    );


                                    for (int i = 0; i < donations.Count; i++)
                                    {
                                        var donation = donations[i];


                                        table.Cell()
                                            .Element(c =>
                                                ReportTemplate.BodyCellStyle(c, i))
                                            .Text((i + 1).ToString());


                                        table.Cell()
                                            .Element(c =>
                                                ReportTemplate.BodyCellStyle(c, i))
                                            .Text(donation.UserEmail ?? "-")
                                            .FontSize(9);


                                        table.Cell()
                                            .Element(c =>
                                                ReportTemplate.BodyCellStyle(c, i))
                                            .Text($"{donation.Amount}");


                                        table.Cell()
                                            .Element(c =>
                                                ReportTemplate.BodyCellStyle(c, i))
                                            .Text(
                                                GetPaymentStatusName(
                                                    donation.PaymentStatus));


                                        table.Cell()
                                            .Element(c =>
                                                ReportTemplate.BodyCellStyle(c, i))
                                            .Text(
                                                donation.CreateAt
                                                .ToString("yyyy/MM/dd"));


                                        table.Cell()
                                            .Element(c =>
                                                ReportTemplate.BodyCellStyle(c, i))
                                            .Text(
                                                donation.PaidAt.HasValue
                                                ? donation.PaidAt.Value
                                                    .ToString("yyyy/MM/dd")
                                                : "-");
                                    }
                                });

                            #endregion
                        });


                    ReportTemplate.Footer(page);

                });

            }).GeneratePdf();
        }

        private static string GetPaymentStatusName(
        PaymentStatus status)
        {
            return status switch
            {
                PaymentStatus.Succeeded => "تم الدفع",
                PaymentStatus.Pending => "قيد الانتظار",
                PaymentStatus.Failed => "فشل",
                _ => status.ToString()
            };
        }

        public byte[] GenerateCasesPdf(
            List<CaseReportDto> cases,
            CasesStatisticsDto statistics,
            CasesReportFilterDto filter)
        { 
            return Document.Create(document =>
            {
                document.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(25);
                    page.PageColor(Colors.White);

                    page.DefaultTextStyle(style => style
                        .FontFamily("Cairo")
                        .FontSize(12));

                    page.ContentFromRightToLeft();


                    page.Header()
                        .Column(column =>
                        {
                            ReportTemplate.Header(
                                column,
                                "تقرير الحالات",
                                _logoPath);
                        });



                    page.Content()
                        .PaddingTop(15)
                        .Column(column =>
                        {
                            column.Spacing(20);


                            #region Filters
                            

                            ReportTemplate.Filters(
                                column,
                                BuildFilters(filter));

                            #endregion



                            #region Statistics

                            ReportTemplate.Statistics(
                                column,

                                (
                                    "إجمالي الحالات",
                                    statistics.Total.ToString(),
                                    Colors.Blue.Darken2
                                ),

                                (
                                    "عاجلة",
                                    statistics.Urgent.ToString(),
                                    Colors.Red.Darken2
                                ),

                                (
                                    "طويلة المدى",
                                    statistics.LongTerm.ToString(),
                                    Colors.Orange.Darken2
                                ),

                                (
                                    "مجهولة",
                                    statistics.Unknown.ToString(),
                                    Colors.Grey.Darken2
                                ),

                                (
                                    "نشطة",
                                    statistics.Active.ToString(),
                                    Colors.Green.Darken2
                                ),

                                (
                                    "تم العثور عليها",
                                    statistics.Found.ToString(),
                                    Colors.Teal.Darken2
                                )
                            );

                            #endregion



                            #region Table

                            column.Item()
                            .Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.ConstantColumn(30); // #
                                    columns.RelativeColumn(1.5f); // Code
                                    columns.RelativeColumn(2);   // Name
                                    columns.RelativeColumn(1.5f);   // Type
                                    columns.RelativeColumn(1.5f);   // Status
                                    columns.RelativeColumn(.7f);// Age
                                    columns.RelativeColumn(1.5f);// Government
                                    columns.RelativeColumn(1.5f);// City
                                    columns.RelativeColumn(1.5f);// Date
                                });



                                ReportTemplate.TableHeader(
                                    table,
                                    "#",
                                    "الكود",
                                    "الاسم",
                                    "النوع",
                                    "الحالة",
                                    "العمر",
                                    "المحافظة",
                                    "المدينة",
                                    "تاريخ الإنشاء"
                                );



                                for (int i = 0; i < cases.Count; i++)
                                {
                                    var item = cases[i];


                                    table.Cell()
                                        .Element(c => ReportTemplate.BodyCellStyle(c, i))
                                        .Text((i + 1).ToString());


                                    table.Cell()
                                        .Element(c => ReportTemplate.BodyCellStyle(c, i))
                                        .Text(item.CaseCode);



                                    table.Cell()
                                        .Element(c => ReportTemplate.BodyCellStyle(c, i))
                                        .Text(item.FullName);



                                    table.Cell()
                                        .Element(c => ReportTemplate.BodyCellStyle(c, i))
                                        .Text(GetCaseTypeName(item.CaseType));



                                    table.Cell()
                                        .Element(c => ReportTemplate.BodyCellStyle(c, i))
                                        .Text(GetCaseStatusName(item.Status));



                                    table.Cell()
                                        .Element(c => ReportTemplate.BodyCellStyle(c, i))
                                        .Text(item.Age.ToString());



                                    table.Cell()
                                        .Element(c => ReportTemplate.BodyCellStyle(c, i))
                                        .Text(item.Government);



                                    table.Cell()
                                        .Element(c => ReportTemplate.BodyCellStyle(c, i))
                                        .Text(item.City);



                                    table.Cell()
                                        .Element(c => ReportTemplate.BodyCellStyle(c, i))
                                        .Text(item.CreatedAt.ToString("yyyy/MM/dd"));
                                }
                            });

                            #endregion

                        });



                    ReportTemplate.Footer(page);

                });

            }).GeneratePdf();
        }



        private Dictionary<string, string> BuildFilters(
            CasesReportFilterDto filter)
        {
            var result = new Dictionary<string, string>();


            if (filter.Status.HasValue)
                result.Add(
                    "الحالة",
                    GetCaseStatusName(filter.Status.Value));

            if (filter.Type.HasValue)
                result.Add(
                    "نوع الحاله", GetCaseTypeName(filter.Type.Value));


            if (filter.Gender.HasValue)
                result.Add(
                    "النوع",
                    GetGenderName(filter.Gender.Value));



            if (!string.IsNullOrWhiteSpace(filter.Government))
                result.Add(
                    "المحافظة",
                    filter.Government);



            if (!string.IsNullOrWhiteSpace(filter.City))
                result.Add(
                    "المدينة",
                    filter.City);



            if (!string.IsNullOrWhiteSpace(filter.CaseCode))
                result.Add(
                    "كود الحالة",
                    filter.CaseCode);



            if (!string.IsNullOrWhiteSpace(filter.FullName))
                result.Add(
                    "الاسم",
                    filter.FullName);



            if (filter.MinAge.HasValue)
                result.Add(
                    "العمر من",
                    filter.MinAge.Value.ToString());



            if (filter.MaxAge.HasValue)
                result.Add(
                    "العمر إلى",
                    filter.MaxAge.Value.ToString());



            if (filter.FromDate.HasValue)
                result.Add(
                    "من تاريخ",
                    filter.FromDate.Value.ToString("dd/MM/yyyy"));



            if (filter.ToDate.HasValue)
                result.Add(
                    "إلى تاريخ",
                    filter.ToDate.Value.ToString("dd/MM/yyyy"));


            return result;
        }

        private static string GetCaseStatusName(CaseStatus status)
        {
            return status switch
            {
                CaseStatus.Pending => "قيد الانتظار",
                CaseStatus.Active => "نشطة",
                CaseStatus.Deleted => "محذوفة",
                CaseStatus.Found => "تم العثور عليها",
                CaseStatus.Rejected => "مرفوضة",
                CaseStatus.Expired => "منتهية",
                _ => status.ToString()
            };
        }



        private static string GetCaseTypeName(CaseType type)
        {
            return type switch
            {
                CaseType.LongTerm => "طويلة المدى",
                CaseType.Urgent => "عاجلة",
                CaseType.Unknown => "مجهوله",
                _ => type.ToString()
            };
        }

        private static string GetGenderName(Gender gender) 
        {
            return gender switch
            {
                Gender.Male => "ذكر",
                Gender.Female => "انثي",
                _ => gender.ToString()
            };
        }


        public byte[] GenerateUsersPdf(
        List<GetUserDto> users,
        UserStatisticsDto statistics,
        UserFilterDto filter, string? roleName)
        {
            return Document.Create(document =>
            {
                document.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(25);
                    page.PageColor(Colors.White);

                    page.DefaultTextStyle(style => style
                        .FontFamily("Cairo")
                        .FontSize(12));

                    page.ContentFromRightToLeft();


                    page.Header()
                        .Column(column =>
                        {
                            ReportTemplate.Header(
                                column,
                                "تقرير المستخدمين",
                                _logoPath);
                        });



                    page.Content()
                        .PaddingTop(15)
                        .Column(column =>
                        {
                            column.Spacing(20);



                            #region Filters

                            ReportTemplate.Filters(
                                column,
                                BuildUserFilters(filter,roleName));

                            #endregion




                            #region Statistics

                            ReportTemplate.Statistics(
                                column,

                                (
                                    "إجمالي المستخدمين",
                                    statistics.TotalUsers.ToString(),
                                    Colors.Blue.Darken2
                                ),

                                (
                                    "المستخدمين النشطين",
                                    statistics.ActiveUsers.ToString(),
                                    Colors.Green.Darken2
                                ),

                                (
                                    "المستخدمين المحظورين",
                                    statistics.BannedUsers.ToString(),
                                    Colors.Red.Darken2
                                ),

                                (
                                    "تم التحقق",
                                    statistics.VerifiedUsers.ToString(),
                                    Colors.Teal.Darken2
                                ),

                                (
                                    "قيد التحقق",
                                    statistics.PendingVerificationUsers.ToString(),
                                    Colors.Orange.Darken2
                                ),

                                (
                                    "غير موثق",
                                    statistics.UnverifiedUsers.ToString(),
                                    Colors.Grey.Darken2
                                )
                            );

                            #endregion




                            #region Table

                            column.Item()
                                .Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.ConstantColumn(30); // #
                                        columns.RelativeColumn(2); // الاسم
                                        columns.RelativeColumn(2); // البريد
                                        columns.RelativeColumn(1.5f); // الهاتف
                                        columns.RelativeColumn(1.5f); // الدور
                                        columns.RelativeColumn(1.5f); // التحقق
                                        columns.RelativeColumn(1); // الحالة
                                    });



                                    ReportTemplate.TableHeader(
                                        table,
                                        "#",
                                        "الاسم",
                                        "البريد الإلكتروني",
                                        "رقم الهاتف",
                                        "الدور",
                                        "حالة التحقق",
                                        "الحالة"
                                    );



                                    for (int i = 0; i < users.Count; i++)
                                    {
                                        var user = users[i];


                                        table.Cell()
                                            .Element(c =>
                                                ReportTemplate.BodyCellStyle(c, i))
                                            .Text((i + 1).ToString());



                                        table.Cell()
                                            .Element(c =>
                                                ReportTemplate.BodyCellStyle(c, i))
                                            .Text($"{user.FName} {user.LName}");



                                        table.Cell()
                                            .Element(c =>
                                                ReportTemplate.BodyCellStyle(c, i))
                                            .Text(user.Email)
                                            .FontSize(9);



                                        table.Cell()
                                            .Element(c =>
                                                ReportTemplate.BodyCellStyle(c, i))
                                            .Text(user.PhoneNumber);



                                        table.Cell()
                                            .Element(c =>
                                                ReportTemplate.BodyCellStyle(c, i))
                                            .Text(GetRoleName(user.Role));



                                        table.Cell()
                                            .Element(c =>
                                                ReportTemplate.BodyCellStyle(c, i))
                                            .Text(GetVerificationStatusName(
                                                user.VerificationStatus));



                                        table.Cell()
                                            .Element(c =>
                                                ReportTemplate.BodyCellStyle(c, i))
                                            .Text(GetBlockStatusName(
                                                user.IsBlocked))
                                            .FontColor(
                                                user.IsBlocked
                                                ? Colors.Red.Darken2
                                                : Colors.Green.Darken2);
                                    }
                                });

                            #endregion

                        });



                    ReportTemplate.Footer(page);

                });

            }).GeneratePdf();
        }

        private Dictionary<string, string> BuildUserFilters(
        UserFilterDto filter, string? roleName)
        {
            var result = new Dictionary<string, string>();


            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            {
                result.Add(
                    "البحث",
                    filter.SearchTerm);
            }
            if (!string.IsNullOrWhiteSpace(roleName))
            {
                result.Add(
                    "الدور",
                    GetRoleName(roleName));
            }


            if (filter.VerificationStatus.HasValue)
            {
                result.Add(
                    "حالة التحقق",
                    GetVerificationStatusName(
                        filter.VerificationStatus.Value));
            }



            if (filter.IsBlocked.HasValue)
            {
                result.Add(
                    "حالة المستخدم",
                    GetBlockStatusName(
                        filter.IsBlocked.Value));
            }


            return result;
        }


        private static string GetVerificationStatusName(
        VerificationStatus status)
        {
            return status switch
            {
                VerificationStatus.Verified => "موثق",

                VerificationStatus.Pending => "قيد التحقق",

                VerificationStatus.Unverified => "غير موثق",

                _ => status.ToString()
            };
        }

        private static string GetBlockStatusName(
        bool isBlocked)
        {
            return isBlocked
                ? "محظور"
                : "نشط";
        }

        private static string GetRoleName(string role)
        {
            return role switch
            {
                "Admin" => "مسؤول",

                "Moderator" => "مشرف",

                "VerifiedUser" => "مستخدم موثق",

                "User" => "مستخدم غير موثوق",

                "SuperAdmin" => "مدير النظام",

                _ => role
            };
        }
    }
}