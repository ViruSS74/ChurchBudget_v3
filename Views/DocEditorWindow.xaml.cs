using ChurchBudget_v3_WPF.Reports;
using ChurchBudget_v3_WPF.Services;
using QuestPDF.Fluent;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Windows;

namespace ChurchBudget_v3_WPF.Views
{
    public partial class DocEditorWindow : Window
    {
        private readonly string dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data\\church.db");
        private readonly ListOfDocsService _service;
        private readonly int _docId;
        private readonly string _docType;
        private readonly string _docNumber;
        private readonly DateTime _docDate;

        private DataRow _orderRow;

        private bool _isLoading = false;

        public DocEditorWindow(int docId, string docType, string docNumber, DateTime docDate, ListOfDocsService service)
        {
            InitializeComponent();
            _docId = docId;
            _docType = docType;
            _docNumber = docNumber;
            _docDate = docDate;
            _service = service;

            LoadDocumentData();
        }

        private void LoadDocumentData()
        {
            try
            {
                _isLoading = true; // Включаем предохранитель: форма временно игнорирует ввод

                // 0. Загружаем сотрудников в ComboBox
                using (var conn = new System.Data.SQLite.SQLiteConnection($"Data Source={dbPath};Version=3;"))
                {
                    conn.Open();
                    var cmd = new System.Data.SQLite.SQLiteCommand("SELECT (last_name || ' ' || first_name || ' ' || middle_name) as fullName FROM personal", conn);
                    using (var reader = cmd.ExecuteReader())
                    {
                        txtPerson.Items.Clear();
                        while (reader.Read())
                        {
                            txtPerson.Items.Add(reader["fullName"].ToString());
                        }
                    }
                }

                // 1. Управляем видимостью бланка и таблицы
                if (_docType == "ПКО" || _docType == "РКО")
                {
                    dgDocItems.Visibility = Visibility.Collapsed;
                    svOrderBlank.Visibility = Visibility.Visible;
                }
                else
                {
                    dgDocItems.Visibility = Visibility.Visible;
                    svOrderBlank.Visibility = Visibility.Collapsed;
                }

                // 2. Загружаем текстовые поля и таблицу позиций
                _orderRow = _service.GetCashOrderRow(_docId);

                string serviceType = (_docType == "Доходы" || _docType == "ПКО") ? "Income" : "Expense";
                DataTable itemsTable = _service.GetFullRaportichka(serviceType, _docId);

                // Считаем суммы и категории, полностью очищая их от скрытых пробелов в БД
                decimal total = 0;
                List<string> activeCategories = new List<string>();

                foreach (DataRow row in itemsTable.Rows)
                {
                    decimal amt = row["amount"] != DBNull.Value ? Convert.ToDecimal(row["amount"]) : 0;
                    total += amt;

                    if (amt > 0 && row["display_name"] != DBNull.Value)
                    {
                        // Метод Trim() полностью удаляет все лишние пробелы в начале и конце строки
                        string cleanCategory = row["display_name"].ToString().Trim();

                        if (!string.IsNullOrEmpty(cleanCategory))
                        {
                            activeCategories.Add(cleanCategory);
                        }
                    }
                }

                // Если это ПКО/РКО и в таблице пусто, пробуем взять сумму напрямую из ордера
                if ((_docType == "ПКО" || _docType == "РКО") && total == 0 && _orderRow != null && _orderRow.Table.Columns.Contains("amount") && _orderRow["amount"] != DBNull.Value)
                {
                    total = Convert.ToDecimal(_orderRow["amount"]);
                }

                lblTotal.Text = $"Итого: {total:F2} руб.";

                // Авто-генерируемое основание (теперь категории склеятся идеально: "топливо, электроэнергия, взносы")
                string autoBasis = "";
                if (_docType == "Доходы" || _docType == "ПКО")
                {
                    autoBasis = $"Приход по документу №{_docNumber}";
                }
                else if (_docType == "Расходы" || _docType == "РКО")
                {
                    autoBasis = activeCategories.Count > 0
                        ? string.Join(", ", activeCategories)
                        : "Хозяйственные расходы";
                }

                // Заполняем поля ввода
                if (_orderRow != null)
                {
                    txtPerson.Text = _orderRow["person_name_manual"] != DBNull.Value && !string.IsNullOrEmpty(_orderRow["person_name_manual"].ToString())
                        ? _orderRow["person_name_manual"].ToString()
                        : (_orderRow["person"] != DBNull.Value ? _orderRow["person"].ToString() : "");

                    // 1. ЕСЛИ ЭТО РКО — ПРИНУДИТЕЛЬНО ПИШЕМ НАСТОЯЩИЕ СТАТЬИ РАСХОДОВ С СУММОЙ > 0
                    if (_docType == "РКО")
                    {
                        // Проверяем: если в сохраненном ордере уже есть заполненное основание, берем его!
                        if (_orderRow.Table.Columns.Contains("base") && _orderRow["base"] != DBNull.Value && !string.IsNullOrEmpty(_orderRow["base"].ToString().Trim()))
                        {
                            txtBasis.Text = _orderRow["base"].ToString().Trim();
                        }
                        else
                        {
                            // Если в базе пусто, тогда используем автогенерацию (или дефолт)
                            txtBasis.Text = !string.IsNullOrEmpty(autoBasis) ? autoBasis : "Хозяйственные расходы";
                        }
                    }
                    else // 2. ДЛЯ ВСЕХ ОСТАЛЬНЫХ ДОКУМЕНТОВ (Доходы, Расходы, ПКО)
                    {
                        if (_orderRow.Table.Columns.Contains("base"))
                        {
                            bool isDbNullOrEmpty = _orderRow["base"] == DBNull.Value || string.IsNullOrEmpty(_orderRow["base"].ToString().Trim());

                            if (isDbNullOrEmpty)
                            {
                                txtBasis.Text = ""; // Если стерли руками, оставляем пустым
                            }
                            else
                            {
                                txtBasis.Text = _orderRow["base"].ToString().Trim();
                            }
                        }
                        else
                        {
                            txtBasis.Text = autoBasis;
                        }
                    }

                    txtAppendix.Text = _orderRow["appendix"] != DBNull.Value ? _orderRow["appendix"].ToString() : "";
                }
                else
                {
                    // Сюда программа зайдет, если записи ордера физически нет в таблице cash_orders
                    var printSignatures = _service.GetStaffSignatures();
                    txtPerson.Text = printSignatures != null ? printSignatures.TreasurerName : "";
                    txtBasis.Text = autoBasis; // Сразу генерируем правильное основание (для РКО — категории с суммами > 0)
                    txtAppendix.Text = "";
                }

                // НАПРАВЛЯЕМ ДАННЫЕ НА ПРАВЫЙ БЛАНК (Убираем прочерки)
                lblBlankPerson.Text = string.IsNullOrEmpty(txtPerson.Text) ? "—" : txtPerson.Text;
                lblBlankBasis.Text = string.IsNullOrEmpty(txtBasis.Text) ? "—" : txtBasis.Text;
                lblBlankAppendix.Text = string.IsNullOrEmpty(txtAppendix.Text) ? "Не указано" : txtAppendix.Text;
                lblBlankAmount.Text = $"{total:F2} руб.";

                // Загружаем таблицу, если это ведомость
                if (_docType == "Доходы" || _docType == "Расходы")
                {
                    dgDocItems.ItemsSource = itemsTable.DefaultView;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки данных документа: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                _isLoading = false; // Отключаем предохранитель, теперь пользователь может вводить данные руками
            }
        }

        // Метод для перетаскивания окна мышкой
        private void TitleBar_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ChangedButton == System.Windows.Input.MouseButton.Left)
            {
                this.DragMove();
            }
        }

        // Кнопка: Сохранить изменения
        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 1. Извлекаем правильный ID кассового ордера
                int actualOrderId = 0;
                if (_orderRow != null && _orderRow.Table.Columns.Contains("id") && _orderRow["id"] != DBNull.Value)
                {
                    actualOrderId = Convert.ToInt32(_orderRow["id"]);
                }

                string fullTextName = txtPerson.Text.Trim(); // Получаем ФИО текстом
                object selectedEmpId = DBNull.Value;

                using (var conn = new System.Data.SQLite.SQLiteConnection($"Data Source={dbPath};Version=3;"))
                {
                    conn.Open();

                    // БРОНЕБОЙНЫЙ ПОИСК ID: Чистим скрытые пробелы функцией TRIM и используем мягкий LIKE
                    if (!string.IsNullOrEmpty(fullTextName))
                    {
                        try
                        {
                            string sqlFindId = @"
                SELECT id FROM personal 
                WHERE TRIM(last_name || ' ' || first_name || ' ' || middle_name) LIKE @fullName
                LIMIT 1;";

                            using (var cmdFind = new SQLiteCommand(sqlFindId, conn))
                            {
                                // Знак % застрахует поиск, даже если в базе у отчества стоят лишние пробелы!
                                cmdFind.Parameters.AddWithValue("@fullName", fullTextName + "%");
                                var resId = cmdFind.ExecuteScalar();
                                if (resId != null)
                                {
                                    selectedEmpId = Convert.ToInt32(resId);
                                }
                            }
                        }
                        catch { /* Справочник не нашел совпадений */ }
                    }

                    string sql = "";

                    // 2. Если запись ордера существует в БД — делаем UPDATE
                    if (actualOrderId > 0)
                    {
                        // Принудительно сохраняем person_id, который мы только что нашли по тексту!
                        sql = @"
            UPDATE cash_orders 
            SET person_name_manual = @person, 
                base = @basis, 
                appendix = @appendix,
                person_id = @personId
            WHERE id = @orderId;";

                        using (var cmd = new System.Data.SQLite.SQLiteCommand(sql, conn))
                        {
                            cmd.Parameters.AddWithValue("@person", fullTextName);
                            cmd.Parameters.AddWithValue("@basis", txtBasis.Text.Trim());
                            cmd.Parameters.AddWithValue("@appendix", txtAppendix.Text.Trim());
                            cmd.Parameters.AddWithValue("@personId", selectedEmpId); // Числовой ID уходит в базу!
                            cmd.Parameters.AddWithValue("@orderId", actualOrderId);

                            cmd.ExecuteNonQuery();
                        }
                    }
                    else
                    {
                        // 3. СТРАХОВКА: Если записи в cash_orders ещё не было, создаем её (INSERT)
                        sql = @"
            INSERT INTO cash_orders (document_id, type, number, date, person, base, appendix, person_name_manual, person_id) 
            VALUES (@docId, @type, @num, @date, @person, @basis, @appendix, @person, @personId);";

                        using (var cmd = new System.Data.SQLite.SQLiteCommand(sql, conn))
                        {
                            cmd.Parameters.AddWithValue("@docId", _docId);
                            cmd.Parameters.AddWithValue("@type", _docType);
                            cmd.Parameters.AddWithValue("@num", _docNumber);
                            cmd.Parameters.AddWithValue("@date", _docDate.ToString("yyyy-MM-dd HH:mm:ss"));
                            cmd.Parameters.AddWithValue("@person", fullTextName);
                            cmd.Parameters.AddWithValue("@basis", txtBasis.Text.Trim());
                            cmd.Parameters.AddWithValue("@appendix", txtAppendix.Text.Trim());
                            cmd.Parameters.AddWithValue("@personId", selectedEmpId);

                            cmd.ExecuteNonQuery();
                        }
                    }
                }

                // Обновляем данные на форме, чтобы зафиксировать изменения в памяти приложения
                _orderRow = _service.GetCashOrderRow(_docId);

                // Направляем обновленный текст на правый бланк просмотра
                if (lblBlankPerson != null) lblBlankPerson.Text = fullTextName;
                if (lblBlankBasis != null) lblBlankBasis.Text = txtBasis.Text;
                if (lblBlankAppendix != null) lblBlankAppendix.Text = string.IsNullOrEmpty(txtAppendix.Text) ? "Не указано" : txtAppendix.Text;

                MessageBox.Show("Изменения успешно сохранены в базу данных.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка сохранения: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Кнопка: Печать бланка (Вызывает QuestPDF)
        private void btnPrint_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 1. Создаем папку Temp для временных файлов
                string tempDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Temp");
                if (!Directory.Exists(tempDir)) Directory.CreateDirectory(tempDir);

                string fileName = $"Print_{_docType}_{_docNumber}_{DateTime.Now.Ticks}.pdf"
                    .Replace("/", "_").Replace("\\", "_").Replace(":", "_");
                string tempFilePath = Path.Combine(tempDir, fileName);

                // ЖЕЛЕЗНО ПОЛУЧАЕМ ПОДПИСИ ИЗ СЕРВИСА
                var printSignatures = _service.GetStaffSignatures();
                string currentTreasurer = printSignatures != null ? printSignatures.TreasurerName : "Не указан";

                // ДИНАМИЧЕСКИ ЗАГРУЖАЕМ ДАННЫЕ ОРГАНИЗАЦИИ ИЗ БД
                string dbOrgName = "Приход храма";
                string dbOrgLocation = "";
                using (var conn = new System.Data.SQLite.SQLiteConnection($"Data Source={dbPath};Version=3;"))
                {
                    conn.Open();
                    var cmd = new System.Data.SQLite.SQLiteCommand("SELECT name, location FROM organizations LIMIT 1", conn);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            dbOrgName = reader["name"] != DBNull.Value ? reader["name"].ToString() : "Приход храма";
                            dbOrgLocation = reader["location"] != DBNull.Value ? reader["location"].ToString() : "";
                        }
                    }
                }

                // 2. Вызываем ваши реальные генераторы отчетов
                if (_docType == "Доходы")
                {
                    if (dgDocItems.ItemsSource is System.Data.DataView dv)
                    {
                        var model = new RaportichkaDataModel
                        {
                            OrganizationName = dbOrgName,
                            LocationText = dbOrgLocation,
                            DocNumber = _docNumber,
                            DocDate = _docDate,
                            ItemsTable = dv.Table,
                            TreasurerName = currentTreasurer
                        };
                        var report = new RaportichkaReport(model);
                        report.GeneratePdf(tempFilePath);
                    }
                }
                else if (_docType == "Расходы")
                {
                    if (dgDocItems.ItemsSource is System.Data.DataView dv)
                    {
                        var model = new ExpenseDataModel
                        {
                            OrganizationName = dbOrgName,
                            LocationText = dbOrgLocation,
                            DocNumber = _docNumber,
                            DocDate = _docDate,
                            ItemsTable = dv.Table,
                            TreasurerName = currentTreasurer,
                            HasChecks = false
                        };
                        var report = new ExpenseReportA4(model);
                        report.GeneratePdf(tempFilePath);
                    }
                }
                else if (_docType == "ПКО" || _docType == "РКО")
                {
                    DateTime orderDate = _docDate;
                    if (_orderRow != null && _orderRow.Table.Columns.Contains("date") && _orderRow["date"] != DBNull.Value)
                    {
                        DateTime.TryParse(_orderRow["date"].ToString(), out orderDate);
                    }

                    decimal orderAmount = 0;
                    DataTable itemsTableForRko = null;

                    // Пытаемся взять таблицу из Grid, если она там вдруг есть
                    if (dgDocItems.ItemsSource is System.Data.DataView dvText)
                    {
                        itemsTableForRko = dvText.Table;
                        foreach (System.Data.DataRow r in itemsTableForRko.Rows)
                        {
                            orderAmount += r["amount"] != DBNull.Value ? Convert.ToDecimal(r["amount"]) : 0;
                        }
                    }
                    // СТРАХОВКА: Если в Grid пусто (как обычно и бывает для ПКО/РКО), берем сумму ордера из _orderRow
                    else if (_orderRow != null && _orderRow.Table.Columns.Contains("amount") && _orderRow["amount"] != DBNull.Value)
                    {
                        orderAmount = Convert.ToDecimal(_orderRow["amount"]);
                    }

                    // === ЕСЛИ ЭТО ПКО ===
                    if (_docType == "ПКО")
                    {
                        var modelIn = new CashOrderInDataModel
                        {
                            OrganizationName = dbOrgName,
                            OrderNumber = _docNumber,
                            OrderDate = orderDate,
                            FromPersonGenitive = txtPerson.Text,
                            Basis = txtBasis.Text,
                            Amount = orderAmount,
                            Appendix = string.IsNullOrEmpty(txtAppendix.Text) ? "—" : txtAppendix.Text,
                            RectorName = printSignatures != null ? printSignatures.RectorName : "Настоятель",
                            TreasurerName = currentTreasurer
                        };

                        var reportIn = new CashOrderInReport(modelIn);
                        reportIn.GeneratePdf(tempFilePath);
                    }
                    // === ЕСЛИ ЭТО РКО ===
                    else if (_docType == "РКО")
                    {
                        // ИСПРАВЛЕНО: Убрали DateTime, так как переменная orderDate уже создана выше
                        orderDate = _docDate;
                        if (_orderRow != null && _orderRow.Table.Columns.Contains("date") && _orderRow["date"] != DBNull.Value)
                        {
                            DateTime.TryParse(_orderRow["date"].ToString(), out orderDate);
                        }

                        // ИСПРАВЛЕНО: Убрали decimal, так как переменная orderAmount уже создана выше
                        orderAmount = 0;
                        if (_orderRow != null && _orderRow.Table.Columns.Contains("amount") && _orderRow["amount"] != DBNull.Value)
                        {
                            orderAmount = Convert.ToDecimal(_orderRow["amount"]);
                        }

                        // Автоматически выгружаем паспортные данные получателя (согласно архитектуре вашей БД)
                        string dbPassportInfo = "";
                        using (var conn = new System.Data.SQLite.SQLiteConnection($"Data Source={dbPath};Version=3;"))
                        {
                            conn.Open();
                            string sqlPass = @"SELECT td.name as doc_type, series, number, issued_by, issue_date 
                           FROM id_documents idd 
                           LEFT JOIN type_id_document td ON idd.type_id_doc = td.id 
                           WHERE employee_id = 2 LIMIT 1"; // Дефолтный паспорт Настоятеля
                            using (var cmdP = new System.Data.SQLite.SQLiteCommand(sqlPass, conn))
                            using (var rdrP = cmdP.ExecuteReader())
                            {
                                if (rdrP.Read())
                                {
                                    string pDate = rdrP["issue_date"] != DBNull.Value ? Convert.ToDateTime(rdrP["issue_date"]).ToString("dd.MM.yyyy") : "";
                                    dbPassportInfo = $"{rdrP["doc_type"]} {rdrP["series"]} {rdrP["number"]}, выдан {rdrP["issued_by"]} {pDate} г.";
                                }
                            }
                        }

                        var modelOut = new CashOrderInDataModel
                        {
                            OrganizationName = dbOrgName,
                            OrderNumber = _docNumber,
                            OrderDate = orderDate,
                            FromPersonGenitive = txtPerson.Text,
                            Basis = txtBasis.Text,
                            Amount = orderAmount,
                            Appendix = string.IsNullOrEmpty(txtAppendix.Text) ? "—" : txtAppendix.Text,
                            RectorName = printSignatures != null ? printSignatures.RectorName : "Настоятель",
                            TreasurerName = currentTreasurer,
                            PassportInfo = dbPassportInfo // Передаем живую строку паспорта
                        };

                        var reportOut = new CashOrderOutReport(modelOut);
                        reportOut.GeneratePdf(tempFilePath);
                    }
                }

                // 3. Открываем созданный PDF в системном просмотрщике
                if (File.Exists(tempFilePath))
                {
                    Process.Start(new ProcessStartInfo(tempFilePath) { UseShellExecute = true });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при вызове оригинальной печати QuestPDF: {ex.Message}", "Ошибка печати", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // 3. Единственный метод для закрытия окна (и для верхнего крестика, и для нижней кнопки)
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        // Вспомогательный метод компиляции во временный файл (ТЕПЕРЬ СТРОГО ВНУТРИ КЛАССА)
        private void OpenPdfReport(QuestPDF.Infrastructure.IDocument report, string fileName)
        {
            string finalPath = Path.Combine(Path.GetTempPath(), fileName);
            report.GeneratePdf(finalPath);
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = finalPath, UseShellExecute = true });
        }

        private void txtPerson_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (_isLoading) return;
            if (lblBlankPerson != null) lblBlankPerson.Text = txtPerson.Text;
        }

        private void txtBasis_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (_isLoading) return;
            if (lblBlankBasis != null) lblBlankBasis.Text = txtBasis.Text;
        }
    }
}