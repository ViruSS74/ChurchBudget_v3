using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Collections.Generic;

namespace ChurchBudget_v3_WPF.Reports
{
    public class CashOrdersPacketReport : IDocument
    {
        private readonly List<CashOrderInDataModel> _orders;

        public CashOrdersPacketReport(List<CashOrderInDataModel> orders)
        {
            _orders = orders;
        }

        public DocumentSettings GetSettings() => DocumentSettings.Default;
        public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

        public void Compose(IDocumentContainer container)
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(0.8f, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontFamily("Times New Roman").FontSize(10).FontColor(Colors.Black));

                page.Content().Column(col =>
                {
                    for (int i = 0; i < _orders.Count; i++)
                    {
                        if (i > 0)
                            col.Item().PageBreak(); // Каждый ордер строго на новой странице пакета

                        var order = _orders[i];

                        if (order.PassportInfo == null) // Признак ПКО (у него нет паспорта в модели)
                        {
                            ComposeSinglePko(col, order);
                        }
                        else // Признак РКО
                        {
                            ComposeSingleRko(col, order);
                        }
                    }
                });
            });
        }

        private void ComposeSinglePko(ColumnDescriptor mainCol, CashOrderInDataModel model)
        {
            mainCol.Item().Row(row =>
            {
                // Слева: Ордер
                row.RelativeItem(1.4f).PaddingRight(15).Column(col =>
                {
                    col.Item().Text($"ПРИХОДНЫЙ КАССОВЫЙ ОРДЕР № {model.OrderNumber}").Bold().FontSize(12);
                    col.Item().PaddingTop(2).Row(r =>
                    {
                        r.ConstantItem(35).Text("Дата: ");
                        r.ConstantItem(25).BorderBottom(1).AlignCenter().Text(model.OrderDate.ToString("dd"));
                        r.ConstantItem(60).BorderBottom(1).AlignCenter().Text(model.OrderDate.ToString("MMMM"));
                        r.ConstantItem(45).BorderBottom(1).AlignCenter().Text(model.OrderDate.ToString("yyyy") + " г.");
                    });
                    col.Item().PaddingTop(8).Table(table =>
                    {
                        table.ColumnsDefinition(columns => { columns.RelativeColumn(2); columns.RelativeColumn(1); });
                        table.Cell().Border(1).Padding(3).AlignCenter().Text("Корреспондирующий счет, субсчет").FontSize(8);
                        table.Cell().Border(1).Padding(3).AlignCenter().Text("Сумма, руб. коп.").FontSize(8);
                        table.Cell().Border(1).Padding(5).Text("");
                        table.Cell().Border(1).Padding(5).AlignRight().Text(model.Amount.ToString("F2") + " ").Bold();
                    });
                    col.Item().PaddingTop(10).Row(r => { r.ConstantItem(75).Text("Принято от: "); r.RelativeItem().BorderBottom(1).Text($" {model.FromPersonGenitive}").Bold(); });
                    col.Item().PaddingTop(8).Row(r => { r.ConstantItem(75).Text("Основание: "); r.RelativeItem().BorderBottom(1).Text($" {model.Basis}"); });
                    col.Item().PaddingTop(8).Row(r => { r.ConstantItem(90).Text("Сумма с НДС: "); r.RelativeItem().BorderBottom(1).Text($" {model.AmountInWords}").Bold(); });
                    col.Item().PaddingTop(8).Row(r => { r.ConstantItem(75).Text("Приложение: "); r.RelativeItem().BorderBottom(1).Text($" {model.Appendix}"); });
                    col.Item().PaddingTop(20).Row(r => { r.ConstantItem(110).Text("Настоятель храма "); r.RelativeItem().BorderBottom(1).Text(""); r.ConstantItem(100).AlignRight().Text($"/{model.RectorName}/"); });
                    col.Item().PaddingTop(6).Row(r => { r.ConstantItem(140).Text("Получил казначей прихода "); r.RelativeItem().BorderBottom(1).Text(""); r.ConstantItem(100).AlignRight().Text($"/{model.TreasurerName}/"); });
                });

                // По центру: Линия отрыва
                row.ConstantItem(15).ExtendVertical().AlignCenter().Column(c =>
                {
                    for (int i = 0; i < 24; i++) c.Item().AlignCenter().Text("¦").FontSize(10).FontColor(Colors.Grey.Darken2);
                });

                // Справа: Квитанция
                row.RelativeItem(1f).PaddingLeft(15).Column(col =>
                {
                    col.Item().Text(model.OrganizationName).FontSize(8).Italic();
                    col.Item().PaddingTop(10).Text("КВИТАНЦИЯ").Bold().FontSize(12);
                    col.Item().Text($"к приходному кассовому ордеру № {model.OrderNumber}").FontSize(8);
                    col.Item().PaddingTop(2).Row(r =>
                    {
                        r.ConstantItem(25).Text("от ");
                        r.ConstantItem(20).BorderBottom(1).AlignCenter().Text(model.OrderDate.ToString("dd"));
                        r.ConstantItem(50).BorderBottom(1).AlignCenter().Text(model.OrderDate.ToString("MMMM"));
                        r.ConstantItem(40).BorderBottom(1).AlignCenter().Text(model.OrderDate.ToString("yyyy") + " г.");
                    });
                    col.Item().PaddingTop(15).Row(r => { r.ConstantItem(70).Text("Принято от: "); r.RelativeItem().BorderBottom(1).Text($" {model.FromPersonGenitive}").Bold(); });
                    col.Item().PaddingTop(10).Row(r => { r.ConstantItem(70).Text("Основание: "); r.RelativeItem().BorderBottom(1).Text($" {model.Basis}"); });
                    col.Item().PaddingTop(10).Row(r => { r.ConstantItem(85).Text("Сумма с НДС: "); r.RelativeItem().BorderBottom(1).Text($" {model.AmountInWords}").Bold().FontSize(9); });
                    col.Item().PaddingTop(20).Row(r => { r.ConstantItem(100).Text("Настоятель храма "); r.RelativeItem().BorderBottom(1).Text(""); r.ConstantItem(90).AlignRight().Text($"/{model.RectorName}/").FontSize(9); });
                    col.Item().PaddingTop(6).Row(r => { r.ConstantItem(100).Text("Получил кассир "); r.RelativeItem().BorderBottom(1).Text(""); r.ConstantItem(90).AlignRight().Text($"/{model.TreasurerName}/").FontSize(9); });
                });
            });
        }

        private void ComposeSingleRko(ColumnDescriptor mainCol, CashOrderInDataModel model)
        {
            mainCol.Item().Text(model.OrganizationName).FontSize(8).Italic();
            mainCol.Item().PaddingBottom(5).Text("");

            mainCol.Item().Row(r =>
            {
                r.ConstantItem(200).Text("РАСХОДНЫЙ КАССОВЫЙ ОРДЕР №").Bold().FontSize(11);
                r.ConstantItem(120).BorderBottom(1).AlignCenter().Text(model.OrderNumber).Bold().FontSize(11);
                r.ConstantItem(20).AlignCenter().Text("«");
                r.ConstantItem(20).BorderBottom(1).AlignCenter().Text(model.OrderDate.ToString("dd")).Bold();
                r.ConstantItem(20).AlignCenter().Text("»");
                r.ConstantItem(80).BorderBottom(1).AlignCenter().Text(model.OrderDate.ToString("MMMM")).Bold();
                r.RelativeItem().PaddingLeft(5).Text($"{model.OrderDate.ToString("yyyy")} г.").Bold();
            });

            mainCol.Item().PaddingTop(8).Table(table =>
            {
                table.ColumnsDefinition(columns => { columns.RelativeColumn(4); columns.RelativeColumn(1); });
                table.Cell().Border(1).Padding(3).Text(" Корреспондирующий счет, субсчет").FontSize(8);
                table.Cell().Border(1).Padding(3).Text(" Сумма, руб. коп.").FontSize(8);
                table.Cell().Border(1).Padding(5).Text("");
                table.Cell().Border(1).Padding(5).AlignRight().Text($"{model.Amount:N2} ").Bold();
            });

            mainCol.Item().PaddingTop(12).Row(r => { r.ConstantItem(45).Text("Выдать"); r.RelativeItem().BorderBottom(1).Text($" {model.FromPersonGenitive}").Bold(); });
            mainCol.Item().PaddingTop(8).Row(r => { r.ConstantItem(60).Text("Основание"); r.RelativeItem().BorderBottom(1).Text($" {model.Basis}"); });
            mainCol.Item().PaddingTop(8).Row(r => { r.ConstantItem(45).Text("Сумма"); r.RelativeItem().BorderBottom(1).Text($" {model.AmountInWords}").Bold(); });
            mainCol.Item().PaddingTop(8).Row(r => { r.ConstantItem(65).Text("Приложение"); r.RelativeItem().BorderBottom(1).Text($" {model.Appendix}"); });

            mainCol.Item().PaddingTop(15).Table(signTable =>
            {
                signTable.ColumnsDefinition(columns => { columns.ConstantColumn(110); columns.ConstantColumn(220); columns.ConstantColumn(70); columns.ConstantColumn(40); columns.ConstantColumn(160); columns.RelativeColumn(); });
                signTable.Cell().Text("Настоятель храма").FontSize(9.5f); signTable.Cell().BorderBottom(1).Text(""); signTable.Cell().AlignCenter().Text("(подпись)").FontSize(7f).Italic(); signTable.Cell().Text(""); signTable.Cell().BorderBottom(1).AlignCenter().Text(model.RectorName).Bold().FontSize(9.5f); signTable.Cell().PaddingLeft(5).Text("(инициалы, фамилия)").FontSize(7f).Italic();
                for (int k = 0; k < 6; k++) signTable.Cell().PaddingTop(4).Text("");
                signTable.Cell().Text("Главный бухгалтер").FontSize(9.5f); signTable.Cell().BorderBottom(1).Text(""); signTable.Cell().AlignCenter().Text("(подпись)").FontSize(7f).Italic(); signTable.Cell().Text(""); signTable.Cell().BorderBottom(1).AlignCenter().Text(model.TreasurerName).Bold().FontSize(9.5f); signTable.Cell().PaddingLeft(5).Text("(инициалы, фамилия)").FontSize(7f).Italic();
            });

            mainCol.Item().PaddingTop(12).Row(r =>
            {
                r.ConstantItem(45).Text("Получил");
                r.RelativeItem().BorderBottom(1).Text("");
                r.ConstantItem(110).AlignCenter().Text("(подпись получателя)").FontSize(7f).Italic();
            }); 
            
            mainCol.Item().PaddingTop(10).Row(r =>
            {
                r.ConstantItem(110).Text("Предъявлен документ");
                r.RelativeItem().BorderBottom(1).Text($" {model.PassportInfo}").FontSize(9);
            });
            
            mainCol.Item().PaddingTop(12).Table(cashierTable =>
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
                cashierTable.Cell().BorderBottom(1).AlignCenter().Text(model.TreasurerName).Bold().FontSize(9.5f);
                cashierTable.Cell().PaddingLeft(5).Text("(инициалы, фамилия)").FontSize(7f).Italic();
            });
        }
    }
}