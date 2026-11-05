using System;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Xml.Linq;

namespace ChurchBudget_v3_WPF.Views
{
    public partial class OpeningBalanceWindow : Window
    {
        private string dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data\\church.db");
        public OpeningBalanceWindow()
        {
            InitializeComponent();
            // Подписываемся на событие загрузки формы, чтобы сразу показать данные
            this.Loaded += OpeningBalanceWindow_Loaded;
        }

        private void OpeningBalanceWindow_Loaded(object sender, RoutedEventArgs e)
        {
            RefreshData();
        }

        // Метод чтения данных из базы и вывода в DataGrid
        private void RefreshData()
        {
            try
            {
                if (!File.Exists(dbPath))
                {
                    MessageBox.Show("Файл базы данных не найден!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                using (var conn = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
                {
                    conn.Open();
                    string sql = @"
                        SELECT 
                            d.id AS [id],
                            d.balance_date AS [balance_date],
                            d.amount AS [amount],
                            d.comment AS [comment],
                            d.created_at AS [created_at]
                        FROM opening_balances d;";
                    using (var cmd = new SQLiteCommand(sql, conn))
                    {
                        using (var adapter = new SQLiteDataAdapter(cmd))
                        {
                            DataTable dt = new DataTable();
                            adapter.Fill(dt);
                            // Передаем таблицу данных напрямую в DataGrid
                            dgOpeningBalance.ItemsSource = dt.DefaultView;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки таблицы: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left) this.DragMove();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e) { this.Close(); }

        // Обработчик кнопки "Сохранить"
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // 1. ВАЛИДАЦИЯ: Проверяем выбор даты
            if (!dpBalanceDate.SelectedDate.HasValue)
            {
                MessageBox.Show("Пожалуйста, выберите дату ввода остатка!", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 2. ВАЛИДАЦИЯ: Проверяем и парсим сумму
            string sumText = txtBalanceSumm.Text.Trim().Replace(',', '.'); // заменяем запятую на точку для надежности
            if (string.IsNullOrWhiteSpace(sumText) || !decimal.TryParse(sumText, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal balanceAmount))
            {
                MessageBox.Show("Пожалуйста, введите корректную сумму остатка!", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 3. Подготовка данных
            DateTime selectedDate = dpBalanceDate.SelectedDate.Value;
            string commentText = txtComment.Text.Trim();

            // 4. ЗАПИСЬ В БАЗУ ДАННЫХ SQLite
            try
            {
                using (var conn = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
                {
                    conn.Open();

                    // SQL-запрос на добавление записи
                    string sql = @"INSERT INTO opening_balances (organization_id, balance_date, amount, comment, created_at) 
                           VALUES (@organization_id, @balance_date, @amount, @comment, @created_at);";

                    using (var cmd = new SQLiteCommand(sql, conn))
                    {
                        // Привязываем параметры, конвертируя типы для SQLite
                        cmd.Parameters.AddWithValue("@organization_id", 1);
                        cmd.Parameters.AddWithValue("@balance_date", selectedDate.ToString("yyyy-MM-dd"));
                        cmd.Parameters.AddWithValue("@amount", (double)balanceAmount); // SQLite любит double для вещественных чисел
                        cmd.Parameters.AddWithValue("@comment", commentText);
                        cmd.Parameters.AddWithValue("@created_at", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

                        cmd.ExecuteNonQuery();
                    }
                }

                // 5. Очищаем поля ввода для следующего документа
                txtBalanceSumm.Text = string.Empty;
                txtComment.Text = string.Empty;
                dpBalanceDate.SelectedDate = DateTime.Now;

                // 6. Перезагружаем таблицу, чтобы новая строка сразу появилась на экране
                RefreshData();

                MessageBox.Show("Начальный остаток успешно сохранен!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при сохранении в базу данных: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Обработчик кнопки "Отмена"
        private void CancelButton_Click(object sender, RoutedEventArgs e) { this.Close(); }
    }
}