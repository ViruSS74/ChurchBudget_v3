using System;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ChurchBudget_v3_WPF.Views
{
    public partial class EmployeeDirWindow : Window
    {
        private string dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data\\church.db");

        public EmployeeDirWindow()
        {
            InitializeComponent();

            // Подписываемся на событие загрузки формы, чтобы сразу показать данные
            this.Loaded += EmployeeDirWindow_Loaded;
        }

        private void EmployeeDirWindow_Loaded(object sender, RoutedEventArgs e)
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
                    string sql = "SELECT id, last_name, first_name, middle_name, role FROM personal;";

                    using (var cmd = new SQLiteCommand(sql, conn))
                    {
                        using (var adapter = new SQLiteDataAdapter(cmd))
                        {
                            DataTable dt = new DataTable();
                            adapter.Fill(dt);

                            // Передаем таблицу данных напрямую в DataGrid
                            dgPersonal.ItemsSource = dt.DefaultView;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки таблицы: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
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

        // Обработчик кнопки "Добавить"
        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            // Открываем пустую карточку (без передачи ID)
            EmployeeEditWindow editWindow = new EmployeeEditWindow();

            // Привязываем владельца, чтобы карточка открылась по центру окна справочника
            editWindow.Owner = this;

            // Если в карточке нажали "Сохранить" (DialogResult == true), обновляем таблицу на экране
            if (editWindow.ShowDialog() == true)
            {
                RefreshData();
            }
        }

        // Обработчик кнопки "Изменить"
        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            // Проверяем, выбрал ли пользователь строку в таблице DataGrid
            if (dgPersonal.SelectedItem != null)
            {
                // Извлекаем строку данных из DataTable
                DataRowView row = (DataRowView)dgPersonal.SelectedItem;
                string orgId = row["id"].ToString(); // Берем ID выбранного сотрудника

                // Открываем карточку и передаем туда ID для загрузки данных
                EmployeeEditWindow editWindow = new EmployeeEditWindow(orgId);
                editWindow.Owner = this;

                // Если данные успешно обновились и сохранились, перезагружаем таблицу
                if (editWindow.ShowDialog() == true)
                {
                    RefreshData();
                }
            }
            else
            {
                MessageBox.Show("Пожалуйста, выберите организацию из таблицы для изменения.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (dgPersonal.SelectedItem != null)
            {
                DataRowView row = (DataRowView)dgPersonal.SelectedItem;
                string orgName = row["name"].ToString();

                MessageBox.Show($"Удаление организации: {orgName}", "Справочник");
            }
            else
            {
                MessageBox.Show("Пожалуйста, выберите организацию из таблицы для удаления.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void dgPersonal_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            // Берем индекс строки, прибавляем 1 и записываем прямо в левое поле (Header)
            e.Row.Header = (e.Row.GetIndex() + 1).ToString();
        }
    }
}