using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Windows; // Для работы WPF MessageBox

namespace ChurchBudget_v3_WPF.Services
{
    public class ExpensesCategoryService
    {
        // Оригинальный класс-модель из WinForms
        public class ExpensesCategoryItem
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public int? ParentId { get; set; } // Чья это подкатегория 

            // Добавляем недостающие свойства для устранения ошибок компиляции:
            public string Description { get; set; }
            public string ReportCode { get; set; }
            public bool IsActive { get; set; }
        }

        public List<ExpensesCategoryItem> GetAll()
        {
            List<ExpensesCategoryItem> list = new List<ExpensesCategoryItem>();
            try
            {
                string dbPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Data\church.db");
                string connString = string.Format("Data Source={0};Version=3;", dbPath);

                using (SQLiteConnection conn = new SQLiteConnection(connString))
                {
                    conn.Open();
                    // Запрос из вашего оригинального WinForms кода
                    // Запрос ко всем полям таблицы
                    string sql = "SELECT id, name, description, is_active, parent_id, report_code FROM expense_categories ORDER BY name";

                    using (SQLiteCommand cmd = new SQLiteCommand(sql, conn))
                    using (SQLiteDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new ExpensesCategoryItem
                            {
                                Id = Convert.ToInt32(reader["id"]),
                                Name = reader["name"].ToString(),
                                ParentId = reader["parent_id"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["parent_id"]),

                                // Добавляем считывание новых полей:
                                Description = reader["description"] == DBNull.Value ? string.Empty : reader["description"].ToString(),
                                ReportCode = reader["report_code"] == DBNull.Value ? string.Empty : reader["report_code"].ToString(),
                                IsActive = reader["is_active"] != DBNull.Value && Convert.ToBoolean(reader["is_active"])
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка SQLite (income_categories): " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            return list;
        }

        public void Save(int? id, string name, int? parentId)
        {
            try
            {
                string dbPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Data\church.db");
                string connString = string.Format("Data Source={0};Version=3;", dbPath);

                using (SQLiteConnection conn = new SQLiteConnection(connString))
                {
                    conn.Open();
                    string sql;
                    if (id == null) // Новая запись
                        sql = "INSERT INTO income_categories (name, parent_id) VALUES (@name, @pid)";
                    else // Редактирование
                        sql = "UPDATE income_categories SET name = @name, parent_id = @pid WHERE id = @id";

                    using (SQLiteCommand cmd = new SQLiteCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@name", name);
                        cmd.Parameters.AddWithValue("@pid", (object)parentId ?? DBNull.Value);
                        if (id != null)
                            cmd.Parameters.AddWithValue("@id", id);

                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка сохранения в БД: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void Delete(int id)
        {
            try
            {
                string dbPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Data\church.db");
                string connString = string.Format("Data Source={0};Version=3;", dbPath);

                using (var conn = new SQLiteConnection(connString))
                {
                    conn.Open();
                    // Сначала проверим, нет ли у категории "детей" (подкатегорий)
                    string checkSql = "SELECT COUNT(*) FROM income_categories WHERE parent_id = @id";
                    using (var checkCmd = new SQLiteCommand(checkSql, conn))
                    {
                        checkCmd.Parameters.AddWithValue("@id", id);
                        int count = Convert.ToInt32(checkCmd.ExecuteScalar());
                        if (count > 0)
                        {
                            MessageBox.Show("Нельзя удалить категорию, у которой есть подкатегории!", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }
                    }

                    // Если детей нет — удаляем
                    string sql = "DELETE FROM income_categories WHERE id = @id";
                    using (var cmd = new SQLiteCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка удаления: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}