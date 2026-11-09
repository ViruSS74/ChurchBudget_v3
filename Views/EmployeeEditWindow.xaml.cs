using System;
using System.Data.SQLite;
using System.IO;
using System.Windows;
using System.Windows.Input;

namespace ChurchBudget_v3_WPF.Views
{
    public partial class EmployeeEditWindow : Window
    {
        private string dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data\\church.db");
        private string currentId = ""; // Пустой для добавления, с номером — для изменения

        // Конструктор принимает ID строки. Если ID нет — это режим добавления новой записи.
        public EmployeeEditWindow(string id = "")
        {
            InitializeComponent();
            currentId = id;

            if (!string.IsNullOrEmpty(currentId))
            {
                // tblWindowTitle.Text = "Редактирование сотрудника";
                LoadOrgData();
            }
        }

        // Загрузка данных одного сотрудника из базы в текстовые поля
        private void LoadOrgData()
        {
            try
            {
                using (var conn = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
                {
                    conn.Open();
                    string sql = "SELECT last_name, first_name, middle_name, role FROM personal WHERE id = @id;";
                    using (var cmd = new SQLiteCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", currentId);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                txtLastName.Text = reader["last_name"].ToString();
                                txtFirstName.Text = reader["first_name"].ToString();
                                txtMiddleName.Text = reader["middle_name"].ToString();
                                txtRole.Text = reader["role"].ToString();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка чтения данных записи: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Кнопка "Сохранить" (выполняет INSERT или UPDATE)
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtLastName.Text))
            {
                MessageBox.Show("Поле 'Фамилия' является обязательным для заполнения!", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using (var conn = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
                {
                    conn.Open();
                    string sql = "";

                    if (string.IsNullOrEmpty(currentId))
                    {
                        sql = "INSERT INTO personal (last_name, first_name, middle_name, role) VALUES (@last_name, @first_name, @middle_name, @role);";
                    }
                    else
                    {
                        sql = "UPDATE personal SET last_name=@last_name, first_name=@first_name, middle_name=@middle_name, role=@role WHERE id=@id;";
                    }

                    using (var cmd = new SQLiteCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@last_name", txtLastName.Text.Trim());
                        cmd.Parameters.AddWithValue("@first_name", txtFirstName.Text.Trim());
                        cmd.Parameters.AddWithValue("@middle_name", txtMiddleName.Text.Trim());
                        cmd.Parameters.AddWithValue("@role", txtRole.Text.Trim());

                        if (!string.IsNullOrEmpty(currentId))
                            cmd.Parameters.AddWithValue("@id", currentId);

                        cmd.ExecuteNonQuery();
                    }
                }

                // Сигнализируем родительской форме, что данные успешно сохранены в БД
                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка сохранения в базу данных: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
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