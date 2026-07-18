using System.Diagnostics;
using System.Globalization;
using System.IO;
using PdfSharp;
using PdfSharp.Drawing;
using PdfSharp.Pdf;

namespace AgendaLivre.Windows;

public partial class MainWindow
{
    private const string ReportPdfWebsite = "www.balcaolivrepdv.com.br";
    private static bool _reportPdfFontsConfigured;

    private void WriteReportPdf(string outputPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        EnsureReportPdfFontsConfigured();

        var finalPath = Path.GetFullPath(outputPath);
        var outputDirectory = Path.GetDirectoryName(finalPath)
            ?? throw new InvalidOperationException("Não foi possível identificar a pasta de destino do PDF.");
        Directory.CreateDirectory(outputDirectory);

        var temporaryPath = Path.Combine(
            outputDirectory,
            $".{Path.GetFileNameWithoutExtension(finalPath)}.{Guid.NewGuid():N}.tmp.pdf");

        try
        {
            var theme = ThemeById(_data.Settings.ThemeId);
            var businessName = ReportPdfCompactText(BusinessDisplayName());
            var period = ReportPdfCompactText(ReportsPeriodText.Text);
            var chartTitle = ReportPdfCompactText(CurrentReportChartOption());
            var generatedAt = DateTime.Now;

            var accentColor = ReportPdfColor(theme.Accent, "#C23A6A");
            var accentStrongColor = ReportPdfColor(theme.AccentDark, theme.Accent);
            var accentSoftColor = PremiumPdfBlend(accentColor, XColors.White, 0.88);
            var pageBackgroundColor = PremiumPdfBlend(accentColor, XColors.White, 0.965);
            var subtleBackgroundColor = PremiumPdfBlend(accentColor, XColors.White, 0.94);
            var lineColor = PremiumPdfBlend(accentColor, XColors.White, 0.76);
            var inkColor = XColor.FromArgb(37, 27, 32);
            var mutedColor = XColor.FromArgb(113, 101, 108);

            var accentBrush = new XSolidBrush(accentColor);
            var accentStrongBrush = new XSolidBrush(accentStrongColor);
            var accentSoftBrush = new XSolidBrush(accentSoftColor);
            var pageBackgroundBrush = new XSolidBrush(pageBackgroundColor);
            var subtleBackgroundBrush = new XSolidBrush(subtleBackgroundColor);
            var panelBrush = new XSolidBrush(XColors.White);
            var inkBrush = new XSolidBrush(inkColor);
            var mutedBrush = new XSolidBrush(mutedColor);
            var linePen = new XPen(lineColor, 0.7);
            var gridPen = new XPen(PremiumPdfBlend(accentColor, XColors.White, 0.84), 0.55);

            var fontOptions = new XPdfFontOptions(PdfFontEncoding.Unicode, PdfFontEmbedding.TryComputeSubset);
            XFont Font(double size, XFontStyleEx style = XFontStyleEx.Regular) =>
                new("Segoe UI", size, style, fontOptions);

            var microFont = Font(6.5, XFontStyleEx.Bold);
            var footerFont = Font(7);
            var smallFont = Font(7.2);
            var smallBoldFont = Font(7.2, XFontStyleEx.Bold);
            var bodyFont = Font(8.3);
            var bodyBoldFont = Font(8.3, XFontStyleEx.Bold);
            var sectionFont = Font(12.5, XFontStyleEx.Bold);
            var metricValueFont = Font(16.5, XFontStyleEx.Bold);
            var businessFont = Font(16.5, XFontStyleEx.Bold);
            var reportTitleFont = Font(9.4, XFontStyleEx.Bold);

            using var document = new PdfDocument();
            using var businessLogo = TryLoadReportPdfImage(_data.Settings.BusinessLogoPath);
            document.Info.Title = $"Relatório de desempenho - {businessName}";
            document.Info.Author = "Agenda Livre";
            document.Info.Subject = $"Relatório do período {period}";
            document.Info.Keywords = "Agenda Livre, relatório, agenda, desempenho";

            const double margin = 34;
            var pageOne = AddPage();
            using (var graphics = XGraphics.FromPdfPage(pageOne))
            {
                DrawPageBackground(graphics, pageOne);
                DrawHeroHeader(graphics, pageOne);
                DrawMetrics(graphics, pageOne);
                DrawChart(graphics, pageOne);
                DrawInsights(graphics, pageOne);
                DrawFooter(graphics, pageOne, 1, 2);
            }

            var pageTwo = AddPage();
            using (var graphics = XGraphics.FromPdfPage(pageTwo))
            {
                DrawPageBackground(graphics, pageTwo);
                DrawCompactHeader(graphics, pageTwo);
                DrawRankedPanels(graphics, pageTwo);
                DrawFinalBrand(graphics, pageTwo);
                DrawFooter(graphics, pageTwo, 2, 2);
            }

            document.Save(temporaryPath);
            File.Move(temporaryPath, finalPath, overwrite: true);

            PdfPage AddPage()
            {
                var page = document.AddPage();
                page.Size = PageSize.A4;
                return page;
            }

            void DrawPageBackground(XGraphics graphics, PdfPage page)
            {
                graphics.DrawRectangle(pageBackgroundBrush, 0, 0, page.Width.Point, page.Height.Point);
            }

            void DrawHeroHeader(XGraphics graphics, PdfPage page)
            {
                var contentWidth = page.Width.Point - margin * 2;
                var bounds = new XRect(margin, 32, contentWidth, 86);
                PremiumPdfRoundedPanel(graphics, bounds, 13, panelBrush, linePen);
                graphics.DrawRoundedRectangle(
                    accentStrongBrush,
                    new XRect(bounds.X + 16, bounds.Y + 12, 38, 3),
                    new XSize(3, 3));

                var logoBounds = new XRect(bounds.X + 16, bounds.Y + 24, 46, 46);
                PremiumPdfRoundedPanel(graphics, logoBounds, 11, subtleBackgroundBrush, linePen);
                if (businessLogo is not null)
                {
                    PremiumPdfDrawImageFit(
                        graphics,
                        businessLogo,
                        new XRect(logoBounds.X + 5, logoBounds.Y + 5, logoBounds.Width - 10, logoBounds.Height - 10));
                }
                else
                {
                    graphics.DrawString(
                        ReportPdfInitials(businessName),
                        Font(12, XFontStyleEx.Bold),
                        accentStrongBrush,
                        logoBounds,
                        XStringFormats.Center);
                }

                var textX = logoBounds.Right + 13;
                var textWidth = 244;
                graphics.DrawString(
                    "AGENDA LIVRE",
                    microFont,
                    accentStrongBrush,
                    new XRect(textX, bounds.Y + 23, textWidth, 10),
                    XStringFormats.TopLeft);
                graphics.DrawString(
                    PremiumPdfFitText(graphics, businessName, businessFont, textWidth),
                    businessFont,
                    inkBrush,
                    new XRect(textX, bounds.Y + 35, textWidth, 22),
                    XStringFormats.TopLeft);
                graphics.DrawString(
                    "Relatório de desempenho",
                    reportTitleFont,
                    inkBrush,
                    new XRect(textX, bounds.Y + 59, textWidth, 14),
                    XStringFormats.TopLeft);

                var periodBounds = new XRect(bounds.Right - 158, bounds.Y + 19, 142, 28);
                PremiumPdfRoundedPanel(graphics, periodBounds, 14, accentSoftBrush);
                graphics.DrawString(
                    $"PERÍODO  {period}",
                    smallBoldFont,
                    accentStrongBrush,
                    periodBounds,
                    XStringFormats.Center);
                graphics.DrawString(
                    $"Gerado em {generatedAt:dd/MM/yyyy 'às' HH:mm}",
                    smallFont,
                    mutedBrush,
                    new XRect(periodBounds.X, bounds.Y + 56, periodBounds.Width, 12),
                    XStringFormats.TopCenter);
            }

            void DrawMetrics(XGraphics graphics, PdfPage page)
            {
                var contentWidth = page.Width.Point - margin * 2;
                DrawSectionHeading(
                    graphics,
                    margin,
                    138,
                    contentWidth,
                    "Resumo do período",
                    "Os principais números da sua operação.");

                var metrics = _reportsMetrics.Take(6).ToList();
                const double gap = 9;
                const double cardHeight = 61;
                var cardWidth = (contentWidth - gap * 2) / 3;
                var glyphs = new[] { "AG", "OK", "!", "R$", "TM", "%" };
                var top = 173d;

                for (var index = 0; index < 6; index++)
                {
                    var row = index / 3;
                    var column = index % 3;
                    var card = new XRect(
                        margin + column * (cardWidth + gap),
                        top + row * (cardHeight + gap),
                        cardWidth,
                        cardHeight);
                    PremiumPdfRoundedPanel(graphics, card, 9, panelBrush, linePen);

                    var iconBounds = new XRect(card.X + 11, card.Y + 11, 25, 25);
                    graphics.DrawEllipse(accentSoftBrush, iconBounds);
                    graphics.DrawString(
                        glyphs[index],
                        Font(index == 3 ? 6.3 : 6.7, XFontStyleEx.Bold),
                        accentStrongBrush,
                        iconBounds,
                        XStringFormats.Center);

                    if (index >= metrics.Count)
                    {
                        continue;
                    }

                    var metric = metrics[index];
                    var textX = card.X + 44;
                    var textWidth = card.Width - 55;
                    graphics.DrawString(
                        PremiumPdfFitText(graphics, metric.Label, smallFont, textWidth),
                        smallFont,
                        mutedBrush,
                        new XRect(textX, card.Y + 9, textWidth, 10),
                        XStringFormats.TopLeft);
                    graphics.DrawString(
                        PremiumPdfFitText(graphics, metric.Value, metricValueFont, textWidth),
                        metricValueFont,
                        inkBrush,
                        new XRect(textX, card.Y + 21, textWidth, 20),
                        XStringFormats.TopLeft);
                    graphics.DrawString(
                        PremiumPdfFitText(graphics, metric.Hint, smallFont, textWidth),
                        smallFont,
                        mutedBrush,
                        new XRect(textX, card.Y + 45, textWidth, 10),
                        XStringFormats.TopLeft);
                }
            }

            void DrawChart(XGraphics graphics, PdfPage page)
            {
                var contentWidth = page.Width.Point - margin * 2;
                DrawSectionHeading(
                    graphics,
                    margin,
                    324,
                    contentWidth,
                    chartTitle,
                    "Comparação visual do período selecionado.");

                var card = new XRect(margin, 357, contentWidth, 213);
                PremiumPdfRoundedPanel(graphics, card, 11, panelBrush, linePen);

                var chartRows = _activeReportChartRows.Take(8).ToList();
                graphics.DrawString(
                    PremiumPdfFitText(graphics, chartTitle, bodyBoldFont, card.Width - 32),
                    bodyBoldFont,
                    inkBrush,
                    new XRect(card.X + 16, card.Y + 13, card.Width - 32, 12),
                    XStringFormats.TopLeft);
                graphics.DrawString(
                    chartRows.Count == 0 ? "Sem dados neste período." : "Valores distribuídos por categoria ou dia.",
                    smallFont,
                    mutedBrush,
                    new XRect(card.X + 16, card.Y + 29, card.Width - 32, 10),
                    XStringFormats.TopLeft);

                if (chartRows.Count == 0 || chartRows.All(row => row.Value <= 0))
                {
                    var emptyBounds = new XRect(card.X + 16, card.Y + 53, card.Width - 32, 105);
                    PremiumPdfRoundedPanel(graphics, emptyBounds, 9, subtleBackgroundBrush);
                    graphics.DrawEllipse(
                        accentSoftBrush,
                        emptyBounds.X + emptyBounds.Width / 2 - 17,
                        emptyBounds.Y + 20,
                        34,
                        34);
                    graphics.DrawString(
                        "0",
                        Font(12, XFontStyleEx.Bold),
                        accentStrongBrush,
                        new XRect(emptyBounds.X, emptyBounds.Y + 20, emptyBounds.Width, 34),
                        XStringFormats.Center);
                    graphics.DrawString(
                        "Nenhum movimento para exibir",
                        bodyBoldFont,
                        inkBrush,
                        new XRect(emptyBounds.X, emptyBounds.Y + 61, emptyBounds.Width, 12),
                        XStringFormats.TopCenter);
                    graphics.DrawString(
                        "Os dados aparecerão aqui assim que houver registros no período.",
                        smallFont,
                        mutedBrush,
                        new XRect(emptyBounds.X + 20, emptyBounds.Y + 78, emptyBounds.Width - 40, 10),
                        XStringFormats.TopCenter);
                }
                else
                {
                    var maximum = Math.Max(1m, chartRows.Max(row => row.Value));
                    var plotX = card.X + 39;
                    var plotY = card.Y + 51;
                    var plotWidth = card.Width - 57;
                    const double plotHeight = 94;
                    var baseline = plotY + plotHeight;

                    for (var level = 0; level <= 4; level++)
                    {
                        var lineY = baseline - level * plotHeight / 4;
                        graphics.DrawLine(gridPen, plotX, lineY, plotX + plotWidth, lineY);
                        var levelValue = maximum * level / 4;
                        graphics.DrawString(
                            levelValue.ToString(levelValue % 1 == 0 ? "N0" : "N1", Brazil),
                            Font(6.2),
                            mutedBrush,
                            new XRect(card.X + 9, lineY - 4, 24, 8),
                            XStringFormats.TopRight);
                    }

                    var groupWidth = plotWidth / Math.Max(1, chartRows.Count);
                    var barWidth = Math.Min(26, groupWidth * 0.42);
                    for (var index = 0; index < chartRows.Count; index++)
                    {
                        var row = chartRows[index];
                        var centerX = plotX + groupWidth * index + groupWidth / 2;
                        var ratio = row.Value <= 0 ? 0 : Math.Clamp((double)(row.Value / maximum), 0, 1);
                        if (ratio > 0)
                        {
                            var barHeight = Math.Max(4, plotHeight * ratio);
                            graphics.DrawRoundedRectangle(
                                accentStrongBrush,
                                new XRect(centerX - barWidth / 2, baseline - barHeight, barWidth, barHeight),
                                new XSize(5, 5));
                            graphics.DrawString(
                                PremiumPdfFitText(graphics, row.ValueText, smallBoldFont, groupWidth - 4),
                                smallBoldFont,
                                inkBrush,
                                new XRect(centerX - groupWidth / 2 + 2, baseline - barHeight - 13, groupWidth - 4, 10),
                                XStringFormats.TopCenter);
                        }
                        else
                        {
                            graphics.DrawEllipse(accentStrongBrush, centerX - 2, baseline - 2, 4, 4);
                        }

                        var (primaryLabel, secondaryLabel) = PremiumPdfChartLabel(row.Label);
                        graphics.DrawString(
                            PremiumPdfFitText(graphics, primaryLabel, Font(6.4), groupWidth - 4),
                            Font(6.4),
                            mutedBrush,
                            new XRect(centerX - groupWidth / 2 + 2, baseline + 7, groupWidth - 4, 9),
                            XStringFormats.TopCenter);
                        if (!string.IsNullOrWhiteSpace(secondaryLabel))
                        {
                            graphics.DrawString(
                                PremiumPdfFitText(graphics, secondaryLabel, Font(6.4), groupWidth - 4),
                                Font(6.4),
                                mutedBrush,
                                new XRect(centerX - groupWidth / 2 + 2, baseline + 16, groupWidth - 4, 9),
                                XStringFormats.TopCenter);
                        }
                    }
                }

                var total = chartRows.Sum(row => row.Value);
                var totalLabel = chartTitle.Equals(ReportChartRevenue, StringComparison.OrdinalIgnoreCase)
                    ? total.ToString("C0", Brazil)
                    : $"{total:N0} ag.";
                var secondarySummary = chartTitle.Equals(ReportChartStatus, StringComparison.OrdinalIgnoreCase)
                    ? $"{chartRows.Count} status"
                    : chartTitle.Equals(ReportChartRevenue, StringComparison.OrdinalIgnoreCase)
                        ? $"Média {(total / Math.Max(1, chartRows.Count)).ToString("C0", Brazil)}"
                        : $"Média {(total / Math.Max(1, chartRows.Count)).ToString("N1", Brazil)}";
                DrawSummaryPill(graphics, card.X + 16, card.Y + 181, 118, $"Total  {totalLabel}");
                DrawSummaryPill(graphics, card.Right - 134, card.Y + 181, 118, secondarySummary);
            }

            void DrawInsights(XGraphics graphics, PdfPage page)
            {
                var contentWidth = page.Width.Point - margin * 2;
                DrawSectionHeading(
                    graphics,
                    margin,
                    590,
                    contentWidth,
                    "Leituras rápidas",
                    "Pontos importantes para acompanhar o período.");

                var rows = _reportsInsights.Take(6).ToList();
                const double columnGap = 9;
                const double rowGap = 7;
                const double cardHeight = 42;
                var cardWidth = (contentWidth - columnGap) / 2;
                var top = 624d;

                for (var index = 0; index < 6; index++)
                {
                    var rowIndex = index / 2;
                    var columnIndex = index % 2;
                    var bounds = new XRect(
                        margin + columnIndex * (cardWidth + columnGap),
                        top + rowIndex * (cardHeight + rowGap),
                        cardWidth,
                        cardHeight);
                    PremiumPdfRoundedPanel(graphics, bounds, 8, panelBrush, linePen);

                    var iconBounds = new XRect(bounds.X + 9, bounds.Y + 10, 22, 22);
                    graphics.DrawEllipse(accentSoftBrush, iconBounds);
                    graphics.DrawString(
                        (index + 1).ToString(Brazil),
                        Font(6.8, XFontStyleEx.Bold),
                        accentStrongBrush,
                        iconBounds,
                        XStringFormats.Center);

                    if (index >= rows.Count)
                    {
                        continue;
                    }

                    var row = rows[index];
                    const double badgeWidth = 58;
                    var badge = new XRect(bounds.Right - badgeWidth - 9, bounds.Y + 11, badgeWidth, 20);
                    PremiumPdfRoundedPanel(graphics, badge, 10, accentSoftBrush);
                    graphics.DrawString(
                        PremiumPdfFitText(graphics, row.BadgeText, smallBoldFont, badge.Width - 10),
                        smallBoldFont,
                        accentStrongBrush,
                        badge,
                        XStringFormats.Center);

                    var textX = bounds.X + 39;
                    var textWidth = badge.X - textX - 7;
                    graphics.DrawString(
                        PremiumPdfFitText(graphics, row.Name, bodyBoldFont, textWidth),
                        bodyBoldFont,
                        inkBrush,
                        new XRect(textX, bounds.Y + 7, textWidth, 11),
                        XStringFormats.TopLeft);
                    graphics.DrawString(
                        PremiumPdfFitText(graphics, row.Detail, smallFont, textWidth),
                        smallFont,
                        mutedBrush,
                        new XRect(textX, bounds.Y + 23, textWidth, 9),
                        XStringFormats.TopLeft);
                }
            }

            void DrawCompactHeader(XGraphics graphics, PdfPage page)
            {
                var contentWidth = page.Width.Point - margin * 2;
                var bounds = new XRect(margin, 32, contentWidth, 58);
                PremiumPdfRoundedPanel(graphics, bounds, 11, panelBrush, linePen);

                var logoBounds = new XRect(bounds.X + 13, bounds.Y + 11, 36, 36);
                PremiumPdfRoundedPanel(graphics, logoBounds, 9, subtleBackgroundBrush);
                if (businessLogo is not null)
                {
                    PremiumPdfDrawImageFit(
                        graphics,
                        businessLogo,
                        new XRect(logoBounds.X + 4, logoBounds.Y + 4, logoBounds.Width - 8, logoBounds.Height - 8));
                }
                else
                {
                    graphics.DrawString(
                        ReportPdfInitials(businessName),
                        Font(9, XFontStyleEx.Bold),
                        accentStrongBrush,
                        logoBounds,
                        XStringFormats.Center);
                }

                graphics.DrawString(
                    PremiumPdfFitText(graphics, businessName, reportTitleFont, 280),
                    reportTitleFont,
                    inkBrush,
                    new XRect(logoBounds.Right + 11, bounds.Y + 13, 280, 13),
                    XStringFormats.TopLeft);
                graphics.DrawString(
                    $"Relatório de desempenho  •  {period}",
                    smallFont,
                    mutedBrush,
                    new XRect(logoBounds.Right + 11, bounds.Y + 31, 280, 10),
                    XStringFormats.TopLeft);

                var chip = new XRect(bounds.Right - 104, bounds.Y + 16, 88, 26);
                PremiumPdfRoundedPanel(graphics, chip, 13, accentSoftBrush);
                graphics.DrawString(
                    "DETALHES",
                    smallBoldFont,
                    accentStrongBrush,
                    chip,
                    XStringFormats.Center);
            }

            void DrawRankedPanels(XGraphics graphics, PdfPage page)
            {
                var contentWidth = page.Width.Point - margin * 2;
                const double gap = 12;
                var columnWidth = (contentWidth - gap) / 2;
                const double top = 112;
                const double panelHeight = 448;

                DrawRankedPanel(
                    graphics,
                    new XRect(margin, top, columnWidth, panelHeight),
                    "Serviços mais realizados",
                    "Movimento no período",
                    _reportsServices.Take(6).ToList());
                DrawRankedPanel(
                    graphics,
                    new XRect(margin + columnWidth + gap, top, columnWidth, panelHeight),
                    "Profissionais",
                    "Desempenho da equipe",
                    _reportsProfessionals.Take(8).ToList());
            }

            void DrawRankedPanel(
                XGraphics graphics,
                XRect bounds,
                string title,
                string subtitle,
                IReadOnlyList<EstablishmentListRow> rows)
            {
                PremiumPdfRoundedPanel(graphics, bounds, 11, panelBrush, linePen);
                graphics.DrawRoundedRectangle(
                    accentStrongBrush,
                    new XRect(bounds.X + 14, bounds.Y + 14, 4, 23),
                    new XSize(4, 4));
                graphics.DrawString(
                    PremiumPdfFitText(graphics, title, sectionFont, bounds.Width - 86),
                    sectionFont,
                    inkBrush,
                    new XRect(bounds.X + 25, bounds.Y + 13, bounds.Width - 86, 16),
                    XStringFormats.TopLeft);
                graphics.DrawString(
                    PremiumPdfFitText(graphics, subtitle, smallFont, bounds.Width - 86),
                    smallFont,
                    mutedBrush,
                    new XRect(bounds.X + 25, bounds.Y + 31, bounds.Width - 86, 10),
                    XStringFormats.TopLeft);

                var countChip = new XRect(bounds.Right - 51, bounds.Y + 15, 37, 22);
                PremiumPdfRoundedPanel(graphics, countChip, 11, accentSoftBrush);
                graphics.DrawString(
                    rows.Count.ToString(Brazil),
                    smallBoldFont,
                    accentStrongBrush,
                    countChip,
                    XStringFormats.Center);

                if (rows.Count == 0)
                {
                    var empty = new XRect(bounds.X + 13, bounds.Y + 57, bounds.Width - 26, 70);
                    PremiumPdfRoundedPanel(graphics, empty, 8, subtleBackgroundBrush);
                    graphics.DrawString(
                        "Nenhum registro no período",
                        bodyFont,
                        mutedBrush,
                        empty,
                        XStringFormats.Center);
                    return;
                }

                const double rowHeight = 42;
                const double rowGap = 5;
                var rowTop = bounds.Y + 55;
                for (var index = 0; index < rows.Count; index++)
                {
                    var row = rows[index];
                    var rowBounds = new XRect(
                        bounds.X + 12,
                        rowTop + index * (rowHeight + rowGap),
                        bounds.Width - 24,
                        rowHeight);
                    PremiumPdfRoundedPanel(graphics, rowBounds, 8, subtleBackgroundBrush);

                    var rankBounds = new XRect(rowBounds.X + 8, rowBounds.Y + 10, 22, 22);
                    graphics.DrawEllipse(accentSoftBrush, rankBounds);
                    graphics.DrawString(
                        (index + 1).ToString(Brazil),
                        smallBoldFont,
                        accentStrongBrush,
                        rankBounds,
                        XStringFormats.Center);

                    const double resultWidth = 57;
                    var resultBounds = new XRect(
                        rowBounds.Right - resultWidth - 7,
                        rowBounds.Y + 11,
                        resultWidth,
                        20);
                    PremiumPdfRoundedPanel(graphics, resultBounds, 10, panelBrush, linePen);
                    graphics.DrawString(
                        PremiumPdfFitText(graphics, row.BadgeText, smallBoldFont, resultBounds.Width - 9),
                        smallBoldFont,
                        accentStrongBrush,
                        resultBounds,
                        XStringFormats.Center);

                    var textX = rowBounds.X + 38;
                    var textWidth = resultBounds.X - textX - 7;
                    graphics.DrawString(
                        PremiumPdfFitText(graphics, row.Name, bodyBoldFont, textWidth),
                        bodyBoldFont,
                        inkBrush,
                        new XRect(textX, rowBounds.Y + 7, textWidth, 11),
                        XStringFormats.TopLeft);
                    graphics.DrawString(
                        PremiumPdfFitText(graphics, row.Detail, smallFont, textWidth),
                        smallFont,
                        mutedBrush,
                        new XRect(textX, rowBounds.Y + 23, textWidth, 9),
                        XStringFormats.TopLeft);
                }
            }

            void DrawFinalBrand(XGraphics graphics, PdfPage page)
            {
                var contentWidth = page.Width.Point - margin * 2;
                var bounds = new XRect(margin, 700, contentWidth, 72);
                PremiumPdfRoundedPanel(graphics, bounds, 12, accentSoftBrush, linePen);

                var markBounds = new XRect(bounds.X + 17, bounds.Y + 17, 36, 36);
                graphics.DrawEllipse(panelBrush, markBounds);
                graphics.DrawString(
                    "AL",
                    Font(9, XFontStyleEx.Bold),
                    accentStrongBrush,
                    markBounds,
                    XStringFormats.Center);

                graphics.DrawString(
                    "RELATÓRIO GERADO COM",
                    microFont,
                    accentStrongBrush,
                    new XRect(markBounds.Right + 12, bounds.Y + 14, 210, 9),
                    XStringFormats.TopLeft);
                graphics.DrawString(
                    "Agenda Livre",
                    Font(13, XFontStyleEx.Bold),
                    inkBrush,
                    new XRect(markBounds.Right + 12, bounds.Y + 27, 210, 17),
                    XStringFormats.TopLeft);
                graphics.DrawString(
                    ReportPdfWebsite,
                    smallFont,
                    mutedBrush,
                    new XRect(markBounds.Right + 12, bounds.Y + 48, 210, 10),
                    XStringFormats.TopLeft);

                graphics.DrawString(
                    "Gestão simples e profissional",
                    bodyFont,
                    inkBrush,
                    new XRect(bounds.Right - 210, bounds.Y + 20, 190, 12),
                    XStringFormats.TopRight);
                graphics.DrawString(
                    "para a sua agenda.",
                    bodyFont,
                    inkBrush,
                    new XRect(bounds.Right - 210, bounds.Y + 36, 190, 12),
                    XStringFormats.TopRight);
            }

            void DrawSectionHeading(
                XGraphics graphics,
                double x,
                double y,
                double width,
                string title,
                string subtitle)
            {
                graphics.DrawRoundedRectangle(
                    accentStrongBrush,
                    new XRect(x, y + 2, 4, 24),
                    new XSize(4, 4));
                graphics.DrawString(
                    PremiumPdfFitText(graphics, title, sectionFont, width - 14),
                    sectionFont,
                    inkBrush,
                    new XRect(x + 12, y, width - 12, 16),
                    XStringFormats.TopLeft);
                graphics.DrawString(
                    PremiumPdfFitText(graphics, subtitle, smallFont, width - 14),
                    smallFont,
                    mutedBrush,
                    new XRect(x + 12, y + 18, width - 12, 10),
                    XStringFormats.TopLeft);
            }

            void DrawSummaryPill(XGraphics graphics, double x, double y, double width, string text)
            {
                var bounds = new XRect(x, y, width, 21);
                PremiumPdfRoundedPanel(graphics, bounds, 10.5, accentSoftBrush);
                graphics.DrawString(
                    PremiumPdfFitText(graphics, text, smallBoldFont, bounds.Width - 12),
                    smallBoldFont,
                    accentStrongBrush,
                    bounds,
                    XStringFormats.Center);
            }

            void DrawFooter(XGraphics graphics, PdfPage page, int pageNumber, int pageCount)
            {
                var contentWidth = page.Width.Point - margin * 2;
                const double footerY = 806;
                graphics.DrawLine(linePen, margin, footerY - 7, page.Width.Point - margin, footerY - 7);
                graphics.DrawString(
                    $"Agenda Livre  •  {ReportPdfWebsite}",
                    footerFont,
                    mutedBrush,
                    new XRect(margin, footerY, contentWidth * 0.72, 10),
                    XStringFormats.TopLeft);
                graphics.DrawString(
                    $"Página {pageNumber} de {pageCount}",
                    footerFont,
                    mutedBrush,
                    new XRect(margin + contentWidth * 0.72, footerY, contentWidth * 0.28, 10),
                    XStringFormats.TopRight);
            }
        }
        finally
        {
            try
            {
                if (File.Exists(temporaryPath))
                {
                    File.Delete(temporaryPath);
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                Debug.WriteLine($"Não foi possível remover o PDF temporário: {ex.Message}");
            }
        }
    }

    private static void PremiumPdfRoundedPanel(
        XGraphics graphics,
        XRect bounds,
        double radius,
        XBrush fill,
        XPen? border = null)
    {
        var cornerSize = new XSize(radius * 2, radius * 2);
        if (border is null)
        {
            graphics.DrawRoundedRectangle(fill, bounds, cornerSize);
        }
        else
        {
            graphics.DrawRoundedRectangle(border, fill, bounds, cornerSize);
        }
    }

    private static void PremiumPdfDrawImageFit(XGraphics graphics, XImage image, XRect target)
    {
        var imageRatio = image.PixelHeight <= 0 ? 1 : image.PixelWidth / (double)image.PixelHeight;
        var targetRatio = target.Width / target.Height;
        var drawWidth = target.Width;
        var drawHeight = target.Height;

        if (imageRatio > targetRatio)
        {
            drawHeight = target.Width / imageRatio;
        }
        else
        {
            drawWidth = target.Height * imageRatio;
        }

        graphics.DrawImage(
            image,
            target.X + (target.Width - drawWidth) / 2,
            target.Y + (target.Height - drawHeight) / 2,
            drawWidth,
            drawHeight);
    }

    private static string PremiumPdfFitText(XGraphics graphics, string? value, XFont font, double maximumWidth)
    {
        var text = ReportPdfCompactText(value);
        if (string.IsNullOrEmpty(text) || graphics.MeasureString(text, font).Width <= maximumWidth)
        {
            return text;
        }

        const string suffix = "...";
        while (text.Length > 1 && graphics.MeasureString(text + suffix, font).Width > maximumWidth)
        {
            text = text[..^1];
        }

        return text.TrimEnd() + suffix;
    }

    private static (string Primary, string Secondary) PremiumPdfChartLabel(string? value)
    {
        var compact = ReportPdfCompactText(value);
        var commaIndex = compact.IndexOf(',');
        if (commaIndex > 0 && commaIndex < compact.Length - 1)
        {
            return (compact[..commaIndex].Trim(), compact[(commaIndex + 1)..].Trim());
        }

        return (compact, "");
    }

    private static XColor PremiumPdfBlend(XColor first, XColor second, double secondAmount)
    {
        secondAmount = Math.Clamp(secondAmount, 0, 1);
        var firstAmount = 1 - secondAmount;
        return XColor.FromArgb(
            (byte)Math.Round(first.R * firstAmount + second.R * secondAmount),
            (byte)Math.Round(first.G * firstAmount + second.G * secondAmount),
            (byte)Math.Round(first.B * firstAmount + second.B * secondAmount));
    }

    private static XImage? TryLoadReportPdfImage(string? path)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return null;
            }

            var resolvedPath = Path.IsPathRooted(path)
                ? path
                : Path.Combine(AppContext.BaseDirectory, path);
            return File.Exists(resolvedPath) ? XImage.FromFile(resolvedPath) : null;
        }
        catch (Exception ex) when (ex is IOException
                                   or UnauthorizedAccessException
                                   or InvalidOperationException
                                   or ArgumentException
                                   or NotSupportedException)
        {
            Debug.WriteLine($"A logo não pôde ser carregada no PDF: {ex.Message}");
            return null;
        }
    }

    private static void EnsureReportPdfFontsConfigured()
    {
        if (_reportPdfFontsConfigured)
        {
            return;
        }

        try
        {
            PdfSharp.Fonts.GlobalFontSettings.UseWindowsFontsUnderWindows = true;
        }
        catch (InvalidOperationException ex)
        {
            Debug.WriteLine($"Configuração de fontes do PDF ignorada: {ex.Message}");
        }

        _reportPdfFontsConfigured = true;
    }

    private static XColor ReportPdfColor(string? value, string fallback)
    {
        var clean = (string.IsNullOrWhiteSpace(value) ? fallback : value).Trim().TrimStart('#');
        if (clean.Length == 8)
        {
            clean = clean[2..];
        }

        if (clean.Length != 6
            || !byte.TryParse(clean[..2], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var red)
            || !byte.TryParse(clean.Substring(2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var green)
            || !byte.TryParse(clean.Substring(4, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var blue))
        {
            return XColors.Black;
        }

        return XColor.FromArgb(red, green, blue);
    }

    private static string ReportPdfCompactText(string? value)
    {
        return string.Join(
            " ",
            (value ?? "")
                .Replace('\u2011', '-')
                .Replace('\u2013', '-')
                .Replace('\u2014', '-')
                .Replace("\r", " ", StringComparison.Ordinal)
                .Replace("\n", " ", StringComparison.Ordinal)
                .Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    private static string ReportPdfInitials(string? value)
    {
        var initials = (value ?? "")
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Take(2)
            .Where(part => part.Length > 0)
            .Select(part => char.ToUpper(part[0], Brazil));
        var result = string.Concat(initials);
        return string.IsNullOrWhiteSpace(result) ? "AL" : result;
    }
}
