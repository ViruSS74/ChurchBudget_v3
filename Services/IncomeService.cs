using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;

public class IncomeService
{
    private string GetConnectionString()
    {
        string dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
        dbPath = Path.Combine(dbPath, "church.db");
        return string.Format("Data Source={0};Version=3;", dbPath);
    }

    // Метод для получения всех записей из БД
    public List<CategoryModel> GetAll()
    {
        var list = new List<CategoryModel>();
        using (var conn = new SQLiteConnection(GetConnectionString()))
        {
            conn.Open();
            // Выбираем все поля, включая parent_id
            string sql = "SELECT id, name, description, is_active, parent_id FROM income_categories ORDER BY name ASC";
            using (var cmd = new SQLiteCommand(sql, conn))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    list.Add(new CategoryModel
                    {
                        Id = Convert.ToInt32(reader["id"]),
                        Name = reader["name"].ToString(),
                        Description = reader["description"].ToString(),
                        IsActive = Convert.ToInt32(reader["is_active"]),
                        // Обрабатываем NULL для parent_id
                        ParentId = reader["parent_id"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["parent_id"])
                    });
                }
            }
        }
        return list;
    }
}