using System;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ChurchBudget_v3_WPF.Views
{
    public partial class IDDocsDirWindow : Window
    {
        private string dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data\\church.db");

        public IDDocsDirWindow()
        {
            InitializeComponent();

            // Подписываемся на событие загрузки формы, чтобы сразу показать данные
            this.Loaded += IDDocsDirWindow_Loaded;
        }

        private void IDDocsDirWindow_Loaded(object sender, RoutedEventArgs e)
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

                // Стало (добавили параметр DateTimeFormat):
                using (var conn = new SQLiteConnection($"Data Source={dbPath};Version=3;DateTimeFormat=ISO8601;"))
                {
                    conn.Open();

                    // Новый SQL-запрос со склейкой ФИО и подтягиванием названий документов
                    string sql = @"
                SELECT 
                    d.id AS [id],
                    (p.last_name || ' ' || SUBSTR(p.first_name, 1, 1) || '.' || SUBSTR(p.middle_name, 1, 1) || '.') AS [Сотрудник],
                    t.name AS [Тип документа],
                    d.series AS [Серия],
                    d.number AS [Номер],
                    d.issued_by AS [Выдано],
                    d.issue_date AS [Дата выдачи],
                    d.employee_id,
                    d.type_id_doc
                FROM id_documents d
                LEFT JOIN personal p ON d.employee_id = p.id
                LEFT JOIN type_id_document t ON d.type_id_doc = t.id;";

                    using (var cmd = new SQLiteCommand(sql, conn))
                    {
                        using (var adapter = new SQLiteDataAdapter(cmd))
                        {
                            DataTable dt = new DataTable();
                            adapter.Fill(dt);

                            // Передаем таблицу данных напрямую в DataGrid
                            dgIDDocuments.ItemsSource = dt.DefaultView;
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
            IDDocsEditWindow editWindow = new IDDocsEditWindow();

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
            if (dgIDDocuments.SelectedItem != null)
            {
                // Извлекаем строку данных из DataTable
                DataRowView row = (DataRowView)dgIDDocuments.SelectedItem;

                // КРИТИЧЕСКОЕ ИСПРАВЛЕНИЕ: Берем именно ID самого документа, а не сотрудника!
                string documentId = row["id"].ToString();

                // Открываем карточку редактирования и передаем туда ID документа
                IDDocsEditWindow editWindow = new IDDocsEditWindow(documentId);
                editWindow.Owner = this;

                if (editWindow.ShowDialog() == true)
                {
                    RefreshData(); // Перезагружаем таблицу, чтобы увидеть изменения
                }
            }
            else
            {
                // Исправлен текст под контекст сотрудников и документов
                MessageBox.Show("Пожалуйста, выберите документ из таблицы для его изменения.",
                                "Внимание",
                                MessageBoxButton.OK,
                                MessageBoxImage.Warning);
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (dgIDDocuments.SelectedItem != null)
            {
                DataRowView row = (DataRowView)dgIDDocuments.SelectedItem;
                string orgName = row["name"].ToString();

                MessageBox.Show($"Удаление документа: {orgName}", "Справочник");
            }
            else
            {
                MessageBox.Show("Пожалуйста, выберите сотрудника из таблицы для удаления.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void dgIDDocuments_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            // Берем индекс строки, прибавляем 1 и записываем прямо в левое поле (Header)
            e.Row.Header = (e.Row.GetIndex() + 1).ToString();
        }
    }
}