using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;

namespace ChurchBudget_v3_WPF.Reports
{
    public class CashOrderOutReport : IDocument
    {
        private readonly CashOrderInDataModel _model;

        public CashOrderOutReport(CashOrderInDataModel model)
        {
            _model = model;
        }

        public DocumentSettings GetSettings() => DocumentSettings.Default;
        public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

        public void Compose(IDocumentContainer container)
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(1, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontFamily("Times New Roman").FontSize(10).FontColor(Colors.Black));

                page.Content().Column(col =>
                {
                    // Шапка организации
                    col.Item().Text(_model.OrganizationName).FontSize(8).Italic();
                    col.Item().PaddingBottom(5).Text("");

                    // Заголовок РКО
                    col.Item().Row(r =>
                    {
                        r.ConstantItem(200).Text("РАСХОДНЫЙ КАССОВЫЙ ОРДЕР №").Bold().FontSize(11);
                        r.ConstantItem(120).BorderBottom(1).AlignCenter().Text(_model.OrderNumber).Bold().FontSize(11);
                        r.ConstantItem(20).AlignCenter().Text("«");
                        r.ConstantItem(20).BorderBottom(1).AlignCenter().Text(_model.OrderDate.ToString("dd")).Bold();
                        r.ConstantItem(20).AlignCenter().Text("»");
                        r.ConstantItem(80).BorderBottom(1).AlignCenter().Text(_model.OrderDate.ToString("MMMM")).Bold();
                        r.RelativeItem().PaddingLeft(5).Text($"{_model.OrderDate.ToString("yyyy")} г.").Bold();
                    });

                    // Бухгалтерская сетка счетов
                    col.Item().PaddingTop(8).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(4);
                            columns.RelativeColumn(1);
                        });
                        table.Cell().Border(1).Padding(3).Text(" Корреспондирующий счет, субсчет").FontSize(8);
                        table.Cell().Border(1).Padding(3).Text(" Сумма, руб. коп.").FontSize(8);

                        table.Cell().Border(1).Padding(5).Text("");
                        table.Cell().Border(1).Padding(5).AlignRight().Text($"{_model.Amount:N2} ").Bold();
                    });

                    // Выдать
                    col.Item().PaddingTop(12).Row(r =>
                    {
                        r.ConstantItem(45).Text("Выдать");
                        r.RelativeItem().BorderBottom(1).Text($" {_model.FromPersonGenitive}").Bold();
                    });
                    col.Item().PaddingLeft(45).Text("(фамилия, собственное имя и отчество (если таковое имеется))").FontSize(7f).Italic();

                    // Основание
                    col.Item().PaddingTop(8).Row(r =>
                    {
                        r.ConstantItem(60).Text("Основание");
                        r.RelativeItem().BorderBottom(1).Text($" {_model.Basis}");
                    });

                    // Сумма прописью (ИСПРАВЛЕНО: Теперь берется AmountInWords из модели)
                    col.Item().PaddingTop(8).Row(r =>
                    {
                        r.ConstantItem(45).Text("Сумма");
                        r.RelativeItem().BorderBottom(1).Text($" {_model.AmountInWords}").Bold();
                    });
                    col.Item().PaddingLeft(45).Text("(прописью)").FontSize(7f).Italic();

                    // Приложение
                    col.Item().PaddingTop(8).Row(r =>
                    {
                        r.ConstantItem(65).Text("Приложение");
                        r.RelativeItem().BorderBottom(1).Text($" {_model.Appendix}");
                    });

                    // ТАБЛИЦА ПОДПИСЕЙ (ЖЕСТКАЯ БЕЗОПАСНАЯ СЕТКА)
                    col.Item().PaddingTop(15).Table(signTable =>
                    {
                        signTable.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(110); // Название должности
                            columns.ConstantColumn(220); // Линия подписи
                            columns.ConstantColumn(70);  // Подпись текст
                            columns.ConstantColumn(40);  // Отступ
                            columns.ConstantColumn(160); // Линия ФИО
                            columns.RelativeColumn();    // Текст ФИО расшифровка
                        });

                        // Строка 1: Настоятель храма
                        signTable.Cell().Text("Настоятель храма").FontSize(9.5f);
                        signTable.Cell().BorderBottom(1).Text("");
                        signTable.Cell().AlignCenter().Text("(подпись)").FontSize(7f).Italic();
                        signTable.Cell().Text("");
                        signTable.Cell().BorderBottom(1).AlignCenter().Text(_model.RectorName).Bold().FontSize(9.5f);
                        signTable.Cell().PaddingLeft(5).Text("(инициалы, фамилия)").FontSize(7f).Italic();

                        // Разделительный пустой ряд для вертикального отступа
                        for (int k = 0; k < 6; k++) signTable.Cell().PaddingTop(4).Text("");

                        // Строка 2: Главный бухгалтер
                        signTable.Cell().Text("Главный бухгалтер").FontSize(9.5f);
                        signTable.Cell().BorderBottom(1).Text("");
                        signTable.Cell().AlignCenter().Text("(подпись)").FontSize(7f).Italic();
                        signTable.Cell().Text("");
                        signTable.Cell().BorderBottom(1).AlignCenter().Text(_model.TreasurerName).Bold().FontSize(9.5f);
                        signTable.Cell().PaddingLeft(5).Text("(инициалы, фамилия)").FontSize(7f).Italic();
                    });

                    // Место для ручной расписки получателя
                    col.Item().PaddingTop(12).Row(r =>
                    {
                        r.ConstantItem(45).Text("Получил");
                        r.RelativeItem().BorderBottom(1).Text("");
                        r.ConstantItem(110).AlignCenter().Text("(подпись получателя)").FontSize(7f).Italic();
                    });

                    // Паспортные данные
                    col.Item().PaddingTop(10).Row(r =>
                    {
                        r.ConstantItem(110).Text("Предъявлен документ");
                        r.RelativeItem().BorderBottom(1).Text($" {_model.PassportInfo}").FontSize(9);
                    });
                    col.Item().PaddingLeft(110).Text("(данные о документе, удостоверяющем личность получателя)").FontSize(7f).Italic();

                    // Подпись выдачи кассира через сетку
                    col.Item().PaddingTop(12).Table(cashierTable =>
                    {
                        cashierTable.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(110);
                            columns.ConstantColumn(220);
                            columns.ConstantColumn(70);
                            columns.ConstantColumn(40);
                            columns.ConstantColumn(160);
                            columns.RelativeColumn();
                        });

                        cashierTable.Cell().Text("Выдал кассир").FontSize(9.5f);
                        cashierTable.Cell().BorderBottom(1).Text("");
                        cashierTable.Cell().AlignCenter().Text("(подпись)").FontSize(7f).Italic();
                        cashierTable.Cell().Text("");
                        cashierTable.Cell().BorderBottom(1).AlignCenter().Text(_model.TreasurerName).Bold().FontSize(9.5f);
                        cashierTable.Cell().PaddingLeft(5).Text("(инициалы, фамилия)").FontSize(7f).Italic();
                    });
                });
            });
        }
    }
}