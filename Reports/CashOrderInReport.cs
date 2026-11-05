using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;

namespace ChurchBudget_v3_WPF.Reports
{
    public class CashOrderInReport : IDocument
    {
        private readonly CashOrderInDataModel _model;

        public CashOrderInReport(CashOrderInDataModel model)
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
                page.Margin(0.8f, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontFamily("Times New Roman").FontSize(10).FontColor(Colors.Black));

                page.Content().Row(row =>
                {
                    // ЛЕВАЯ ЧАСТЬ: Приходный кассовый ордер
                    row.RelativeItem(1.4f).PaddingRight(15).Column(col => ComposeOrder(col));

                    // СРЕДНЯЯ ЧАСТЬ: ИСПРАВЛЕНО под старый QuestPDF (ExtendVertical вместо ExpandVertical)
                    row.ConstantItem(15).ExtendVertical().AlignCenter().Column(c =>
                    {
                        // Генерируем красивый сквозной пунктир сверху вниз
                        for (int i = 0; i < 24; i++)
                        {
                            c.Item().AlignCenter().Text("¦").FontSize(10).FontColor(Colors.Grey.Darken2);
                        }
                    });

                    // ПРАВАЯ ЧАСТЬ: Квитанция
                    row.RelativeItem(1f).PaddingLeft(15).Column(col => ComposeReceipt(col));
                });
            });
        }

        private void ComposeOrder(ColumnDescriptor col)
        {
            col.Item().Text($"ПРИХОДНЫЙ КАССОВЫЙ ОРДЕР № {_model.OrderNumber}").Bold().FontSize(12);
            col.Item().PaddingTop(2).Row(r =>
            {
                r.ConstantItem(35).Text("Дата: ");
                r.ConstantItem(25).BorderBottom(1).AlignCenter().Text(_model.OrderDate.ToString("dd"));
                r.ConstantItem(60).BorderBottom(1).AlignCenter().Text(_model.OrderDate.ToString("MMMM"));
                r.ConstantItem(45).BorderBottom(1).AlignCenter().Text(_model.OrderDate.ToString("yyyy") + " г.");
            });

            col.Item().PaddingTop(8).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(2);
                    columns.RelativeColumn(1);
                });
                table.Cell().Border(1).Padding(3).AlignCenter().Text("Корреспондирующий счет, субсчет").FontSize(8);
                table.Cell().Border(1).Padding(3).AlignCenter().Text("Сумма, руб. коп.").FontSize(8);

                table.Cell().Border(1).Padding(5).Text("");
                table.Cell().Border(1).Padding(5).AlignRight().Text(_model.Amount.ToString("F2") + " ").Bold();
            });

            col.Item().PaddingTop(10).Row(r =>
            {
                r.ConstantItem(75).Text("Принято от: ");
                r.RelativeItem().BorderBottom(1).Text($" {_model.FromPersonGenitive}").Bold();
            });
            col.Item().PaddingLeft(75).Text("(фамилия, собственное имя и отчество)").FontSize(7).Italic();

            col.Item().PaddingTop(8).Row(r =>
            {
                r.ConstantItem(75).Text("Основание: ");
                r.RelativeItem().BorderBottom(1).Text($" {_model.Basis}");
            });

            col.Item().PaddingTop(8).Row(r =>
            {
                r.ConstantItem(65).Text("Ставка НДС");
                r.ConstantItem(25).BorderBottom(1f).AlignCenter().Text("—");
                r.ConstantItem(25).Text(" % ");
                r.ConstantItem(65).Text("Сумма НДС");
                r.RelativeItem().BorderBottom(1f).AlignCenter().Text("—");
                r.ConstantItem(55).Text(" руб. — коп.");
            });

            col.Item().PaddingTop(8).Row(r =>
            {
                r.ConstantItem(90).Text("Сумма с НДС: ");
                r.RelativeItem().BorderBottom(1).Text($" {_model.AmountInWords}").Bold();
            });
            col.Item().PaddingLeft(90).Text("(прописью)").FontSize(7).Italic();

            col.Item().PaddingTop(5).Row(r =>
            {
                r.ConstantItem(25).Text(" ");
                r.ConstantItem(70).BorderBottom(1).AlignCenter().Text(_model.Amount.ToString("F2")).Bold();
                r.ConstantItem(90).Text(" руб. коп.");
            });
            col.Item().PaddingLeft(45).Text("(цифрами)").FontSize(7).Italic();

            col.Item().PaddingTop(8).Row(r =>
            {
                r.ConstantItem(75).Text("Приложение: ");
                r.RelativeItem().BorderBottom(1).Text($" {_model.Appendix}");
            });

            col.Item().PaddingTop(20).Row(r =>
            {
                r.ConstantItem(110).Text("Настоятель храма ");
                r.RelativeItem().BorderBottom(1).Text("");
                r.ConstantItem(100).AlignRight().Text($"/{_model.RectorName}/");
            });

            col.Item().PaddingTop(6).Row(r =>
            {
                r.ConstantItem(140).Text("Получил казначей прихода ");
                r.RelativeItem().BorderBottom(1).Text("");
                r.ConstantItem(100).AlignRight().Text($"/{_model.TreasurerName}/");
            });
        }

        private void ComposeReceipt(ColumnDescriptor col)
        {
            col.Item().Text(_model.OrganizationName).FontSize(8).Italic();
            col.Item().PaddingTop(10).Text("КВИТАНЦИЯ").Bold().FontSize(12);
            col.Item().Text($"к приходному кассовому ордеру № {_model.OrderNumber}").FontSize(8);
            col.Item().PaddingTop(2).Row(r =>
            {
                r.ConstantItem(25).Text("от ");
                r.ConstantItem(20).BorderBottom(1).AlignCenter().Text(_model.OrderDate.ToString("dd"));
                r.ConstantItem(50).BorderBottom(1).AlignCenter().Text(_model.OrderDate.ToString("MMMM"));
                r.ConstantItem(40).BorderBottom(1).AlignCenter().Text(_model.OrderDate.ToString("yyyy") + " г.");
            });

            col.Item().PaddingTop(15).Row(r =>
            {
                r.ConstantItem(70).Text("Принято от: ");
                r.RelativeItem().BorderBottom(1).Text($" {_model.FromPersonGenitive}").Bold();
            });

            col.Item().PaddingTop(10).Row(r =>
            {
                r.ConstantItem(70).Text("Основание: ");
                r.RelativeItem().BorderBottom(1).Text($" {_model.Basis}");
            });

            col.Item().PaddingTop(10).Row(r =>
            {
                r.ConstantItem(65).Text("Ставка НДС");
                r.ConstantItem(25).BorderBottom(1f).AlignCenter().Text("—");
                r.ConstantItem(20).Text(" %");
                r.ConstantItem(65).Text(" Сумма НДС");
                r.RelativeItem().BorderBottom(1f).AlignCenter().Text("—");
                r.ConstantItem(55).Text(" руб. — коп.");
            });

            col.Item().PaddingTop(10).Row(r =>
            {
                r.ConstantItem(85).Text("Сумма с НДС: ");
                r.RelativeItem().BorderBottom(1).Text($" {_model.AmountInWords}").Bold().FontSize(9);
            });
            col.Item().PaddingLeft(85).Text("(прописью)").FontSize(7).Italic();

            col.Item().PaddingTop(5).Row(r =>
            {
                r.ConstantItem(70).BorderBottom(1).AlignCenter().Text(_model.Amount.ToString("F2")).Bold();
                r.ConstantItem(50).Text(" руб. коп.");
            });

            col.Item().PaddingTop(20).Row(r =>
            {
                r.ConstantItem(100).Text("Настоятель храма ");
                r.RelativeItem().BorderBottom(1).Text("");
                r.ConstantItem(90).AlignRight().Text($"/{_model.RectorName}/").FontSize(9);
            });

            col.Item().PaddingTop(6).Row(r =>
            {
                r.ConstantItem(100).Text("Получил кассир ");
                r.RelativeItem().BorderBottom(1).Text("");
                r.ConstantItem(90).AlignRight().Text($"/{_model.TreasurerName}/").FontSize(9);
            });

            col.Item().PaddingTop(10).Text("М.П.").FontSize(9);
        }
    }
}
