using System;
using System.Data;
using System.Data.SQLite; // Используем родной пакет .NET Framework
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ChurchBudget_v3_WPF.Views
{
    public partial class OrganizationDirWindow : Window
    {
        private string dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data\\church.db");

        public OrganizationDirWindow()
        {
            InitializeComponent();

            // Подписываемся на событие загрузки формы, чтобы сразу показать данные
            this.Loaded += OrganizationDirWindow_Loaded;
        }

        private void OrganizationDirWindow_Loaded(object sender, RoutedEventArgs e)
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
                    string sql = "SELECT id, name, location, deanery, diocese FROM organizations;";

                    using (var cmd = new SQLiteCommand(sql, conn))
                    {
                        using (var adapter = new SQLiteDataAdapter(cmd))
                        {
                            DataTable dt = new DataTable();
                            adapter.Fill(dt);

                            // Передаем таблицу данных напрямую в DataGrid
                            dgOrganizations.ItemsSource = dt.DefaultView;
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
            OrganizationEditWindow editWindow = new OrganizationEditWindow();

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
            if (dgOrganizations.SelectedItem != null)
            {
                // Извлекаем строку данных из DataTable
                DataRowView row = (DataRowView)dgOrganizations.SelectedItem;
                string orgId = row["id"].ToString(); // Берем ID выбранной организации

                // Открываем карточку и передаем туда ID для загрузки данных
                OrganizationEditWindow editWindow = new OrganizationEditWindow(orgId);
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
            if (dgOrganizations.SelectedItem != null)
            {
                DataRowView row = (DataRowView)dgOrganizations.SelectedItem;
                string orgName = row["name"].ToString();

                MessageBox.Show($"Удаление организации: {orgName}", "Справочник");
            }
            else
            {
                MessageBox.Show("Пожалуйста, выберите организацию из таблицы для удаления.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void dgOrganizations_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            // Берем индекс строки, прибавляем 1 и записываем прямо в левое поле (Header)
            e.Row.Header = (e.Row.GetIndex() + 1).ToString();
        }
    }
}