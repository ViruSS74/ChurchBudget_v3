using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Collections.Generic;
using System.Data.SQLite;

namespace ChurchBudget_v3_WPF.Reports
{
    public class FinancialBudgetReport : IDocument
    {
        private readonly BudgetReportModel _model;

        // Строка подключения к БД, которую мы передадим из окна при создании отчета
        private readonly string _connectionString;

        // Обновленный конструктор: теперь он принимает модель и строку подключения
        public FinancialBudgetReport(BudgetReportModel model, string connectionString)
        {
            _model = model;
            _connectionString = connectionString;

            // СРАЗУ при создании отчета принудительно лечим и пересчитываем расходы из базы данных,
            // полностью подменяя кривой словарь из старой DLL на наш точный расчет!
            LoadCorrectExpensesFromDatabase();
        }

        public DocumentMetadata GetMetadata() => DocumentMetadata.Default;
        public DocumentSettings GetSettings() => DocumentSettings.Default;

        // Наш собственный, независимый метод сбора расходов напрямую по числовым ID
        private void LoadCorrectExpensesFromDatabase()
        {
            if (string.IsNullOrEmpty(_connectionString)) return;

            // Наш точный SQL-запрос, который связывает таблицы по числовому ID (ei.category = ec.id),
            // полностью исключая любые текстовые опечатки, пробелы или проблемы старой DLL
            string sql = @"
                SELECT ec.report_code, SUM(ei.amount) as total_amount
                FROM expense_items ei
                JOIN expense_categories ec ON CAST(ei.category AS INTEGER) = ec.id
                GROUP BY ec.report_code;";

            try
            {
                // Создаем новый чистый словарь для подмены данных
                var correctExpenses = new Dictionary<string, decimal>();

                using (var conn = new SQLiteConnection(_connectionString))
                {
                    conn.Open();
                    using (var cmd = new SQLiteCommand(sql, conn))
                    {
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string code = reader["report_code"] != DBNull.Value ? reader["report_code"].ToString() : "";
                                decimal amount = reader["total_amount"] != DBNull.Value ? Convert.ToDecimal(reader["total_amount"]) : 0;

                                if (!string.IsNullOrEmpty(code))
                                {
                                    correctExpenses[code] = amount;
                                }
                            }
                        }
                    }
                }

                // Перезаписываем данные в модели на наши точные и честные результаты!
                if (correctExpenses.Count > 0)
                {
                    _model.ExpenseByCode = correctExpenses;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Error FinancialBudgetReport SQL] {ex.Message}");
            }
        }

        public void Compose(IDocumentContainer container)
        {
            container.Page(page =>
            {
                page.Margin(40);
                page.Size(PageSizes.A4);
                page.DefaultTextStyle(x => x.FontSize(9.5f).FontFamily("Arial"));

                page.Content().Column(column =>
                {
                    // === 1. ШАПКА ОТЧЕТА ===
                    column.Item().AlignCenter().Text("Борисовская епархия Белорусской Православной Церкви Московского Патриархата").Bold().FontSize(10);
                    column.Item().PaddingBottom(10).Text("");
                    column.Item().AlignCenter().Text("Финансовый отчет о движении денежных средств").Bold().FontSize(12);

                    column.Item().AlignCenter().Row(r =>
                {
                    r.ConstantItem(20).Text("за ").Bold().FontSize(11);
                    r.RelativeItem().BorderBottom(1).AlignCenter().Text(_model.PeriodText).Bold().FontSize(11);
                });
                    column.Item().PaddingBottom(8).Text("");

                    column.Item().Row(r =>
                {
                    r.ConstantItem(15).Text("В ").Italic().FontSize(9);
                    r.RelativeItem().BorderBottom(1).AlignCenter().Text($"{_model.OrganizationName}, {_model.Location}, {_model.Deanery}").Italic().FontSize(9);
                });
                    column.Item().AlignCenter().Text("(наименование прихода, место расположения, благочиние)").FontSize(7.5f).Italic();
                    column.Item().PaddingBottom(15).Text("");

                    // === 2. ТАБЛИЧНАЯ ЧАСТЬ ===
                    column.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(30);  // Стр №
                    columns.RelativeColumn();    // Содержание
                    columns.ConstantColumn(70);  // Сумма
                    columns.ConstantColumn(100); // Примечание
                });

                    table.Header(header =>
                {
                    header.Cell().Row(1).Column(1).Border(1).Background("#FFFFFF").AlignCenter().Text("Стр№").Bold().FontSize(8.5f);
                    header.Cell().Row(1).Column(2).Border(1).Background("#FFFFFF").AlignCenter().Text("Содержание").Bold().FontSize(8.5f);
                    header.Cell().Row(1).Column(3).Border(1).Background("#FFFFFF").AlignCenter().Text("Сумма\n(касса + р/счет)").Bold().FontSize(8.5f);
                    header.Cell().Row(1).Column(4).Border(1).Background("#FFFFFF").AlignCenter().Text("Примечание").Bold().FontSize(8.5f);
                    for (uint num = 1; num <= 4; num++)
                    {
                        header.Cell().Row(2).Column(num).Border(1).Background("#FFFFFF").AlignCenter().Text(num.ToString()).FontSize(8).Bold();
                    }
                });

                    // Универсальные функции локального расчета
                    decimal GetGroupSum(Dictionary<string, decimal> source, params string[] codes)
                    {
                        decimal sum = 0;
                        foreach (var code in codes)
                        {
                            if (source.ContainsKey(code))
                                sum += source[code];
                        }
                        return sum;
                    }

                    void AddHeaderRow(string num, string title)
                    {
                        table.Cell().Border(1).AlignCenter().Text(num).Bold();
                        table.Cell().Border(1).Text($" {title}").Bold();
                        table.Cell().Border(1).Text("");
                        table.Cell().Border(1).Text("");
                    }

                    void AddDataRow(string title, string code, Dictionary<string, decimal> source, bool isSubItem = true)
                    {
                        table.Cell().Border(1).Text("");
                        table.Cell().Border(1).Text(isSubItem ? $"    {title}" : $" {title}");
                        decimal val = source.ContainsKey(code) ? source[code] : 0;
                        table.Cell().Border(1).AlignRight().Text(val > 0 ? val.ToString("F2") + " " : "0.00 ");
                        table.Cell().Border(1).Text("");
                    }

                    void AddParentCalculatedRow(string title, string[] childCodes, Dictionary<string, decimal> source)
                    {
                        table.Cell().Border(1).Text("");
                        table.Cell().Border(1).Text($" {title}");
                        decimal groupSum = GetGroupSum(source, childCodes);
                        table.Cell().Border(1).AlignRight().Text(groupSum > 0 ? groupSum.ToString("F2") + " " : "0.00 ");
                        table.Cell().Border(1).Text("");
                    }

                    // --- 1. НАЧАЛЬНЫЙ ОСТАТОК ---
                    table.Cell().Border(1).AlignCenter().Text("1").Bold();
                    table.Cell().Border(1).Text(" Остаток средств на начало периода").Bold();
                    table.Cell().Border(1).AlignRight().Text(_model.OpeningBalance.ToString("F2") + " ").Bold();
                    table.Cell().Border(1).Text("");

                    // --- 2. ДОХОДЫ ---
                    AddHeaderRow("2", "Доходы (поступления)");
                    table.Cell().Border(1).Text(""); table.Cell().Border(1).Text("   в т.ч (статьи доходов):").Italic(); table.Cell().Border(1).Text(""); table.Cell().Border(1).Text("");
                    AddDataRow("Скарбонка", "INC_BOX", _model.IncomeByCode);
                    AddDataRow("Крещение", "INC_BAPTISM", _model.IncomeByCode);
                    AddDataRow("Отпевание", "INC_FUNER", _model.IncomeByCode);
                    AddDataRow("Венчание", "INC_WED", _model.IncomeByCode);
                    AddDataRow("Свечи", "INC_CANDLES", _model.IncomeByCode);
                    AddDataRow("Церковная утварь", "INC_UTEN", _model.IncomeByCode);
                    AddDataRow("Литература", "INC_BOOKS", _model.IncomeByCode);
                    AddDataRow("Изделия из драгоценных металлов религиозного назначения", "INC_GOLD", _model.IncomeByCode);
                    AddDataRow("Записки", "INC_NOTES", _model.IncomeByCode);
                    AddDataRow("Благотворительная помощь (без предоставления отчета)", "INC_CHARHELP", _model.IncomeByCode);
                    AddDataRow("Спонсорская помощь (с предоставлением отчета)", "INC_SPONCHELP", _model.IncomeByCode);
                    AddDataRow("Целевое пожертвование на нужды храма", "INC_TARGDONAT", _model.IncomeByCode);
                    AddDataRow("Прочие доходы", "INC_OTHER", _model.IncomeByCode);
                    decimal totalIncome = 0; foreach (var v in _model.IncomeByCode.Values) totalIncome += v;
                    table.Cell().Border(1).Text("");
                    table.Cell().Border(1).Text(" Итого поступило за период (по стр. 2)").Bold();
                    table.Cell().Border(1).AlignRight().Text(totalIncome.ToString("F2") + " ").Bold();
                    table.Cell().Border(1).Text("");
                    
                    // --- 3. РАСХОДЫ ---
                    AddHeaderRow("3", "Расходы");
                    table.Cell().Border(1).Text(""); table.Cell().Border(1).Text("   в т.ч (статьи расходов):").Italic(); table.Cell().Border(1).Text(""); table.Cell().Border(1).Text("");
                    
                    // 1. Коммунальные
                    AddParentCalculatedRow("1. Коммунальные и платежи", new[] { "EXP_ELECTR", "EXP_HEAT", "EXP_WATER", "EXP_LINK", "EXP_OTHERS", "EXP_COMM" }, _model.ExpenseByCode);
                    AddDataRow("Электроэнергия", "EXP_ELECTR", _model.ExpenseByCode);
                    AddDataRow("Отопление", "EXP_HEAT", _model.ExpenseByCode);
                    AddDataRow("Вода и канализация", "EXP_WATER", _model.ExpenseByCode);
                    AddDataRow("Связь (телефон, интернет)", "EXP_LINK", _model.ExpenseByCode);
                    AddDataRow("Другие расходы", "EXP_OTHERS", _model.ExpenseByCode);
                    
                    // 2. Транспортные
                    AddParentCalculatedRow("2. Транспортные расходы (топливо, запчасти, оплата страхового полиса, дорожный сбор)", new[] { "EXP_FUEL", "EXP_SPARE", "EXP_INSURE", "EXP_RTOLL", "EXP_TRANSP" }, _model.ExpenseByCode);
                    
                    // 3. Хозяйственные
                    AddDataRow("3. Хозяйственные расходы", "EXP_HOUSE", _model.ExpenseByCode, false);
                    
                    // 4. Ремонтно-строительные
                    AddParentCalculatedRow("4. Ремонтно-строительные работы", new[] { "EXP_BUILD", "EXP_BUILDPAY", "EXP_REPL" }, _model.ExpenseByCode);
                    AddDataRow("Стройматериалы", "EXP_BUILD", _model.ExpenseByCode);
                    AddDataRow("Оплата за строительные работы", "EXP_BUILDPAY", _model.ExpenseByCode);
                    
                    // 5. Церковная лавка
                    AddParentCalculatedRow("5. Церковная лавка", new[] { "EXP_CANDLES", "EXP_CHUTEN", "EXP_BOOKS", "EXP_GOLD", "EXP_OTHERPROD", "EXP_SHOP" }, _model.ExpenseByCode);
                    AddDataRow("Свечи", "EXP_CANDLES", _model.ExpenseByCode);
                    AddDataRow("Церковная утварь", "EXP_CHUTEN", _model.ExpenseByCode);
                    AddDataRow("Литература", "EXP_BOOKS", _model.ExpenseByCode);
                    AddDataRow("Изделия из драгоценных металлов религиозного назначения", "EXP_GOLD", _model.ExpenseByCode);
                    AddDataRow("Прочие изделия", "EXP_OTHERPROD", _model.ExpenseByCode);
                    
                    // 6. Расходы на оплату труда
                    AddParentCalculatedRow("6. Расходы на оплату труда", new[] { "EXP_PAYM", "EXP_TAX", "EXP_FINSUP" }, _model.ExpenseByCode);
                    AddDataRow("Зарплата", "EXP_PAYM", _model.ExpenseByCode);
                    AddDataRow("Налоги", "EXP_TAX", _model.ExpenseByCode);
                    AddDataRow("Материальная поддержка", "EXP_FINSUP", _model.ExpenseByCode);
                    
                    // 7. Благотворительность
                    AddDataRow("7. Благотворительная помощь", "EXP_CHAR", _model.ExpenseByCode, false);
                    
                    // 8. Взносы
                    AddParentCalculatedRow("8. Взносы", new[] { "EXP_DIOCONTR", "EXP_ADDCONTR", "EXP_OTHERCONTR", "EXP_CONTR" }, _model.ExpenseByCode);
                    AddDataRow("Епархиальные взносы", "EXP_DIOCONTR", _model.ExpenseByCode);
                    AddDataRow("Дополнительный взнос", "EXP_ADDCONTR", _model.ExpenseByCode);
                    AddDataRow("Прочие взносы", "EXP_OTHERCONTR", _model.ExpenseByCode);
                    
                    // 9-17 Остальные категории расходов
                    AddDataRow("9. Расходы на организацию и проведение праздничных приходских и прочих мероприятий...", "EXP_HOLD", _model.ExpenseByCode, false);
                    AddDataRow("10. Расходы на содержание библиотеки", "EXP_LIB", _model.ExpenseByCode, false);
                    AddDataRow("11. Расходы на социальную работу", "EXP_SOC", _model.ExpenseByCode, false);
                    AddDataRow("12. Расходы на содержание Воскресной школы", "EXP_SUNSCHL", _model.ExpenseByCode, false);
                    AddDataRow("13. Расходы на питание", "EXP_FOOD", _model.ExpenseByCode, false);
                    AddDataRow("14. Приобретение церковной утвари, оборудования для прихода", "EXP_UTEN", _model.ExpenseByCode, false);
                    AddDataRow("15. Расходы на командировки, сессии, паломнические поездки", "EXP_BISTRIP", _model.ExpenseByCode, false);
                    AddDataRow("16. Расходы на оплату технической документации, проектной документации и др.", "EXP_TECHDOC", _model.ExpenseByCode, false);
                    AddDataRow("17. Прочие расходы", "EXP_OTHER", _model.ExpenseByCode, false);
                    
                    // --- ИТОГИ ---
                    decimal totalExpense = 0; foreach (var v in _model.ExpenseByCode.Values) totalExpense += v;
                    table.Cell().Border(1).Text("");
                    table.Cell().Border(1).Text(" Итого расходы за период (по стр. 3)").Bold();
                    table.Cell().Border(1).AlignRight().Text(totalExpense.ToString("F2") + " ").Bold();
                    table.Cell().Border(1).Text("");
                    table.Cell().Border(1).Text("");
                    table.Cell().Border(1).Text(" Остаток средств на конец периода").Bold();
                    table.Cell().Border(1).AlignRight().Text(_model.ClosingBalance.ToString("F2") + " ").Bold();
                    table.Cell().Border(1).Text("");
                });
                    
                    // === 3. ПОДПИСИ СТОРОН ===
                    column.Item().PaddingTop(25).Column(c =>
                    {
                        void AddSignatureLine(string title, string name)
                        {
                            c.Item().PaddingBottom(5).Row(r =>
                            {
                                r.RelativeItem().Text(title);
                                r.ConstantItem(120).BorderBottom(1).Text("");
                                r.ConstantItem(160).PaddingLeft(10).Text($"/ {name} /");
                            });
                        }
                        string rector = _model.Signatures.ContainsKey("Настоятель храма") ? _model.Signatures["Настоятель храма"] : "";
                        string chairman = _model.Signatures.ContainsKey("Председатель приходского совета") ? _model.Signatures["Председатель приходского совета"] : "";
                        string treasurer = _model.Signatures.ContainsKey("Казначей") ? _model.Signatures["Казначей"] : "";
                        string auditor = _model.Signatures.ContainsKey("Член ревизионной комиссии") ? _model.Signatures["Член ревизионной комиссии"] : "";
                        AddSignatureLine("Настоятель храма", rector);
                        AddSignatureLine("Председатель приходского совета", chairman);
                        AddSignatureLine("Казначей (гл. бухгалтер, бухгалтер)", treasurer);
                        AddSignatureLine("Члены ревизионной комиссии", auditor);
                        c.Item().PaddingTop(10).Text("М.П.").FontSize(9).Italic();
                    });
                });
            });
        }
    }
}
