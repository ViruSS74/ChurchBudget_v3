using ChurchBudget_v3_WPF.Reports;
using ChurchBudget_v3_WPF.Services;
using ChurchBudget;
using QuestPDF.Fluent;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Windows;

namespace ChurchBudget_v3_WPF.Views
{
    public partial class BatchPrintWindow : Window
    {
        private readonly string _connString;
        private readonly ListOfDocsService _listOfDocsService; // Используем ваш родной сервис списка документов

        public BatchPrintWindow()
        {
            InitializeComponent();
            string dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "church.db");
            _connString = $"Data Source={dbPath};Version=3;";
            QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

            // Инициализируем ваш родной сервис
            _listOfDocsService = new ListOfDocsService(_connString);
            InitPeriodControls();
        }

        private void InitPeriodControls()
        {
            int currentYear = DateTime.Now.Year;
            for (int year = currentYear - 2; year <= currentYear + 3; year++)
                cmbYear.Items.Add(year);
            cmbYear.SelectedItem = currentYear;

            string[] monthNames = { "Январь", "Февраль", "Март", "Апрель", "Май", "Июнь", "Июль", "Август", "Сентябрь", "Октябрь", "Ноябрь", "Декабрь" };
            foreach (var month in monthNames)
                cmbMonth.Items.Add(month);
            cmbMonth.SelectedIndex = DateTime.Now.Month - 1;

            string[] docNames = { "Рапортичка", "Расходы", "Приходный ордер (ПКО)", "Расходный ордер (РКО)" };
            foreach (var doc in docNames)
                cmbDoc.Items.Add(doc);
            cmbDoc.SelectedIndex = 0;
        }

        private void PrintButton_Click(object sender, RoutedEventArgs e)
        {
            if (cmbYear.SelectedItem == null || cmbMonth.SelectedItem == null || cmbDoc.SelectedItem == null)
            {
                MessageBox.Show("Пожалуйста, выберите параметры выгрузки.", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int year = Convert.ToInt32(cmbYear.SelectedItem);
            int month = cmbMonth.SelectedIndex + 1;
            string selectedDocType = cmbDoc.SelectedItem.ToString();
            int orgId = 1;

            Microsoft.Win32.SaveFileDialog sfd = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "PDF Documents (*.pdf)|*.pdf",
                Title = "Сохранить пакет документов за месяц",
                FileName = $"{selectedDocType.Replace(" ", "_")}_{cmbMonth.SelectedItem}_{year}.pdf"
            };

            if (sfd.ShowDialog() == true)
            {
                try
                {
                    var financialService = new ChurchBudget.FinancialReport(_connString);
                    var org = financialService.GetOrganizationInfo(orgId);
                    var sigs = financialService.GetSignatures(orgId);

                    string dbOrgName = org.ContainsKey("Name") ? org["Name"] : "Приход храма";
                    string dbOrgLocation = org.ContainsKey("Location") ? org["Location"] : "";
                    string rector = sigs.ContainsKey("Настоятель храма") ? sigs["Настоятель храма"] : "Не указан";
                    string currentTreasurer = sigs.ContainsKey("Казначей") ? sigs["Казначей"] : "Не указан";

                    string monthStr = month.ToString("D2");

                    // =========================================================================
                    // === 1. ПАКЕТНАЯ ПЕЧАТЬ РАПОТОРТИЧЕК (ВЫТЯГИВАЕМ СТРОГО ИЗ income_docs) ===
                    // =========================================================================
                    if (selectedDocType == "Рапортичка")
                    {
                        var raportichkaModels = new List<RaportichkaDataModel>();

                        using (var conn = new SQLiteConnection(_connString))
                        {
                            conn.Open();
                            // Ищем все документы доходов за выбранный месяц года
                            string sql = "SELECT id, doc_number, date FROM income_docs WHERE STRFTIME('%Y', date) = @year AND STRFTIME('%m', date) = @month ORDER BY date ASC, doc_number ASC";
                            using (var cmd = new SQLiteCommand(sql, conn))
                            {
                                cmd.Parameters.AddWithValue("@year", year.ToString());
                                cmd.Parameters.AddWithValue("@month", monthStr);
                                using (var reader = cmd.ExecuteReader())
                                {
                                    while (reader.Read())
                                    {
                                        int docId = Convert.ToInt32(reader["id"]);
                                        string docNum = reader["doc_number"].ToString();
                                        DateTime docDate = Convert.ToDateTime(reader["date"]);

                                        // Подгружаем реальную таблицу позиций через ваш родной сервис
                                        DataTable itemsTable = _listOfDocsService.GetFullRaportichka("Income", docId);

                                        raportichkaModels.Add(new RaportichkaDataModel
                                        {
                                            OrganizationName = dbOrgName,
                                            LocationText = dbOrgLocation,
                                            DocNumber = docNum,
                                            DocDate = docDate,
                                            ItemsTable = itemsTable,
                                            TreasurerName = currentTreasurer
                                        });
                                    }
                                }
                            }
                        }

                        if (raportichkaModels.Count == 0)
                        {
                            MessageBox.Show("Рапортички за выбранный период не найдены.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                            return;
                        }

                        var doc = new RaportichkaPacketReport(raportichkaModels);
                        doc.GeneratePdf(sfd.FileName);
                    }
                    // =========================================================================
                    // === 2. ПАКЕТНАЯ ПЕЧАТЬ РАСХОДОВ (ВЫТЯГИВАЕМ СТРОГО ИЗ expense_docs) ===
                    // =========================================================================
                    else if (selectedDocType == "Расходы")
                    {
                        var expenseModels = new List<ExpenseDataModel>();

                        using (var conn = new SQLiteConnection(_connString))
                        {
                            conn.Open();
                            // Ищем все документы расходов за выбранный месяц года
                            string sql = "SELECT id, doc_number, date FROM expense_docs WHERE STRFTIME('%Y', date) = @year AND STRFTIME('%m', date) = @month ORDER BY date ASC, doc_number ASC";
                            using (var cmd = new SQLiteCommand(sql, conn))
                            {
                                cmd.Parameters.AddWithValue("@year", year.ToString());
                                cmd.Parameters.AddWithValue("@month", monthStr);
                                using (var reader = cmd.ExecuteReader())
                                {
                                    while (reader.Read())
                                    {
                                        int docId = Convert.ToInt32(reader["id"]);
                                        string docNum = reader["doc_number"].ToString();
                                        DateTime docDate = Convert.ToDateTime(reader["date"]);

                                        // Подгружаем реальную таблицу позиций через ваш родной сервис
                                        DataTable itemsTable = _listOfDocsService.GetFullRaportichka("Expense", docId);

                                        expenseModels.Add(new ExpenseDataModel
                                        {
                                            OrganizationName = dbOrgName,
                                            LocationText = dbOrgLocation,
                                            DocNumber = docNum,
                                            DocDate = docDate,
                                            ItemsTable = itemsTable,
                                            TreasurerName = currentTreasurer,
                                            HasChecks = false
                                        });
                                    }
                                }
                            }
                        }

                        if (expenseModels.Count == 0)
                        {
                            MessageBox.Show("Документы расходов за выбранный период не найдены.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                            return;
                        }

                        var doc = new ExpensePacketReport(expenseModels);
                        doc.GeneratePdf(sfd.FileName);
                    }
                    // === 3 и 4. ПАКЕТ ОРДЕРОВ ПКО ИЛИ РКО (ПОСТРАНИЧНО) ===
                    else
                    {
                        string orderFilter = selectedDocType.Contains("ПКО") ? "ПКО" : "РКО";
                        List<CashOrderInDataModel> orderList = new List<CashOrderInDataModel>();

                        using (var conn = new SQLiteConnection(_connString)) 
                        {
                            conn.Open(); 
                            string sql = @"SELECT * FROM cash_orders WHERE order_type = @type AND STRFTIME ('%Y', date) = @year AND STRFTIME ('%m', date) = @month ORDER BY date (date) ASC, order_number ASC";
                            using (var cmd = new SQLiteCommand(sql, conn)) 
                            { 
                                cmd.Parameters.AddWithValue("@type", orderFilter);
                                cmd.Parameters.AddWithValue("@year", year.ToString());
                                cmd.Parameters.AddWithValue("@month", monthStr);
                                using (var reader = cmd.ExecuteReader()) 
                                {
                                    while (reader.Read()) 
                                    {
                                        DateTime docDate = Convert.ToDateTime(reader["date"]);
                                        string person = reader ["person_name_manual"] != DBNull.Value ? reader ["person_name_manual"].ToString() : "";
                                        string passport = null;
                                        if (orderFilter == "РКО") 
                                        {
                                            passport = "Паспорт не указан в БД";
                                            string sqlPass = @"SELECT td.name as doc_type, series, number, issued_by, issue_date FROM id_documents idd LEFT JOIN type_id_document td ON idd.type_id_doc = td.id WHERE employee_id = 2 LIMIT 1";
                                            using (var cmdP = new SQLiteCommand(sqlPass, conn)) 
                                            using (var rdrP = cmdP.ExecuteReader()) 
                                            {
                                                if (rdrP.Read()) 
                                                {
                                                    string pDate = rdrP ["issue_date"] != DBNull.Value ? Convert.ToDateTime (rdrP["issue_date"]).ToString("dd.MM.yyyy") : "";
                                                    passport = $"{rdrP["doc_type"]} {rdrP["series"]} {rdrP["number"]}, выдан {rdrP["issued_by"]} {pDate} г.";
                                                }
                                            }
                                        }
                                        orderList.Add(new CashOrderInDataModel 
                                        { 
                                            OrganizationName = dbOrgName, OrderNumber = reader["order_number"].ToString().Replace("ПКО-", "").Replace("РКО-", ""), OrderDate = docDate, Amount = Convert.ToDecimal(reader["amount"]), FromPersonGenitive = string.IsNullOrEmpty(person) ? currentTreasurer : person, Basis = reader["base"] != DBNull.Value ? reader["base"].ToString() : "Хозяйственные нужды", Appendix = reader["appendix"] != DBNull.Value ? reader["appendix"].ToString() : "—", RectorName = rector, TreasurerName = currentTreasurer, PassportInfo = passport });
                                    }
                                }
                            }
                        }
                        if (orderList.Count == 0) 
                        { 
                            MessageBox.Show($"Кассовые документы {orderFilter} за выбранный период не найдены.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information); return; 
                        }
                        var doc = new CashOrdersPacketReport(orderList);
                        doc.GeneratePdf(sfd.FileName);
                    }
                    
                    // Открываем созданный общий PDF-пакет за месяц
                    
                    System.Diagnostics.Process.Start(sfd.FileName);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка пакетного формирования: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        
        private void TitleBar_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ChangedButton == System.Windows.Input.MouseButton.Left)
                this.DragMove();
        }
        
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}