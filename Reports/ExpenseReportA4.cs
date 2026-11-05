using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Data;

namespace ChurchBudget_v3_WPF.Reports
{
    public class ExpenseReportA4 : IDocument
    {
        private readonly ExpenseDataModel _model;
        private decimal _totalAmount = 0;

        public ExpenseReportA4(ExpenseDataModel model)
        {
            _model = model;

            // Высчитываем общую сумму расходов
            foreach (DataRow row in _model.ItemsTable.Rows)
            {
                _totalAmount += Convert.ToDecimal(row["amount"]);
            }
        }

        public DocumentSettings GetSettings() => DocumentSettings.Default;
        public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

        public void Compose(IDocumentContainer container)
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.5f, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontFamily("Times New Roman").FontSize(10).FontColor(Colors.Black));

                page.Content().Column(col =>
                {
                    // 1. Шапка организации
                    col.Item().AlignCenter().Text(_model.OrganizationName).Bold().FontSize(11);
                    col.Item().AlignCenter().Text(_model.LocationText).FontSize(9).Italic();

                    col.Item().PaddingTop(15).AlignCenter().Text("РАСХОДЫ").Bold().FontSize(14);
                    col.Item().PaddingTop(2).Text($"за {_model.DocDate.ToString("dd.MM.yyyy")}г.").Underline();
                    col.Item().PaddingTop(10).Text("Потрачено:");

                    // 2. Динамическая Таблица расходов
                    col.Item().PaddingTop(10).Table(table =>
                    {
                        // Задаем ширину колонок в зависимости от наличия чеков
                        table.ColumnsDefinition(columns =>
                        {
                            if (_model.HasChecks)
                            {
                                columns.RelativeColumn(5); // Содержание
                                columns.RelativeColumn(2); // Основание
                                columns.RelativeColumn(1.5f); // № Чека
                                columns.ConstantColumn(70); // Сумма
                            }
                            else
                            {
                                columns.RelativeColumn(6); // Содержание
                                columns.RelativeColumn(2.5f); // Основание
                                columns.ConstantColumn(70); // Сумма
                            }
                        });

                        // Шапка таблицы
                        table.Header(header =>
                        {
                            header.Cell().Border(1).Background(Colors.Grey.Lighten3).Padding(4).AlignCenter().Text("Содержание").Bold();
                            header.Cell().Border(1).Background(Colors.Grey.Lighten3).Padding(4).AlignCenter().Text("Основание").Bold();
                            if (_model.HasChecks)
                            {
                                header.Cell().Border(1).Background(Colors.Grey.Lighten3).Padding(4).AlignCenter().Text("№ Чека").Bold();
                            }
                            header.Cell().Border(1).Background(Colors.Grey.Lighten3).Padding(4).AlignCenter().Text("Сумма").Bold();
                        });

                        // Переменная для отслеживания групп (родительских категорий)
                        string currentParent = "";

                        // Заполнение строк данными
                        foreach (DataRow row in _model.ItemsTable.Rows)
                        {
                            string category = row["display_name"].ToString();
                            decimal amount = Convert.ToDecimal(row["amount"]);

                            // Подтягиваем данные из полей, если они есть (в зависимости от вашей структуры DataTable расходов)
                            string basis = row.Table.Columns.Contains("basis") ? row["basis"].ToString() : "";
                            string checkNum = row.Table.Columns.Contains("check_number") ? row["check_number"].ToString() : "";
                            string parent = row.Table.Columns.Contains("parent_name") ? row["parent_name"].ToString() : "";

                            // Логика вывода родительской группы (если она изменилась и не пустая)
                            if (!string.IsNullOrEmpty(parent) && parent != currentParent)
                            {
                                currentParent = parent;

                                // Строка заголовка группы [0.1.11-0.1.12]
                                var cellGroup = table.Cell().RowSpan(1).ColumnSpan(_model.HasChecks ? (uint)3 : (uint)2)
                                                                    .Border(1).Background(Colors.Grey.Lighten4).Padding(4).AlignLeft();
                                cellGroup.Text(parent).Bold();

                                // Сумма по группе (если вы хотите выводить подытог, как в HTML версии)
                                table.Cell().Border(1).Background(Colors.Grey.Lighten4).Padding(4).AlignRight().Text("").Bold();
                            }

                            // Обычная строка расхода
                            // Если категория является подкатегорией (в вашем сервисе вы добавляли пробел), QuestPDF это отобразит
                            table.Cell().Border(1).Padding(3).AlignLeft().Text(category);
                            table.Cell().Border(1).Padding(3).AlignLeft().Text(basis);

                            if (_model.HasChecks)
                            {
                                table.Cell().Border(1).Padding(3).AlignCenter().Text(checkNum);
                            }

                            table.Cell().Border(1).Padding(3).AlignRight().Text(amount > 0 ? amount.ToString("F2") : "0.00");
                        }

                        // Итоговая строка
                        table.Cell().ColumnSpan(_model.HasChecks ? (uint)3 : (uint)2).Border(1).Background(Colors.Grey.Lighten2).Padding(5).Text("ИТОГО РАСХОДОВ").Bold();
                        table.Cell().Border(1).Background(Colors.Grey.Lighten2).Padding(5).AlignRight().Text(_totalAmount.ToString("F2")).Bold();
                    });

                    // 3. Подписи в подвале
                    col.Item().PaddingTop(25).Row(row =>
                    {
                        row.RelativeItem().AlignLeft().Text("Казначей");
                        row.ConstantItem(150).BorderBottom(1).BorderColor(Colors.Black);
                        row.ConstantItem(100).AlignRight().Text($"/{_model.TreasurerName}/");
                    });

                    col.Item().PaddingTop(15).AlignLeft().Text("М.П.");
                });
            });
        }
    }
}