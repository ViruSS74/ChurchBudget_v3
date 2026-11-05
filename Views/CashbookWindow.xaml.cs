using ChurchBudget_v3_WPF.Reports;
using ChurchBudget_v3_WPF.Services;
using QuestPDF.Companion;
using QuestPDF.Fluent;
using System;
using System.Collections.Generic;
using System.Data;
using System.Windows;

namespace ChurchBudget_v3_WPF.Views
{
    public partial class CashBookWindow : Window
    {
        private readonly CashbookService _service;
        private readonly ListOfDocsService _docsService;
        private readonly string _connString;

        public CashBookWindow()
        {
            InitializeComponent();

            // Путь к базе данных SQLite
            string dbPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "church.db");
            _connString = $"Data Source={dbPath};Version=3;";

            _service = new CashbookService(_connString);
            _docsService = new ListOfDocsService(_connString);

            // Активируем лицензию QuestPDF
            QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

            // Настраиваем списки периода
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

        // 1. НАЖАТИЕ: ПЕЧАТЬ ТИТУЛЬНОГО ЛИСТА
        private void BtnPrintTitle_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                int year = Convert.ToInt32(cmbYear.SelectedItem);
                DataRow org = _service.GetOrganizationInfo();
                string orgName = org?["name"]?.ToString() ?? "Религиозная община";

                var report = new CashBookReport("Title", year, orgName, null);

                // Исправлено: сохраняем во временный файл и открываем его
                string filePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TitlePage.pdf");
                report.GeneratePdf(filePath);
                System.Diagnostics.Process.Start(filePath);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка генерации титульного листа: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // 2. НАЖАТИЕ: ПАКЕТНАЯ ПЕЧАТЬ ЗА МЕСЯЦ (ВКЛАДНЫЕ ЛИСТЫ)
        private void BtnPrintMonthPages_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                int year = Convert.ToInt32(cmbYear.SelectedItem);
                int month = cmbMonth.SelectedIndex + 1;

                DataTable dtYearOps = _service.GetOperationsForYear(year);
                var activeDates = new List<DateTime>();
                foreach (DataRow row in dtYearOps.Rows)
                {
                    if (DateTime.TryParse(row["date"].ToString(), out DateTime opDate) && !activeDates.Contains(opDate.Date))
                        activeDates.Add(opDate.Date);
                }
                activeDates.Sort();

                var reportPages = new List<CashBookModel>();
                int lastPageNumPrevMonth = 0;

                for (int i = 0; i < activeDates.Count; i++)
                {
                    DateTime currentDay = activeDates[i];
                    int realPageNumber = i + 1;

                    if (currentDay.Month < month)
                        lastPageNumPrevMonth = realPageNumber;

                    if (currentDay.Month == month)
                    {
                        DataTable dayOps = _service.GetOperationsForDay(year, currentDay.Month, currentDay.Day);
                        int opsCountBeforeThisDay = _service.GetOperationsCountBeforeDay(year, currentDay.Month, currentDay.Day);
                        int startYearlySerialNumber = opsCountBeforeThisDay + 1;

                        decimal openingBalance = (currentDay.Month == 1 && currentDay.Day == 1)
                            ? _service.GetOpeningBalanceForYear(year)
                            : _service.CalculateClosingBalance(currentDay.AddDays(-1).Year, currentDay.AddDays(-1).Month, currentDay.AddDays(-1).Day);

                        decimal income = 0, expense = 0;
                        foreach (DataRow r in dayOps.Rows)
                        {
                            decimal amt = Convert.ToDecimal(r["amount"]);
                            if (r["order_type"].ToString() == "ПКО") income += amt; else expense += amt;
                        }

                        bool isFirstInMonth = (i == 0) || (activeDates[i - 1].Month != month);
                        bool isLastInMonth = (i == activeDates.Count - 1) || (activeDates[i + 1].Month != month);
                        bool isLastInYear = (i == activeDates.Count - 1);

                        reportPages.Add(new CashBookModel
                        {
                            Date = currentDay,
                            PageNumber = startYearlySerialNumber,
                            LastPageNumPrevMonth = lastPageNumPrevMonth,
                            IsFirstInMonth = isFirstInMonth,
                            IsLastInMonth = isLastInMonth,
                            IsLastInYear = isLastInYear,
                            OpeningBalance = openingBalance,
                            ClosingBalance = openingBalance + income - expense,
                            TotalIncome = income,
                            TotalExpense = expense,
                            Operations = dayOps
                        });
                    }
                }

                if (reportPages.Count == 0)
                {
                    MessageBox.Show("В выбранном месяце не было кассовых операций.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var signatures = _docsService.GetStaffSignatures();
                var report = new CashBookReport(year, reportPages, signatures);

                // Исправлено: сохраняем пакет листов за месяц и открываем его
                string filePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"CashBook_Month_{month}.pdf");
                report.GeneratePdf(filePath);
                System.Diagnostics.Process.Start(filePath);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка пакетной генерации кассовых листов: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // 3. НАЖАТИЕ: ПЕЧАТЬ ЗАВЕРИТЕЛЬНОГО ШТАМПА
        private void BtnPrintStamp_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                int year = Convert.ToInt32(cmbYear.SelectedItem);
                var signatures = _docsService.GetStaffSignatures();

                DataTable dtYearOps = _service.GetOperationsForYear(year);
                var activeDates = new List<DateTime>();
                foreach (DataRow row in dtYearOps.Rows)
                {
                    if (DateTime.TryParse(row["date"].ToString(), out DateTime opDate) && !activeDates.Contains(opDate.Date))
                        activeDates.Add(opDate.Date);
                }

                var report = new CashBookReport("Stamp", year, activeDates.Count.ToString(), signatures);

                // Исправлено: сохраняем штамп и открываем его
                string filePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "LastPageStamp.pdf");
                report.GeneratePdf(filePath);
                System.Diagnostics.Process.Start(filePath);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка генерации заверительного штампа: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Служебные методы управления окном WPF без стандартных рамок
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