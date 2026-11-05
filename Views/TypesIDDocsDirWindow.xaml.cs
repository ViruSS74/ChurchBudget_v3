using System;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ChurchBudget_v3_WPF.Views
{
    public partial class TypesIDDocsDirWindow : Window
    {
        private string dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data\\church.db");

        public TypesIDDocsDirWindow()
        {
            InitializeComponent();
            // Подписываемся на событие загрузки формы, чтобы сразу показать данные
            this.Loaded += TypesIDDocsDirWindow_Loaded;
        }

        private void TypesIDDocsDirWindow_Loaded(object sender, RoutedEventArgs e)
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
                            d.name AS [name] 
                        FROM type_id_document d;";

                    using (var cmd = new SQLiteCommand(sql, conn))
                    {
                        using (var adapter = new SQLiteDataAdapter(cmd))
                        {
                            DataTable dt = new DataTable();
                            adapter.Fill(dt);
                            // Передаем таблицу данных напрямую в DataGrid
                            dgTypesIDDocs.ItemsSource = dt.DefaultView;
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
            TypesIDDocsEditWindow editWindow = new TypesIDDocsEditWindow();
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
            // 1. Проверяем, выбрана ли строка в DataGrid
            if (dgTypesIDDocs.SelectedItem != null)
            {
                // Извлекаем строку данных из DataTable
                DataRowView row = (DataRowView)dgTypesIDDocs.SelectedItem;
                string documentId = row["id"].ToString();

                // ИСПРАВЛЕНО: Передаем documentId в конструктор, чтобы окно открылось в режиме РЕДАКТИРОВАНИЯ
                TypesIDDocsEditWindow editWindow = new TypesIDDocsEditWindow(documentId);
                editWindow.Owner = this;

                // Если в открытом окне нажали "Сохранить" (DialogResult = true)
                if (editWindow.ShowDialog() == true)
                {
                    RefreshData(); // Перезагружаем таблицу, чтобы увидеть изменения
                }
            }
            else
            {
                MessageBox.Show("Пожалуйста, выберите документ из таблицы для его изменения.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        // Обработчик кнопки "Удалить"
        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            // 1. Проверяем, выбрана ли строка
            if (dgTypesIDDocs.SelectedItem != null)
            {
                DataRowView row = (DataRowView)dgTypesIDDocs.SelectedItem;
                string documentId = row["id"].ToString();
                string docName = row["name"].ToString();

                // ИСПРАВЛЕНО: Спрашиваем подтверждение пользователя перед удалением
                MessageBoxResult result = MessageBoxResult.No;
                result = MessageBox.Show($"Вы уверены, что хотите полностью удалить тип документа: \"{docName}\"?",
                                         "Подтверждение удаления",
                                         MessageBoxButton.YesNo,
                                         MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        // ИСПРАВЛЕНО: Реализовано физическое удаление записи из SQLite
                        using (var conn = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
                        {
                            conn.Open();
                            string sql = "DELETE FROM type_id_document WHERE id = @id;";

                            using (var cmd = new SQLiteCommand(sql, conn))
                            {
                                cmd.Parameters.AddWithValue("@id", documentId);
                                cmd.ExecuteNonQuery();
                            }
                        }

                        // Обновляем список, чтобы удаленная строка исчезла с экрана
                        RefreshData();
                        MessageBox.Show("Запись успешно удалена.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Ошибка при удалении записи: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            else
            {
                MessageBox.Show("Пожалуйста, выберите документ из таблицы для удаления.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void dgTypesIDDocs_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            // Берем индекс строки, прибавляем 1 и записываем прямо в левое поле (Header)
            e.Row.Header = (e.Row.GetIndex() + 1).ToString();
        }
    }
}