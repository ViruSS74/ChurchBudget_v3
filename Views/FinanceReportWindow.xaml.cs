using ChurchBudget;
using ChurchBudget_v3_WPF.Reports;
using ChurchBudget_v3_WPF.Services;
using Microsoft.Win32;
using QuestPDF.Fluent;
using System;
using System.Collections.Generic;
using System.Data;
using System.Windows;

namespace ChurchBudget_v3_WPF.Views
{
    public partial class FinanceReportWindow : Window
    {
        private readonly ChurchBudget.FinancialReport _service;
        private readonly ListOfDocsService _docsService;
        private readonly string _connString;

        public FinanceReportWindow()
        {
            InitializeComponent();
            string dbPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "church.db");
            _connString = $"Data Source={dbPath};Version=3;";

            // вызываем правильное имя класса
            _service = new ChurchBudget.FinancialReport(_connString);
            _docsService = new ListOfDocsService(_connString);

            QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
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
        }

        // 1. НАЖАТИЕ: ПЕЧАТЬ ГОДОВОГО ОТЧЕТА
        private void BtnPrintYearReport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                int year = Convert.ToInt32(cmbYear.SelectedItem);
                int orgId = 1;

                DateTime startPeriod = new DateTime(year, 1, 1);
                DateTime endPeriod = new DateTime(year, 12, 31);

                var org = _service.GetOrganizationInfo(orgId);
                var sigs = _service.GetSignatures(orgId);

                var incomes = _service.GetIncomeByReportCode(startPeriod, endPeriod);
                var expenses = _service.GetExpenseByReportCode(startPeriod, endPeriod);

                decimal openBal = _service.GetOpeningBalanceForPeriod(orgId, startPeriod);
                decimal totalInc = 0; foreach (var v in incomes.Values) totalInc += v;
                decimal totalExp = 0; foreach (var v in expenses.Values) totalExp += v;
                decimal closeBal = openBal + totalInc - totalExp;

                var model = new BudgetReportModel
                {
                    PeriodText = $"{year} год",
                    OrganizationName = org.ContainsKey("Name") ? org["Name"] : "Приход храма",
                    Location = org.ContainsKey("Location") ? org["Location"] : "",
                    Deanery = org.ContainsKey("Blagochinie") ? org["Blagochinie"] : "",
                    Diocese = org.ContainsKey("Diocese") ? org["Diocese"] : "Борисовская епархия",
                    OpeningBalance = openBal,
                    ClosingBalance = closeBal,
                    IncomeByCode = incomes,
                    ExpenseByCode = expenses,
                    Signatures = sigs
                };

                // 1. Инициализируем наш отчет, передавая модель и строку подключения
                FinancialBudgetReport document = new FinancialBudgetReport(model, _connString);

                // 2. Вычисляем четкий путь к файлу в корневой папке приложения
                string filePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"FinancialReport_Year_{year}.pdf");

                // 3. Исправлено: Генерируем PDF напрямую по вычисленному пути filePath (несуществующий saveFileDialog удален)
                document.GeneratePdf(filePath);

                // 4. Открываем готовый PDF-файл для просмотра или печати
                System.Diagnostics.Process.Start(filePath);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка генерации годового отчета: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // 2. НАЖАТИЕ: ПЕЧАТЬ ОТЧЕТА ЗА ВЫБРАННЫЙ МЕСЯЦ
        private void BtnPrintMonthReport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                int year = Convert.ToInt32(cmbYear.SelectedItem);
                int month = cmbMonth.SelectedIndex + 1;
                int orgId = 1;

                DateTime startPeriod = new DateTime(year, month, 1);
                DateTime endPeriod = startPeriod.AddMonths(1).AddDays(-1);

                string[] monthGenitive = { "", "январь", "февраль", "март", "апрель", "май", "июнь", "июль", "август", "сентябрь", "октябрь", "ноябрь", "декабрь" };

                var org = _service.GetOrganizationInfo(orgId);
                var sigs = _service.GetSignatures(orgId);

                var incomes = _service.GetIncomeByReportCode(startPeriod, endPeriod);
                var expenses = _service.GetExpenseByReportCode(startPeriod, endPeriod);

                decimal openBal = _service.GetOpeningBalanceForPeriod(orgId, startPeriod);
                decimal totalInc = 0; foreach (var v in incomes.Values) totalInc += v;
                decimal totalExp = 0; foreach (var v in expenses.Values) totalExp += v;
                decimal closeBal = openBal + totalInc - totalExp;

                var model = new BudgetReportModel
                {
                    PeriodText = $"{monthGenitive[month]} {year} г.",
                    OrganizationName = org.ContainsKey("Name") ? org["Name"] : "Приход храма",
                    Location = org.ContainsKey("Location") ? org["Location"] : "",
                    Deanery = org.ContainsKey("Blagochinie") ? org["Blagochinie"] : "",
                    Diocese = org.ContainsKey("Diocese") ? org["Diocese"] : "Борисовская епархия",
                    OpeningBalance = openBal,
                    ClosingBalance = closeBal,
                    IncomeByCode = incomes,
                    ExpenseByCode = expenses,
                    Signatures = sigs
                };

                // 1. Инициализируем наш отчет, передавая модель и строку подключения
                FinancialBudgetReport document = new FinancialBudgetReport(model, _connString);

                // 2. Вычисляем четкий путь к файлу
                string filePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"FinancialReport_Month_{month}_{year}.pdf");

                // 3. Исправлено: Генерируем PDF напрямую по вычисленному пути filePath
                document.GeneratePdf(filePath);

                // 4. Открываем готовый PDF-файл для просмотра или печати
                System.Diagnostics.Process.Start(filePath);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка генерации отчета за месяц: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
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