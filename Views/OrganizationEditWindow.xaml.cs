using System;
using System.IO;
using System.Data.SQLite;
using System.Windows;
using System.Windows.Input;

namespace ChurchBudget_v3_WPF.Views
{
    public partial class OrganizationEditWindow : Window
    {
        private string dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data\\church.db");
        private string currentId = ""; // Пустой для добавления, с номером — для изменения

        // Конструктор принимает ID строки. Если ID нет — это режим добавления новой записи.
        public OrganizationEditWindow(string id = "")
        {
            InitializeComponent();
            currentId = id;

            if (!string.IsNullOrEmpty(currentId))
            {
                LoadOrgData();
            }
        }

        // Загрузка данных одной организации из базы в текстовые поля
        private void LoadOrgData()
        {
            try
            {
                using (var conn = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
                {
                    conn.Open();
                    string sql = "SELECT name, location, deanery, diocese FROM organizations WHERE id = @id;";
                    using (var cmd = new SQLiteCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", currentId);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                txtName.Text = reader["name"].ToString();
                                txtLocation.Text = reader["location"].ToString();
                                txtDeanery.Text = reader["deanery"].ToString();
                                txtDiocese.Text = reader["diocese"].ToString();
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
            if (string.IsNullOrWhiteSpace(txtName.Text))
            {
                MessageBox.Show("Поле 'Название' является обязательным для заполнения!", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
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
                        sql = "INSERT INTO organizations (name, location, deanery, diocese) VALUES (@name, @location, @deanery, @diocese);";
                    }
                    else
                    {
                        sql = "UPDATE organizations SET name=@name, location=@location, deanery=@deanery, diocese=@diocese WHERE id=@id;";
                    }

                    using (var cmd = new SQLiteCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@name", txtName.Text.Trim());
                        cmd.Parameters.AddWithValue("@location", txtLocation.Text.Trim());
                        cmd.Parameters.AddWithValue("@deanery", txtDeanery.Text.Trim());
                        cmd.Parameters.AddWithValue("@diocese", txtDiocese.Text.Trim());

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