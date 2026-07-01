using System;
using System.Collections.Generic;
using System.IO;
using BusinessManager.Application.Services;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace BusinessManager.Reporting;

public class QuestPdfGenerator : IPdfGenerator
{
    private const string CompanyName    = "BRENDA'S PRINTING HUB";
    private const string CompanyPhone   = "Tel: 0703699727";
    private const string CompanyEmail   = "Email: brelinda65@gmail.com";
    private const string CompanyAddress = "Katwe-Kevina road";
    private const string NavyHex        = "#0D47A1";
    private const string NavyLightHex   = "#1565C0";

    public byte[] GenerateFinancialReport(FinancialReportData data)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.MarginHorizontal(1.8f, Unit.Centimetre);
                page.MarginTop(1.5f, Unit.Centimetre);
                page.MarginBottom(1.5f, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(10).FontColor("#222222"));

                page.Header().Element(c => BuildHeader(c, data));
                page.Content().Element(c => BuildContent(c, data));
                page.Footer().Element(BuildFooter);
            });
        }).GeneratePdf();
    }

    private static string? FindLogoPath()
    {
        // Look next to the exe first, then in the working directory
        var candidates = new[]
        {
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "brenda.jpeg"),
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "brenda.jpg"),
            Path.Combine(Directory.GetCurrentDirectory(), "Assets", "brenda.jpeg"),
        };
        foreach (var p in candidates)
            if (File.Exists(p)) return p;
        return null;
    }

    private static void BuildHeader(IContainer container, FinancialReportData data)
    {
        var logoPath = FindLogoPath();

        container.Column(col =>
        {
            // Company header row with logo
            col.Item().Row(row =>
            {
                // Logo
                if (logoPath != null)
                {
                    row.ConstantItem(72).Height(62).Padding(2)
                        .Image(logoPath, ImageScaling.FitArea);
                    row.ConstantItem(14);
                }

                // Name + address
                row.RelativeItem().Column(nameCol =>
                {
                    nameCol.Item().Text(CompanyName)
                        .FontSize(22).Bold().FontColor(NavyHex);
                    nameCol.Item().PaddingTop(2).Text(CompanyAddress)
                        .FontSize(9).FontColor("#555555");
                });

                // Contact (right-aligned)
                row.ConstantItem(200).Column(contactCol =>
                {
                    contactCol.Item().AlignRight().Text(CompanyPhone)
                        .FontSize(9).FontColor("#555555");
                    contactCol.Item().AlignRight().Text(CompanyEmail)
                        .FontSize(9).FontColor("#555555");
                });
            });

            col.Item().PaddingTop(8).LineHorizontal(3).LineColor(NavyHex);
            col.Item().PaddingTop(2).LineHorizontal(1).LineColor(NavyLightHex);

            // Report title bar
            col.Item().PaddingTop(12).PaddingBottom(4).Row(row =>
            {
                row.RelativeItem().Column(titleCol =>
                {
                    titleCol.Item().Text(data.ReportTitle.ToUpper())
                        .FontSize(16).Bold().FontColor(NavyHex);
                    titleCol.Item().PaddingTop(2).Text($"Period: {data.Period}")
                        .FontSize(10).FontColor("#444444");
                });
                row.ConstantItem(160).AlignRight().Column(dateCol =>
                {
                    dateCol.Item().AlignRight().Text("Generated:")
                        .FontSize(9).FontColor("#777777");
                    dateCol.Item().AlignRight().Text(DateTime.Now.ToString("dd MMMM yyyy, HH:mm"))
                        .FontSize(9).FontColor("#555555");
                });
            });

            col.Item().PaddingTop(4).LineHorizontal(1).LineColor("#CCCCCC");
        });
    }

    private static void BuildContent(IContainer container, FinancialReportData data)
    {
        container.PaddingTop(16).Column(col =>
        {
            // Summary cards
            col.Item().PaddingBottom(16).Row(row =>
            {
                SummaryCard(row.RelativeItem(), "TOTAL INCOME",
                    $"UGX {data.TotalIncome:N0}", "#1B5E20", "#E8F5E9");
                row.ConstantItem(12);
                SummaryCard(row.RelativeItem(), "TOTAL EXPENSES",
                    $"UGX {data.TotalExpenses:N0}", "#B71C1C", "#FFEBEE");
                row.ConstantItem(12);
                var profitColor = data.NetProfit >= 0 ? "#0D47A1" : "#B71C1C";
                var profitBg    = data.NetProfit >= 0 ? "#E3F2FD" : "#FFEBEE";
                SummaryCard(row.RelativeItem(), "NET PROFIT",
                    $"UGX {data.NetProfit:N0}", profitColor, profitBg);
            });

            // Transactions count
            col.Item().PaddingBottom(20).Row(row =>
            {
                CountCard(row.RelativeItem(), "Sales Transactions", data.TotalSaleTransactions);
                row.ConstantItem(12);
                CountCard(row.RelativeItem(), "Expense Entries", data.TotalExpenseTransactions);
            });

            // Income breakdown
            if (data.IncomeSummary.Count > 0)
            {
                col.Item().PaddingBottom(4).Text("INCOME BREAKDOWN")
                    .FontSize(12).Bold().FontColor(NavyHex);
                col.Item().PaddingBottom(16).Table(table =>
                {
                    table.ColumnsDefinition(cols =>
                    {
                        cols.RelativeColumn(4);
                        cols.RelativeColumn(2);
                        cols.RelativeColumn(1.5f);
                    });

                    // Header
                    table.Header(h =>
                    {
                        HeaderCell(h.Cell(), "Service / Product");
                        HeaderCell(h.Cell(), "Amount (UGX)");
                        HeaderCell(h.Cell(), "Share %");
                    });

                    bool alt = false;
                    foreach (var line in data.IncomeSummary)
                    {
                        var bg = alt ? "#F5F5F5" : Colors.White;
                        DataCell(table.Cell(), line.Label, bg);
                        DataCellRight(table.Cell(), line.Amount.ToString("N0"), bg);
                        DataCellRight(table.Cell(), $"{line.Percentage:F1}%", bg);
                        alt = !alt;
                    }

                    // Total row
                    table.Cell().ColumnSpan(3).Background("#E3F2FD")
                        .Padding(6).Row(r =>
                        {
                            r.RelativeItem().Text("TOTAL INCOME").Bold().FontSize(10);
                            r.ConstantItem(120).AlignRight()
                                .Text($"UGX {data.TotalIncome:N0}").Bold().FontSize(10);
                        });
                });
            }

            // Expense breakdown
            if (data.ExpenseDetails.Count > 0)
            {
                col.Item().PaddingBottom(4).Text("EXPENSE DETAILS")
                    .FontSize(12).Bold().FontColor("#B71C1C");
                col.Item().PaddingBottom(16).Table(table =>
                {
                    table.ColumnsDefinition(cols =>
                    {
                        cols.ConstantColumn(80);
                        cols.RelativeColumn(2);
                        cols.RelativeColumn(3);
                        cols.RelativeColumn(2);
                    });

                    table.Header(h =>
                    {
                        HeaderCell(h.Cell(), "Date");
                        HeaderCell(h.Cell(), "Category");
                        HeaderCell(h.Cell(), "Description");
                        HeaderCell(h.Cell(), "Amount (UGX)");
                    });

                    bool alt = false;
                    decimal expTotal = 0;
                    foreach (var e in data.ExpenseDetails)
                    {
                        var bg = alt ? "#F5F5F5" : Colors.White;
                        DataCell(table.Cell(), e.Date.ToString("dd/MM/yyyy"), bg);
                        DataCell(table.Cell(), e.Category, bg);
                        DataCell(table.Cell(), e.Description, bg);
                        DataCellRight(table.Cell(), e.Amount.ToString("N0"), bg);
                        expTotal += e.Amount;
                        alt = !alt;
                    }

                    table.Cell().ColumnSpan(4).Background("#FFEBEE")
                        .Padding(6).Row(r =>
                        {
                            r.RelativeItem().Text("TOTAL EXPENSES").Bold().FontSize(10).FontColor("#B71C1C");
                            r.ConstantItem(120).AlignRight()
                                .Text($"UGX {expTotal:N0}").Bold().FontSize(10).FontColor("#B71C1C");
                        });
                });
            }

            // Sales details (if included)
            if (data.SaleDetails.Count > 0 && data.ReportType.Contains("Sales", StringComparison.OrdinalIgnoreCase))
            {
                col.Item().PaddingBottom(4).Text("SALES DETAILS")
                    .FontSize(12).Bold().FontColor(NavyHex);
                col.Item().PaddingBottom(16).Table(table =>
                {
                    table.ColumnsDefinition(cols =>
                    {
                        cols.ConstantColumn(90);
                        cols.RelativeColumn();
                        cols.RelativeColumn(2);
                    });

                    table.Header(h =>
                    {
                        HeaderCell(h.Cell(), "Date & Time");
                        HeaderCell(h.Cell(), "Amount (UGX)");
                        HeaderCell(h.Cell(), "Description");
                    });

                    bool alt = false;
                    foreach (var s in data.SaleDetails)
                    {
                        var bg = alt ? "#F5F5F5" : Colors.White;
                        DataCell(table.Cell(), s.Date.ToString("dd/MM HH:mm"), bg);
                        DataCellRight(table.Cell(), s.Amount.ToString("N0"), bg);
                        DataCell(table.Cell(), s.Description, bg);
                        alt = !alt;
                    }
                });
            }

            // Net profit summary box
            col.Item().PaddingTop(8).Border(1).BorderColor("#CCCCCC")
                .Background(data.NetProfit >= 0 ? "#E8F5E9" : "#FFEBEE")
                .Padding(14).Row(r =>
                {
                    r.RelativeItem().Text("NET PROFIT / (LOSS)")
                        .FontSize(13).Bold()
                        .FontColor(data.NetProfit >= 0 ? "#1B5E20" : "#B71C1C");
                    r.ConstantItem(200).AlignRight()
                        .Text($"UGX {data.NetProfit:N0}")
                        .FontSize(15).Bold()
                        .FontColor(data.NetProfit >= 0 ? "#1B5E20" : "#B71C1C");
                });
        });
    }

    private static void BuildFooter(IContainer container)
    {
        container.PaddingTop(8).Column(col =>
        {
            col.Item().LineHorizontal(1).LineColor("#CCCCCC");
            col.Item().PaddingTop(6).Row(row =>
            {
                row.RelativeItem().Text(CompanyName + " — Confidential")
                    .FontSize(8).FontColor("#999999");
                row.ConstantItem(120).AlignRight()
                    .Text(text =>
                    {
                        text.Span("Page ").FontSize(8).FontColor("#999999");
                        text.CurrentPageNumber().FontSize(8).FontColor("#999999");
                        text.Span(" of ").FontSize(8).FontColor("#999999");
                        text.TotalPages().FontSize(8).FontColor("#999999");
                    });
            });
        });
    }

    private static void SummaryCard(IContainer container, string label, string value,
        string textColor, string bgColor)
    {
        container.Background(bgColor).Border(1).BorderColor("#DDDDDD")
            .Padding(12).Column(col =>
            {
                col.Item().Text(label).FontSize(8).Bold().FontColor(textColor).LetterSpacing(0.5f);
                col.Item().PaddingTop(4).Text(value).FontSize(14).Bold().FontColor(textColor);
            });
    }

    private static void CountCard(IContainer container, string label, int count)
    {
        container.Background("#F5F5F5").Border(1).BorderColor("#DDDDDD")
            .Padding(10).Row(row =>
            {
                row.RelativeItem().Text(label).FontSize(10).FontColor("#555555");
                row.ConstantItem(40).AlignRight()
                    .Text(count.ToString()).FontSize(13).Bold().FontColor(NavyHex);
            });
    }

    private static void HeaderCell(IContainer container, string text)
    {
        container.Background(NavyHex).Padding(7)
            .Text(text).FontSize(10).Bold().FontColor(Colors.White);
    }

    private static void DataCell(IContainer container, string text, string bg)
    {
        container.Background(bg).BorderBottom(1).BorderColor("#EEEEEE")
            .Padding(6).Text(text).FontSize(9);
    }

    private static void DataCellRight(IContainer container, string text, string bg)
    {
        container.Background(bg).BorderBottom(1).BorderColor("#EEEEEE")
            .Padding(6).AlignRight().Text(text).FontSize(9);
    }
}
