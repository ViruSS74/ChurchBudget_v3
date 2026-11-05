using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Data;

namespace ChurchBudget_v3_WPF.Reports
{
    public class RaportichkaReport : IDocument
    {
        private readonly RaportichkaDataModel _model;
        private decimal _totalAmount = 0;

        public RaportichkaReport(RaportichkaDataModel model)
        {
            _model = model;

            foreach (DataRow row in _model.ItemsTable.Rows)
            {
                _totalAmount += Convert.ToDecimal(row["amount"]);
            }
        }

        // Добавлен обязательный для новых версий метод настроек отчета
        public DocumentSettings GetSettings() => DocumentSettings.Default;

        public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

        public void Compose(IDocumentContainer container)
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A5);
                page.Margin(1, Unit.Centimetre);
                page.PageColor(Colors.White);

                // ИСПРАВЛЕНО: Вместо ForegroundColor() в новых версиях используется FontColor()
                page.DefaultTextStyle(x => x.FontFamily("Times New Roman").FontSize(10).FontColor(Colors.Black));

                page.Content().Column(col =>
                {
                    col.Item().AlignCenter().Text(_model.OrganizationName).Bold().FontSize(11);
                    col.Item().AlignCenter().Text(_model.LocationText).FontSize(9).Italic();

                    col.Item().PaddingTop(10).AlignCenter().Text("РАПОРТИЧКА").Bold().FontSize(13);
                    col.Item().PaddingTop(2).Text($"за {_model.DocDate.ToString("dd.MM.yyyy")}г.").Underline();
                    col.Item().PaddingTop(5).Text("Принято:");

                    col.Item().PaddingTop(5).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn();
                            columns.ConstantColumn(70);
                        });

                        foreach (DataRow row in _model.ItemsTable.Rows)
                        {
                            string category = row["display_name"].ToString();
                            decimal amount = Convert.ToDecimal(row["amount"]);

                            table.Cell().Border(1).BorderColor(Colors.Black).PaddingLeft(5).PaddingVertical(1).Text(category);
                            table.Cell().Border(1).BorderColor(Colors.Black).PaddingRight(5).PaddingVertical(1).AlignRight().Text(amount.ToString("F2"));
                        }

                        table.Cell().Border(1).BorderColor(Colors.Black).PaddingLeft(5).PaddingVertical(3).Text("Итого сдано").Bold();
                        table.Cell().Border(1).BorderColor(Colors.Black).PaddingRight(5).PaddingVertical(3).AlignRight().Text(_totalAmount.ToString("F2")).Bold();
                    });

                    col.Item().PaddingTop(15).Row(row =>
                    {
                        row.RelativeItem().AlignLeft().Text("Казначей");
                        row.ConstantItem(110).BorderBottom(1).BorderColor(Colors.Black);
                        row.ConstantItem(90).AlignRight().Text($"/{_model.TreasurerName}/");
                    });

                    col.Item().PaddingTop(10).AlignLeft().Text("М.П.");
                });
            });
        }
    }
}