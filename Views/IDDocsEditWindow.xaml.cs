using System;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Windows;
using System.Windows.Input;

namespace ChurchBudget_v3_WPF.Views
{
    public partial class IDDocsEditWindow : Window
    {
        // Путь к базе данных в папке Data в корне приложения
        private string dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "church.db");

        private string _documentId; // Хранит ID документа (null для добавления, не null для редактирования)

        // Конструктор для режима ДОБАВЛЕНИЯ нового документа
        public IDDocsEditWindow()
        {
            InitializeComponent();
            _documentId = null;

            LoadComboBoxes(); // Заполняем списки сотрудников и типов документов
        }

        // Конструктор для режима РЕДАКТИРОВАНИЯ существующего документа
        public IDDocsEditWindow(string documentId)
        {
            InitializeComponent();
            _documentId = documentId;

            LoadComboBoxes();   // 1. Сначала загружаем списки в ComboBox
            LoadDocumentData(); // 2. Затем заполняем поля старыми данными из БД
        }

        // Заполнение выпадающих списков ComboBox из справочников
        private void LoadComboBoxes()
        {
            try
            {
                // Стало (добавили параметр DateTimeFormat):
                using (var conn = new SQLiteConnection($"Data Source={dbPath};Version=3;DateTimeFormat=ISO8601;"))
                {
                    conn.Open();

                    // 1. Загрузка сотрудников со склейкой ФИО
                    string sqlEmployees = "SELECT id, (last_name || ' ' || first_name || ' ' || middle_name) AS [FullFIO] FROM personal ORDER BY last_name;";
                    using (var cmd = new SQLiteCommand(sqlEmployees, conn))
                    {
                        using (var adapter = new SQLiteDataAdapter(cmd))
                        {
                            DataTable dt = new DataTable();
                            adapter.Fill(dt);
                            cmbEmployee.ItemsSource = dt.DefaultView;
                        }
                    }

                    // 2. Загрузка типов документов
                    string sqlTypes = "SELECT id, name FROM type_id_document ORDER BY name;";
                    using (var cmd = new SQLiteCommand(sqlTypes, conn))
                    {
                        using (var adapter = new SQLiteDataAdapter(cmd))
                        {
                            DataTable dt = new DataTable();
                            adapter.Fill(dt);
                            cmbTypeIDDoc.ItemsSource = dt.DefaultView;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при загрузке справочников: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Загрузка данных документа в поля (работает только в режиме РЕДАКТИРОВАНИЯ)
        private void LoadDocumentData()
        {
            if (string.IsNullOrEmpty(_documentId)) return;

            try
            {
                using (var conn = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
                {
                    conn.Open();
                    string sql = "SELECT * FROM id_documents WHERE id = @id;";
                    using (var cmd = new SQLiteCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", _documentId);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                // Выставляем выбранные значения в списках по их числовым ID
                                cmbEmployee.SelectedValue = reader["employee_id"];
                                cmbTypeIDDoc.SelectedValue = reader["type_id_doc"];

                                txtSeries.Text = reader["series"].ToString();
                                txtNumber.Text = reader["number"].ToString();
                                txtIssuedBy.Text = reader["issued_by"].ToString();

                                // Работа с датой через календарь DatePicker
                                if (reader["issue_date"] != DBNull.Value)
                                {
                                    // Теперь библиотека сама распарсит строку "2026-08-02" в чистую дату
                                    dpIssueDate.SelectedDate = Convert.ToDateTime(reader["issue_date"]);
                                }
                                else
                                {
                                    dpIssueDate.SelectedDate = null;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при чтении данных документа: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Сохранение данных (общая кнопка для INSERT и UPDATE)
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // Простейшая валидация полей
            if (cmbEmployee.SelectedValue == null || cmbTypeIDDoc.SelectedValue == null ||
                string.IsNullOrWhiteSpace(txtNumber.Text) || string.IsNullOrWhiteSpace(txtIssuedBy.Text) ||
                dpIssueDate.SelectedDate == null)
            {
                MessageBox.Show("Пожалуйста, заполните все обязательные поля (Сотрудник, Тип документа, Номер, Кем выдан, Дата).",
                                "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using (var conn = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
                {
                    conn.Open();
                    string sql;

                    // Если _documentId пустой — делаем добавление новой записи
                    if (string.IsNullOrEmpty(_documentId))
                    {
                        sql = @"INSERT INTO id_documents (employee_id, type_id_doc, series, number, issued_by, issue_date) 
                                VALUES (@employee_id, @type_id_doc, @series, @number, @issued_by, @issue_date);";
                    }
                    // Иначе — обновляем существующую запись
                    else
                    {
                        sql = @"UPDATE id_documents 
                                SET employee_id = @employee_id, type_id_doc = @type_id_doc, series = @series, 
                                    number = @number, issued_by = @issued_by, issue_date = @issue_date 
                                WHERE id = @id;";
                    }

                    using (var cmd = new SQLiteCommand(sql, conn))
                    {
                        // Привязываем параметры из элементов интерфейса
                        cmd.Parameters.AddWithValue("@employee_id", cmbEmployee.SelectedValue);
                        cmd.Parameters.AddWithValue("@type_id_doc", cmbTypeIDDoc.SelectedValue);
                        cmd.Parameters.AddWithValue("@series", string.IsNullOrWhiteSpace(txtSeries.Text) ? (object)DBNull.Value : txtSeries.Text);
                        cmd.Parameters.AddWithValue("@number", txtNumber.Text.Trim());
                        cmd.Parameters.AddWithValue("@issued_by", txtIssuedBy.Text.Trim());

                        if (dpIssueDate.SelectedDate.HasValue)
                        {
                            // Принудительно форматируем в текст: "2026-08-02"
                            string isoDate = dpIssueDate.SelectedDate.Value.ToString("yyyy-MM-dd");
                            cmd.Parameters.AddWithValue("@issue_date", isoDate);
                        }
                        else
                        {
                            cmd.Parameters.AddWithValue("@issue_date", DBNull.Value);
                        }

                        if (!string.IsNullOrEmpty(_documentId))
                        {
                            cmd.Parameters.AddWithValue("@id", _documentId);
                        }

                        cmd.ExecuteNonQuery();
                    }
                }

                this.DialogResult = true; // Возвращаем статус успеха в родительскую форму для вызова RefreshData()
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при сохранении данных: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left) this.DragMove();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}
