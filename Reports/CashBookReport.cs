using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Collections.Generic;
using System.Data;

namespace ChurchBudget_v3_WPF.Reports
{
    public class CashBookModel
    {
        public DateTime Date { get; set; }
        public int PageNumber { get; set; } // Сюда из окна передается стартовый номер операции года
        public int LastPageNumPrevMonth { get; set; }
        public bool IsFirstInMonth { get; set; }
        public bool IsLastInMonth { get; set; }
        public bool IsLastInYear { get; set; }
        public decimal OpeningBalance { get; set; }
        public decimal ClosingBalance { get; set; }
        public decimal TotalIncome { get; set; }
        public decimal TotalExpense { get; set; }
        public DataTable Operations { get; set; }
    }

    public class CashBookReport : IDocument
    {
        private readonly string _mode; // "Title", "Pages", "Stamp"
        private readonly int _year;
        private readonly List<CashBookModel> _pagesData;
        private readonly string _orgName;
        private readonly dynamic _signatures;

        // Конструктор для Листов-вкладышей
        public CashBookReport(int year, List<CashBookModel> pagesData, dynamic signatures)
        {
            _mode = "Pages";
            _year = year;
            _pagesData = pagesData;
            _signatures = signatures;
            _orgName = "Религиозная община";
        }

        // Конструктор для Титульного листа или Штампа
        public CashBookReport(string mode, int year, string orgName, dynamic signatures, List<CashBookModel> pagesData = null)
        {
            _mode = mode;
            _year = year;
            _orgName = orgName;
            _signatures = signatures;
            _pagesData = pagesData;
        }

        public DocumentMetadata GetMetadata() => DocumentMetadata.Default;
        public DocumentSettings GetSettings() => DocumentSettings.Default;

        public void Compose(IDocumentContainer container)
        {
            // Теперь И титульный лист, И страницы вкладышей — АЛЬБОМНЫЕ (Landscape)
            // Только заверительный штамп в самом конце остается книжным
            var size = _mode == "Stamp" ? PageSizes.A4.Portrait() : PageSizes.A4.Landscape();

            container.Page(page =>
            {
                page.Size(size);
                page.Margin(1, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontFamily("Times New Roman").FontSize(10));

                if (_mode == "Title") ComposeTitle(page.Content());
                else if (_mode == "Stamp") ComposeStamp(page.Content());
                else ComposeActivePages(page.Content());
            });
        }

        private void ComposeTitle(IContainer container)
        {
            // Исправлена ширина для альбомной ориентации обложки
            container.AlignCenter().AlignMiddle().Width(650).Border(1.5f).Padding(40).Column(col =>
            {
                col.Item().Text(_orgName).Bold().FontSize(14).Underline().AlignCenter();
                col.Item().Text("(полное наименование юридического лица)").FontSize(8.5f).Italic().AlignCenter();

                col.Item().PaddingBottom(50).Text("");

                col.Item().Text("КАССОВАЯ КНИГА").FontSize(24).Bold().AlignCenter();
                col.Item().Text($"на {_year} год").FontSize(16).AlignCenter();
            });
        }

        private void ComposeActivePages(IDocumentContainer container)
        {
            // На уровне контейнера определяем размер (Landscape для страниц)
            var size = PageSizes.A4.Landscape();
        }

        private void ComposeActivePages(IContainer container)
        {
            container.Column(col =>
            {
                for (int i = 0; i < _pagesData.Count; i++)
                {
                    var dayData = _pagesData[i];

                    // Исправление: Прибавляем индекс текущего листа к номеру последнего листа прошлого месяца
                    int currentSheetNumber = dayData.LastPageNumPrevMonth + (i + 1);

                    col.Item().Element(c => ComposeSingleDay(c, dayData, currentSheetNumber));
                    if (i < _pagesData.Count - 1)
                        col.Item().PageBreak();
                }
            });
        }

        private void ComposeSingleDay(IContainer container, CashBookModel data, int sheetNumber)
        {
            string[] months = { "", "января", "февраля", "марта", "апреля", "мая", "июня", "июля", "августа", "сентября", "октября", "ноября", "декабря" };

            container.Column(column =>
            {
                // Верхний колонтитул листа
                column.Item().AlignRight().Text("Вкладной лист кассовой книги").FontSize(7.5f).Italic();

                // Заголовок листа с датой и номером
                column.Item().Row(row =>
                {
                    row.RelativeItem().Text($"Касса за {data.Date.Day} {months[data.Date.Month]} {data.Date.Year} г.").FontSize(11).Bold();
                    row.ConstantItem(100).Text($"Лист {sheetNumber}").FontSize(10).Bold().AlignRight();
                });

                column.Item().PaddingBottom(5).Text("");

                // Табличная часть
                column.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(70);  // 1. Номер документа (Порядковый номер операции)
                        columns.RelativeColumn();    // 2. От кого получено или кому выдано
                        columns.ConstantColumn(80);  // 3. Номер корр. счета
                        columns.ConstantColumn(85);  // 4. Приход
                        columns.ConstantColumn(85);  // 5. Расход
                    });

                    // Двухрядная шапка таблицы строго по форме РБ
                    table.Header(header =>
                    {
                        // Ряд 1: Текстовые заголовки
                        header.Cell().Row(1).Column(1).Border(1).BorderColor("#000000").Background("#FFFFFF").AlignCenter().AlignMiddle().Text("Номер документа").FontSize(8);
                        header.Cell().Row(1).Column(2).Border(1).BorderColor("#000000").Background("#FFFFFF").AlignCenter().AlignMiddle().Text("От кого получено или кому выдано").FontSize(8);
                        header.Cell().Row(1).Column(3).Border(1).BorderColor("#000000").Background("#FFFFFF").AlignCenter().AlignMiddle().Text("Номер корреспонди-\nрующего\nсчета,\nсубсчета").FontSize(7.5f).LineHeight(1.0f);
                        header.Cell().Row(1).Column(4).Border(1).BorderColor("#000000").Background("#FFFFFF").AlignCenter().AlignMiddle().Text("Приход, рублей").FontSize(8);
                        header.Cell().Row(1).Column(5).Border(1).BorderColor("#000000").Background("#FFFFFF").AlignCenter().AlignMiddle().Text("Расход, рублей").FontSize(8);

                        // Ряд 2: Цифровые указатели колонок (1, 2, 3, 4, 5)
                        for (uint num = 1; num <= 5; num++)
                        {
                            header.Cell().Row(2).Column(num).Border(1).BorderColor("#000000").Background("#FFFFFF").AlignCenter().Text(num.ToString()).FontSize(7.5f).Bold();
                        }
                    });

                    // Строка 1: Остаток на начало дня
                    table.Cell().Border(1).Text("");
                    table.Cell().Border(1).Text(" Остаток на начало дня").Bold().FontSize(8);
                    table.Cell().Border(1).Text("");
                    table.Cell().Border(1).AlignRight().Text(data.OpeningBalance.ToString("F2") + " ").FontSize(8);
                    table.Cell().Border(1).AlignCenter().Text("x").FontSize(8);

                    // Вывод реальных операций из БД (ОДИН чистый исправленный цикл)
                    int localOperationIndex = data.PageNumber;
                    int filledRows = 0;

                    foreach (DataRow row in data.Operations.Rows)
                    {
                        string orderType = row["order_type"].ToString();
                        string orderNum = row["order_number"].ToString(); // Сохраняем полный номер (например, РКО-05062026-001)
                        string counterparty = row["counterparty"].ToString();
                        decimal amount = Convert.ToDecimal(row["amount"]);

                        // 1 колонка: Сквозной порядковый номер операции за год
                        table.Cell().Border(1).AlignCenter().Text(localOperationIndex.ToString()).FontSize(8);

                        // 2 колонка: Номер документа + Контрагент (Бухгалтерское описание действия)
                        table.Cell().Border(1).Text($" {orderNum} {counterparty}").FontSize(8);

                        // 3 колонка: Номер корр. счета (всегда пустой)
                        table.Cell().Border(1).Text("");

                        // 4 колонка: Приход
                        table.Cell().Border(1).AlignRight().Text(orderType == "ПКО" ? amount.ToString("F2") + " " : "- ").FontSize(8);

                        // 5 колонка: Расход
                        table.Cell().Border(1).AlignRight().Text(orderType == "РКО" ? amount.ToString("F2") + " " : "- ").FontSize(8);

                        localOperationIndex++;
                        filledRows++;
                    }

                    // Дополнение пустыми строками строго до 15 строк суммарно
                    int requiredEmptyRows = Math.Max(0, 15 - filledRows);
                    for (int i = 0; i < requiredEmptyRows; i++)
                    {
                        table.Cell().Border(1).Text(""); // 1. Порядковый номер
                        table.Cell().Border(1).Text(""); // 2. Документ и контрагент
                        table.Cell().Border(1).Text(""); // 3. Корр. счет
                        table.Cell().Border(1).AlignRight().Text("- ").FontSize(8);
                        table.Cell().Border(1).AlignRight().Text("- ").FontSize(8);
                    }

                    // Итоги таблицы
                    // Итого по листу
                    table.Cell().ColumnSpan(3).Border(1).AlignRight().Text("Итого по листу ").FontSize(8);
                    table.Cell().Border(1).AlignRight().Text(data.TotalIncome.ToString("F2") + " ").FontSize(8);
                    table.Cell().Border(1).AlignRight().Text(data.TotalExpense.ToString("F2") + " ").FontSize(8);

                    // Итого за день
                    table.Cell().ColumnSpan(3).Border(1).AlignRight().Text("Итого за день ").FontSize(8);
                    table.Cell().Border(1).AlignRight().Text(data.TotalIncome.ToString("F2") + " ").FontSize(8);
                    table.Cell().Border(1).AlignRight().Text(data.TotalExpense.ToString("F2") + " ").FontSize(8);

                    // Остаток на конец дня
                    table.Cell().ColumnSpan(3).Border(1).AlignRight().Text("Остаток на конец дня ").Bold().FontSize(8);
                    table.Cell().Border(1).AlignRight().Text(data.ClosingBalance.ToString("F2") + " ").Bold().FontSize(8);
                    table.Cell().Border(1).AlignCenter().Text("x").FontSize(8);

                    // В том числе на оплату труда
                    table.Cell().ColumnSpan(3).Border(1).AlignRight().Text("В том числе на оплату труда ").FontSize(8);
                    table.Cell().Border(1).AlignRight().Text("- ").FontSize(8);
                    table.Cell().Border(1).AlignCenter().Text("x").FontSize(8);
                });

                column.Item().PaddingBottom(15).Text("");

                // Блок подписей кассира и бухгалтера (на всю ширину листа)
                column.Item().Column(c =>
                {
                    string cashierName = _signatures.CashierName;
                    string accountantName = _signatures.AccountantName;

                    c.Item().Row(r =>
                    {
                        r.ConstantItem(60).Text("Кассир");
                        r.ConstantItem(150).BorderBottom(1).Text("");
                        r.RelativeItem().PaddingLeft(15).Text(cashierName);
                    });

                    c.Item().PaddingBottom(6).Text("");

                    // Количество документов прописью
                    c.Item().Row(r =>
                    {
                        r.ConstantItem(330).Text("Записи в кассовой книге проверил и документы в количестве ");
                        r.ConstantItem(150).BorderBottom(1).AlignCenter().Text(ReportHelper.NumberToWords(data.Operations.Rows.Count)).Bold();
                    });

                    c.Item().Row(r =>
                    {
                        r.ConstantItem(75).Text("приходных и");
                        r.ConstantItem(150).BorderBottom(1).Text("");
                        r.RelativeItem().PaddingLeft(8).Text("расходных получил.");
                    });

                    c.Item().PaddingBottom(6).Text("");

                    c.Item().Row(r =>
                    {
                        r.ConstantItem(60).Text("Бухгалтер");
                        r.ConstantItem(150).BorderBottom(1).Text("");
                        r.RelativeItem().PaddingLeft(15).Text(accountantName);
                    });
                });

                // Блок отметок
                column.Item().PaddingTop(15).AlignLeft().Width(400).Border(1).Padding(6).Column(c =>
                {
                    string prevMonthText = data.IsFirstInMonth ? data.LastPageNumPrevMonth.ToString() : "";
                    string currentMonthText = data.IsLastInMonth ? "X" : "";
                    string currentYearText = data.IsLastInYear ? "X" : "";

                    c.Item().Row(r =>
                    {
                        r.RelativeItem().Text("Номер последнего заполненного листа кассовой книги за предыдущий месяц:");
                        r.ConstantItem(25).Border(1).AlignCenter().Text(prevMonthText);
                    });

                    c.Item().PaddingBottom(3).Text("");

                    c.Item().Row(r =>
                    {
                        r.RelativeItem().Text("Последний лист в текущем месяце:");
                        r.ConstantItem(25).Border(1).AlignCenter().Text(currentMonthText).Bold();
                    });

                    c.Item().PaddingBottom(3).Text("");

                    c.Item().Row(r =>
                    {
                        r.RelativeItem().Text("Последний лист в текущем году:");
                        r.ConstantItem(25).Border(1).AlignCenter().Text(currentYearText).Bold();
                    });
                });
            });
        }

        private void ComposeStamp(IContainer container)
        {
            string rector = _signatures.RectorName;
            string accountant = _signatures.AccountantName;

            string stampOrgName = _orgName != null ? _orgName : "0";
            int shortYear = _year % 100;
            string formatYearStr = shortYear.ToString("D2");

            // Установили мелкий шрифт 8pt, ширину 200 (вместо 320) и плотные внутренние отступы Padding(10)
            container.AlignBottom().AlignRight().Width(200).Border(1).Padding(8).Column(col =>
            {
                col.Item().DefaultTextStyle(x => x.FontSize(8).FontFamily("Times New Roman"));

                col.Item().Text("В настоящей книге пронумеровано,\nпрошнуровано").AlignCenter();
                col.Item().Row(r =>
                {
                    r.ConstantItem(90).Text("и печатью скреплено");
                    r.RelativeItem().BorderBottom(1).AlignCenter().Text(stampOrgName).Bold().FontSize(8.5f);
                    r.ConstantItem(40).Text(" листов.");
                });

                // Уменьшили высоту отступов до 4 и 2 пикселей
                col.Item().PaddingBottom(4).Text("");

                col.Item().Row(r =>
                {
                    r.ConstantItem(65).Text("Настоятель:");
                    r.RelativeItem().BorderBottom(1).Text("");
                    r.ConstantItem(90).PaddingLeft(2).Text(rector).FontSize(8.5f);
                });

                col.Item().PaddingBottom(2).Text("");

                col.Item().Row(r =>
                {
                    r.ConstantItem(65).Text("Казначей:");
                    r.RelativeItem().BorderBottom(1).Text("");
                    r.ConstantItem(90).PaddingLeft(2).Text(accountant).FontSize(8.5f);
                });

                col.Item().PaddingBottom(6).Text("");
                col.Item().Text($"«____» _________________ 20{formatYearStr} г.").AlignCenter();
            });
        }
    }
}