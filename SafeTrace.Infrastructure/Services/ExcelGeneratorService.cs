using ClosedXML.Excel;
using SafeTrace.Application.DTOs.Complaints.Request;
using SafeTrace.Application.DTOs.Complaints.Response;
using SafeTrace.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace SafeTrace.Infrastructure.Services
{
    public class ExcelGeneratorService : IExcelGeneratorService
    {
        public byte[] GenerateComplaintsExcel(
        List<ComplaintResponseDto> complaints,
        ComplaintStatisticsDto statistics,
        ComplaintFilterDto filter)
        {
            using var workbook = new XLWorkbook();

            var worksheet = workbook.Worksheets.Add("Complaints");


            // Title
            worksheet.Cell("A1")
                .Value = "لقاء - تقرير الشكاوى";

            worksheet.Cell("A1")
                .Style
                .Font
                .Bold = true;


            worksheet.Cell("A2")
                .Value =
                $"تاريخ إنشاء التقرير : {DateTime.Now:dd/MM/yyyy}";


            // Statistics
            worksheet.Cell("A4")
                .Value = "الإحصائيات";

            worksheet.Cell("A5")
                .Value = "إجمالي الشكاوى";

            worksheet.Cell("B5")
                .Value = statistics.Total;


            worksheet.Cell("A6")
                .Value = "تم الحل";

            worksheet.Cell("B6")
                .Value = statistics.Solved;


            worksheet.Cell("A7")
                .Value = "غير محلولة";

            worksheet.Cell("B7")
                .Value = statistics.UnSolved;



            // Table Header
            var headerRow = 10;

            worksheet.Cell(headerRow, 1)
                .Value = "#";

            worksheet.Cell(headerRow, 2)
                .Value = "البريد الإلكتروني";

            worksheet.Cell(headerRow, 3)
                .Value = "رقم الحالة";

            worksheet.Cell(headerRow, 4)
                .Value = "الحالة";

            worksheet.Cell(headerRow, 5)
                .Value = "تاريخ الإنشاء";


            // Data
            for (int i = 0; i < complaints.Count; i++)
            {
                var row = headerRow + i + 1;

                var complaint = complaints[i];


                worksheet.Cell(row, 1)
                    .Value = i + 1;


                worksheet.Cell(row, 2)
                    .Value = complaint.UserEmail;


                worksheet.Cell(row, 3)
                    .Value = complaint.CaseCode ?? "-";


                worksheet.Cell(row, 4)
                    .Value = GetStatusName(
                        complaint.ComplaintStatus);


                worksheet.Cell(row, 5)
                    .Value = complaint.CreatedAt
                    .ToString("yyyy/MM/dd");
            }


            // Auto size
            worksheet.Columns()
                .AdjustToContents();


            using var stream = new MemoryStream();

            workbook.SaveAs(stream);

            return stream.ToArray();
        }



        private static string GetStatusName(
            ComplaintStatus status)
        {
            return status switch
            {
                ComplaintStatus.Solved => "تم الحل",
                ComplaintStatus.UnSolved => "غير محلولة",
                _ => status.ToString()
            };
        }
    }
}
