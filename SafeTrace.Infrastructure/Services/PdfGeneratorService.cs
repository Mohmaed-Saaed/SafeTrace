using Microsoft.AspNetCore.Hosting;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SafeTrace.Application.DTOs.Complaints.Request;
using SafeTrace.Application.DTOs.Complaints.Response;
using SafeTrace.Application.DTOs.Payment.Request;
using SafeTrace.Application.DTOs.Payment.Response;
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
    }
}