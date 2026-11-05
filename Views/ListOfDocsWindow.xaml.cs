using ChurchBudget_v3_WPF.Reports;
using ChurchBudget_v3_WPF.Services;
using QuestPDF.Fluent;
using System;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ChurchBudget_v3_WPF.Views
{
    public partial class ListOfDocsWindow : Window
    {
        private readonly string dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data\\church.db");
        private bool isInitialized = false;
        private ListOfDocsService _docsService; // Наше поле сервиса

        // Основной конструктор окна
        public ListOfDocsWindow()
        {
            InitializeComponent();

            // Железно присваиваем значение полю сервиса при создании окна
            _docsService = new ListOfDocsService(dbPath);

            this.Loaded += ListOfDocsWindow_Loaded;
        }

        // Если у вас ниже был второй конструктор (например, для передачи сервиса из главного окна), 
        // убедитесь, что в нем тоже присваивается значение:
        public ListOfDocsWindow(ListOfDocsService docsService) : this()
        {
            if (docsService != null)
            {
                _docsService = docsService;
            }
        }

        private void ListOfDocsWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // 1. Устанавливаем первоначальный период отображения (например, за текущий месяц)
            dpFromDate.SelectedDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            dpToDate.SelectedDate = DateTime.Now;

            // 2. ЗАПОЛНЕНИЕ КОМБОБОКСА ПУНКТАМИ ФИЛЬТРА
            cmbTypeOfDocs.Items.Clear();
            cmbTypeOfDocs.Items.Add("Все");
            cmbTypeOfDocs.Items.Add("Доходы");
            cmbTypeOfDocs.Items.Add("Расходы");
            cmbTypeOfDocs.Items.Add("ПКО");
            cmbTypeOfDocs.Items.Add("РКО");

            // Выбираем по умолчанию самый первый пункт — "Все"
            cmbTypeOfDocs.SelectedIndex = 0;

            // 3. ПОДПИСКА НА СОБЫТИЯ ИЗМЕНЕНИЯ ФИЛЬТРОВ
            // Как только пользователь изменит дату или тип документа — сработает метод FilterChanged
            dpFromDate.SelectedDateChanged += FilterChanged;
            dpToDate.SelectedDateChanged += FilterChanged;
            cmbTypeOfDocs.SelectionChanged += FilterChanged;

            // Поднимаем флаг готовности окна и производим первую загрузку данных
            isInitialized = true;
            RefreshData();
        }

        // Этот метод автоматически срабатывает каждый раз, когда выбирают новый пункт в ComboBox или меняют дату
        private void FilterChanged(object sender, SelectionChangedEventArgs e)
        {
            // Если окно полностью загружено — обновляем таблицу на экране
            if (isInitialized)
            {
                RefreshData();
            }
        }

        // Метод чтения данных из базы и вывода в DataGrid (С новыми колонками ФИО и Оснований)
        // Метод чтения данных из базы и вывода в DataGrid (С исправленным UNION ALL для ПКО/РКО)
        private void RefreshData()
        {
            try
            {
                if (!File.Exists(dbPath))
                {
                    MessageBox.Show("Файл базы данных не найден!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                DateTime startDate = dpFromDate.SelectedDate ?? DateTime.MinValue;
                DateTime endDate = dpToDate.SelectedDate ?? DateTime.MaxValue;
                string filterType = cmbTypeOfDocs.SelectedItem?.ToString() ?? "Все";

                using (var conn = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
                {
                    conn.Open();

                    // Исправленный сквозной SQL-запрос
                    string sql = @"
                SELECT id, date, doc_number, doc_type, doc_ref_id, person_name, main_basis FROM (
                    -- 1. Рапортички доходов
                    SELECT id, date, doc_number, 'Доходы' AS doc_type, id AS doc_ref_id, person_name, main_basis 
                    FROM income_docs 
                    WHERE date(date) >= date(@start) AND date(date) <= date(@end)
                    
                    UNION ALL
                    
                    -- 2. Ведомости расходов
                    SELECT id, date, doc_number, 'Расходы' AS doc_type, id AS doc_ref_id, person_name, main_basis 
                    FROM expense_docs 
                    WHERE date(date) >= date(@start) AND date(date) <= date(@end)
                    
                    UNION ALL
                    
                    -- 3. Приходные кассовые ордера (Умно соединяем с таблицей personal для вывода ФИО)
                    SELECT 
                        co.id, 
                        co.date, 
                        co.order_number AS doc_number, 
                        'ПКО' AS doc_type, 
                        co.doc_ref_id, 
                        COALESCE(p.last_name || ' ' || p.first_name || ' ' || p.middle_name, co.person_name_manual, co.person) AS person_name, 
                        co.base AS main_basis 
                    FROM cash_orders co
                    LEFT JOIN personal p ON co.person_id = p.id
                    WHERE co.order_type = 'ПКО' AND date(co.date) >= date(@start) AND date(co.date) <= date(@end)
                    
                    UNION ALL
                    
                    -- 4. Расходные кассовые ордера (Точно так же соединяем с personal)
                    SELECT 
                        co.id, 
                        co.date, 
                        co.order_number AS doc_number, 
                        'РКО' AS doc_type, 
                        co.doc_ref_id, 
                        COALESCE(p.last_name || ' ' || p.first_name || ' ' || p.middle_name, co.person_name_manual, co.person) AS person_name, 
                        co.base AS main_basis 
                    FROM cash_orders co
                    LEFT JOIN personal p ON co.person_id = p.id
                    WHERE co.order_type = 'РКО' AND date(co.date) >= date(@start) AND date(co.date) <= date(@end)
                )
                WHERE @filter = 'Все' OR doc_type = @filter
                ORDER BY date DESC, id DESC;";

                    using (var cmd = new SQLiteCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@start", startDate.ToString("yyyy-MM-dd"));
                        cmd.Parameters.AddWithValue("@end", endDate.ToString("yyyy-MM-dd"));
                        cmd.Parameters.AddWithValue("@filter", filterType);

                        using (var adapter = new SQLiteDataAdapter(cmd))
                        {
                            DataTable dt = new DataTable();
                            adapter.Fill(dt);

                            // Добавляем колонку Порядкового номера [RowNumber] для XAML
                            dt.Columns.Add("RowNumber", typeof(int));

                            // Страхуем структуру DataTable
                            if (!dt.Columns.Contains("PersonName")) dt.Columns.Add("PersonName", typeof(string));
                            if (!dt.Columns.Contains("MainBasis")) dt.Columns.Add("MainBasis", typeof(string));

                            for (int i = 0; i < dt.Rows.Count; i++)
                            {
                                DataRow row = dt.Rows[i];

                                // Присваиваем № по порядку
                                row["RowNumber"] = i + 1;

                                // Заполняем свойства отображения для DataGrid
                                row["PersonName"] = row["person_name"] != DBNull.Value ? row["person_name"].ToString().Trim() : string.Empty;
                                row["MainBasis"] = row["main_basis"] != DBNull.Value ? row["main_basis"].ToString().Trim() : string.Empty;
                            }

                            // Выводим данные в таблицу журнала
                            dgListOfDocs.ItemsSource = dt.DefaultView;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки журнала документов: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // =========================================================================
        // ОБРАБОТЧИКИ КНОПОК И ИНТЕРФЕЙСА
        // =========================================================================

        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left) this.DragMove();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        // Обработчик кнопки "Открыть"
        private void OpenButton_Click(object sender, RoutedEventArgs e)
        {
            if (dgListOfDocs.SelectedItem == null)
            {
                MessageBox.Show("Пожалуйста, выберите документ из таблицы.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            DataRowView row = (DataRowView)dgListOfDocs.SelectedItem;
            int docId = Convert.ToInt32(row["id"]);
            string docType = row["doc_type"].ToString();
            string docNumber = row["doc_number"].ToString();
            DateTime docDate = Convert.ToDateTime(row["date"]);

            // Открываем единую форму просмотра и редактирования
            DocEditorWindow editorWindow = new DocEditorWindow(docId, docType, docNumber, docDate, _docsService);

            // Делаем главное окно владельцем, чтобы редактор открылся строго по центру над ним
            editorWindow.Owner = this;

            // Открываем модально (блокирует список, пока не закроют редактор)
            editorWindow.ShowDialog();

            // После закрытия редактора обновляем главную таблицу (на случай, если поменялись данные)
            RefreshData();
        }

        // Обработчик кнопки "Удалить"
        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (dgListOfDocs.SelectedItem == null)
            {
                MessageBox.Show("Пожалуйста, выберите документ для удаления.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            DataRowView row = (DataRowView)dgListOfDocs.SelectedItem;
            string docId = row["id"].ToString();
            string docType = row["doc_type"].ToString();
            string docNumber = row["doc_number"].ToString();

            MessageBoxResult result = MessageBox.Show(
                $"Вы уверены, что хотите безвозвратно удалить документ \"{docNumber}\" ({docType})?\nЭто действие очистит связанные позиции содержимого!",
                "Подтверждение каскадного удаления", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    using (var conn = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
                    {
                        conn.Open();
                        using (var trans = conn.BeginTransaction())
                        {
                            try
                            {
                                if (docType == "Доходы")
                                {
                                    // Удаляем наполнение рапортички
                                    using (var cmd = new SQLiteCommand("DELETE FROM income_items WHERE doc_id = @id", conn, trans))
                                    {
                                        cmd.Parameters.AddWithValue("@id", docId); cmd.ExecuteNonQuery();
                                    }
                                    // Удаляем саму рапортичку
                                    using (var cmd = new SQLiteCommand("DELETE FROM income_docs WHERE id = @id", conn, trans))
                                    {
                                        cmd.Parameters.AddWithValue("@id", docId); cmd.ExecuteNonQuery();
                                    }
                                }
                                else if (docType == "Расходы")
                                {
                                    // Удаляем аналитику расходов
                                    using (var cmd = new SQLiteCommand("DELETE FROM expense_items WHERE doc_id = @id", conn, trans))
                                    {
                                        cmd.Parameters.AddWithValue("@id", docId); cmd.ExecuteNonQuery();
                                    }
                                    // Удаляем шапку расходов
                                    using (var cmd = new SQLiteCommand("DELETE FROM expense_docs WHERE id = @id", conn, trans))
                                    {
                                        cmd.Parameters.AddWithValue("@id", docId); cmd.ExecuteNonQuery();
                                    }
                                }
                                else if (docType == "ПКО" || docType == "РКО")
                                {
                                    // Ордера удаляются прямо из сквозного реестра кассы
                                    using (var cmd = new SQLiteCommand("DELETE FROM cash_orders WHERE id = @id", conn, trans))
                                    {
                                        cmd.Parameters.AddWithValue("@id", docId); cmd.ExecuteNonQuery();
                                    }
                                }

                                trans.Commit();
                            }
                            catch
                            {
                                trans.Rollback();
                                throw;
                            }
                        }
                    }

                    RefreshData(); // Обновляем грид
                    MessageBox.Show("Документ и все его вложенные позиции успешно удалены.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка физического удаления: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}