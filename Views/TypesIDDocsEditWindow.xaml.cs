using System;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Windows;
using System.Windows.Input;

namespace ChurchBudget_v3_WPF.Views
{
    public partial class TypesIDDocsEditWindow : Window
    {
        // Путь к базе данных в папке Data в корне приложения
        private string dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "church.db");
        private string _documentId; // Хранит ID типа документа (null для добавления, не null для редактирования)

        // Конструктор для режима ДОБАВЛЕНИЯ нового типа документа
        public TypesIDDocsEditWindow()
        {
            InitializeComponent();
            _documentId = null;
            // Метод LoadComboBoxes() удален, так как комбобоксов больше нет
        }

        // Конструктор для режима РЕДАКТИРОВАНИЯ существующего типа документа
        public TypesIDDocsEditWindow(string documentId)
        {
            InitializeComponent();
            _documentId = documentId;

            // Сразу загружаем старое текстовое название для редактирования
            LoadDocumentData();
        }

        // Загрузка данных в текстовое поле (работает только в режиме РЕДАКТИРОВАНИЯ)
        private void LoadDocumentData()
        {
            if (string.IsNullOrEmpty(_documentId)) return;

            try
            {
                using (var conn = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
                {
                    conn.Open();
                    // ИСПРАВЛЕНО: Выбираем данные именно из таблицы типов документов
                    string sql = "SELECT name FROM type_id_document WHERE id = @id;";
                    using (var cmd = new SQLiteCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", _documentId);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                // Помещаем текст из базы в новый TextBox
                                txtTypeName.Text = reader["name"].ToString();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при чтении данных: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Сохранение данных (общая кнопка для INSERT и UPDATE)
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // ИСПРАВЛЕНО: Валидация — проверяем, что TextBox не пустой
            if (string.IsNullOrWhiteSpace(txtTypeName.Text))
            {
                MessageBox.Show("Введите название типа документа!", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string newTypeName = txtTypeName.Text.Trim();

            try
            {
                using (var conn = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
                {
                    conn.Open();
                    string sql;

                    // Если _documentId пустой — добавляем новую запись
                    if (string.IsNullOrEmpty(_documentId))
                    {
                        // Поле id обычно является AUTOINCREMENT в SQLite, 
                        // поэтому при добавлении передаем ТОЛЬКО name. База сама назначит ID.
                        sql = @"INSERT INTO type_id_document (name) VALUES (@name);";
                    }
                    // Иначе — обновляем существующую запись
                    else
                    {
                        sql = @"UPDATE type_id_document SET name = @name WHERE id = @id;";
                    }

                    using (var cmd = new SQLiteCommand(sql, conn))
                    {
                        // Берем значение из TextBox
                        cmd.Parameters.AddWithValue("@name", newTypeName);

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