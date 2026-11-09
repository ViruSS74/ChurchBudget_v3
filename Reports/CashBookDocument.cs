using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Data;
using System.Linq;

namespace ChurchBudget_v3_WPF.Reports
{
    public class CashBookDocument : IDocument
    {
        private readonly string _reportType; // "Title", "DayPage", "Stamp"
        private readonly int _year;
        private readonly DataTable _operations;
        private readonly dynamic _orgInfo;
        private readonly dynamic _signatures;
        private readonly DateTime? _targetDate;
        private readonly int _pageNumber;

        public CashBookDocument(string reportType, int year, DataTable operations, dynamic orgInfo, dynamic signatures, DateTime? targetDate = null, int pageNumber = 1)
        {
            _reportType = reportType;
            _year = year;
            _operations = operations;
            _orgInfo = orgInfo;
            _signatures = signatures;
            _targetDate = targetDate;
            _pageNumber = pageNumber;
        }

        public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

        public DocumentSettings GetSettings() => DocumentSettings.Default;


        public void Compose(IDocumentContainer container)
        {
            // Устанавливаем альбомную ориентацию для вкладыша, книжную для титула и штампа
            var pageStyle = _reportType == "DayPage" ? PageSizes.A4.Landscape() : PageSizes.A4.Portrait();

            container.Page(page =>
            {
                page.Size(pageStyle);
                page.Margin(1, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontFamily("Times New Roman").FontSize(11));

                page.Content().Element(ComposeContent);
            });
        }

        private void ComposeContent(IContainer container)
        {
            if (_reportType == "Title")
                ComposeTitlePage(container);
            else if (_reportType == "Stamp")
                ComposeStampPage(container);
            else
                ComposeDayPage(container);
        }

        // 1. ТИТУЛЬНЫЙ ЛИСТ
        private void ComposeTitlePage(IContainer container)
        {
            string orgName = _orgInfo?["name"]?.ToString() ?? "________________";
            string location = _orgInfo?["location"]?.ToString() ?? "";

            container.AlignCenter().AlignMiddle().Width(500).Border(1.5f).Padding(30).Column(column =>
            {
                column.Item().Text($"{orgName} {location}").Bold().Underline().AlignCenter();

                // Задали отступ снизу (PaddingBottom) прямо для этой надписи,
                column.Item().PaddingBottom(30).Text("(полное наименование юридического лица)").FontSize(9).Italic().AlignCenter();

                column.Item().Text("КАССОВАЯ КНИГА").FontSize(18).Bold().AlignCenter();
                column.Item().Text($"на {_year} год").FontSize(14).AlignCenter();
            });

        }

        // 2. ВКЛАДНОЙ ЛИСТ (ОСНОВНОЙ)
        private void ComposeDayPage(IContainer container)
        {
            container.Column(column =>
            {
                // Шапка листа
                column.Item().Row(row =>
                {
                    row.RelativeItem().Text($"Касса за {_targetDate?.Day} {GetMonthName(_targetDate?.Month ?? 1)} {_year} г.").Bold();
                    row.ConstantItem(100).Text($"Лист {_pageNumber}").Bold().AlignRight();
                });

                column.Item().PaddingBottom(5);

                // Таблица операций
                column.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(80);  // Номер документа
                        columns.RelativeColumn();     // От кого/кому
                        columns.ConstantColumn(80);  // Корр. счет
                        columns.ConstantColumn(90);  // Приход
                        columns.ConstantColumn(90);  // Расход
                    });

                    // Заголовки таблиц
                    table.Header(header =>
                    {
                        header.Cell().Border(1).AlignCenter().Text("Номер документа");
                        header.Cell().Border(1).AlignCenter().Text("От кого получено или кому выдано");
                        header.Cell().Border(1).AlignCenter().Text("Номер корр. счета");
                        header.Cell().Border(1).AlignCenter().Text("Приход, руб.");
                        header.Cell().Border(1).AlignCenter().Text("Расход, руб.");
                    });

                    // Строки данных
                    decimal totalIncome = 0;
                    decimal totalExpense = 0;

                    foreach (DataRow row in _operations.Rows)
                    {
                        string orderType = row["order_type"].ToString();
                        decimal amount = Convert.ToDecimal(row["amount"]);
                        string counterparty = row["counterparty"].ToString();
                        string docNum = $"{orderType}-{row["order_number"]}";

                        if (orderType == "ПКО") totalIncome += amount; else totalExpense += amount;

                        table.Cell().Border(1).AlignCenter().Text(docNum);
                        table.Cell().Border(1).Text($"  {counterparty}");
                        table.Cell().Border(1).AlignCenter().Text("");
                        table.Cell().Border(1).AlignRight().Text(orderType == "ПКО" ? amount.ToString("F2") : "- ");
                        table.Cell().Border(1).AlignRight().Text(orderType == "РКО" ? amount.ToString("F2") : "- ");
                    }
                });

                column.Item().PaddingBottom(15);

                // Подписи (Кассир / Бухгалтер)
                column.Item().Row(row =>
                {
                    row.RelativeItem().Text($"Кассир: ___________ ({_signatures.CashierName})");
                    row.RelativeItem().Text($"Бухгалтер: ___________ ({_signatures.AccountantName})");
                });
            });
        }

        // 3. ЗАВЕРИТЕЛЬНЫЙ ШТАМП (ПОСЛЕДНИЙ ЛИСТ)
        private void ComposeStampPage(IContainer container)
        {
            container.AlignBottom().AlignRight().Width(300).Border(1).Padding(15).Column(column =>
            {
                column.Item().Text($"В настоящей книге пронумеровано, прошнуровано и скреплено печатью {_pageNumber} листов.").AlignCenter();
                column.Item().PaddingBottom(15);
                column.Item().Text($"Руководитель: _________ ({_signatures.RectorName})");
                column.Item().PaddingBottom(5);
                column.Item().Text($"Главный бухгалтер: _________ ({_signatures.AccountantName})");
            });
        }

        private string GetMonthName(int month)
        {
            string[] months = { "", "января", "февраля", "марта", "апреля", "мая", "июня", "июля", "августа", "сентября", "октября", "ноября", "декабря" };
            return months[month];
        }
    }
}
