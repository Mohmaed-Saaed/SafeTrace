using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace SafeTrace.Infrastructure.Reports
{
    internal class ReportTemplate
    {
        #region Header

        public static void Header(
            ColumnDescriptor column,
            string reportTitle,
            string? logoPath = null)
        {
            column.Spacing(5);

            column.Item().Row(row =>
            {
                row.RelativeItem().Column(titleCol =>
                {
                    titleCol.Item()
                        .AlignCenter()
                        .Text("لقاء")
                        .FontSize(24)
                        .Bold()
                        .FontColor(Colors.Blue.Darken2);

                    titleCol.Item()
                        .AlignCenter()
                        .Text(reportTitle)
                        .FontSize(18)
                        .SemiBold()
                        .FontColor(Colors.Grey.Darken3);
                });

                if (!string.IsNullOrWhiteSpace(logoPath) && File.Exists(logoPath))
                {
                    row.ConstantItem(60)
                        .Height(60)
                        .Image(logoPath)
                        .FitArea();
                }
            });

            column.Item()
                .AlignRight()
                .Text($"تاريخ إنشاء التقرير : {DateTime.Now:dd/MM/yyyy hh:mm tt}")
                .FontSize(10)
                .FontColor(Colors.Grey.Darken2);

            column.Item()
                .PaddingTop(10)
                .LineHorizontal(1.5f)
                .LineColor(Colors.Blue.Darken2);
        }

        #endregion

        #region Filters

        public static void Filters(
            ColumnDescriptor column,
            Dictionary<string, string> filters)
        {
            column.Item()
                .Background(Colors.Grey.Lighten5)
                .Border(1)
                .BorderColor(Colors.Grey.Lighten2)
                .CornerRadius(4)
                .Padding(10)
                .Column(filterColumn =>
                {
                    filterColumn.Spacing(3);

                    filterColumn.Item()
                        .Text("الفلاتر المستخدمة")
                        .Bold()
                        .FontSize(15)
                        .FontColor(Colors.Blue.Darken2);

                    foreach (var item in filters)
                    {
                        filterColumn.Item()
                            .Text($"{item.Key} : {item.Value}");
                    }
                });
        }

        #endregion

        #region Statistics

        // كارت واحد لكل إحصائية بلون مخصص (لو محتاجة تلوين حسب الحالة)
        public static void Statistics(
        ColumnDescriptor column,
        params (string Label, string Value, string Color)[] items)
        {
            column.Item()
                .Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                    });


                    foreach (var item in items)
                    {
                        table.Cell()
                            .Padding(5)
                            .Background(Colors.Grey.Lighten5)
                            .Border(1)
                            .BorderColor(item.Color)
                            .CornerRadius(4)
                            .Padding(10)
                            .Column(c =>
                            {
                                c.Item()
                                    .AlignCenter()
                                    .Text(item.Label)
                                    .FontSize(11)
                                    .FontColor(Colors.Grey.Darken2);


                                c.Item()
                                    .AlignCenter()
                                    .Text(item.Value)
                                    .FontSize(20)
                                    .Bold()
                                    .FontColor(item.Color);
                            });
                    }
                });
        }

        // شكل بسيط بدون تلوين (لو مش محتاجة كروت ملونة)
        public static void SimpleStatistics(
            ColumnDescriptor column,
            Dictionary<string, string> statistics)
        {
            column.Item()
                .Background(Colors.Grey.Lighten5)
                .Border(1)
                .BorderColor(Colors.Grey.Lighten2)
                .CornerRadius(4)
                .Padding(10)
                .Column(stats =>
                {
                    stats.Spacing(5);

                    foreach (var item in statistics)
                    {
                        stats.Item()
                            .Text($"{item.Key} : {item.Value}");
                    }
                });
        }

        #endregion

        #region Table

        public static void TableHeader(
            TableDescriptor table,
            params string[] headers)
        {
            table.Header(header =>
            {
                foreach (var item in headers)
                {
                    header.Cell()
                        .Element(HeaderCellStyle)
                        .Text(item);
                }
            });
        }

        public static IContainer HeaderCellStyle(IContainer container)
        {
            return container
                .Background(Colors.Blue.Darken2)
                .Padding(6)
                .AlignCenter()
                .DefaultTextStyle(x => x.FontColor(Colors.White).Bold());
        }

        public static IContainer BodyCellStyle(IContainer container, int rowIndex)
        {
            var isEven = rowIndex % 2 == 0;

            return container
                .Background(isEven ? Colors.White : Colors.Grey.Lighten5)
                .BorderBottom(1)
                .BorderColor(Colors.Grey.Lighten2)
                .Padding(6)
                .AlignCenter();
        }

        #endregion

        #region Footer

        public static void Footer(PageDescriptor page)
        {
            page.Footer().Column(column =>
            {
                column.Item()
                    .LineHorizontal(0.5f)
                    .LineColor(Colors.Grey.Lighten2);

                column.Item().PaddingTop(5).Row(row =>
                {
                    row.RelativeItem()
                        .Text("لقاء - نظام إدارة التقارير")
                        .FontSize(9)
                        .FontColor(Colors.Grey.Darken1);

                    row.RelativeItem().AlignLeft().Text(text =>
                    {
                        text.DefaultTextStyle(x => x.FontSize(9).FontColor(Colors.Grey.Darken1));
                        text.Span("صفحة ");
                        text.CurrentPageNumber();
                        text.Span(" من ");
                        text.TotalPages();
                    });
                });
            });
        }

        #endregion
    }
}
