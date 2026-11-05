using System;
using System.Data.SQLite;

namespace ChurchBudget_v3_WPF.Services
{
    public class DBService
    {
        private readonly string _connectionString;

        public DBService(string connectionString)
        {
            _connectionString = connectionString;
        }

        // 1. Получить текущую версию базы данных из таблицы db_version
        public string GetDatabaseVersion()
        {
            try
            {
                using (var conn = new SQLiteConnection(_connectionString))
                {
                    conn.Open();
                    string sql = "SELECT version FROM db_version ORDER BY id DESC LIMIT 1;";
                    using (var cmd = new SQLiteCommand(sql, conn))
                    {
                        var result = cmd.ExecuteScalar();
                        return result != null ? result.ToString().Trim() : "1.0";
                    }
                }
            }
            catch
            {
                // Если таблицы db_version вообще нет в БД (старый формат), возвращаем базовую 1.0
                return "1.0";
            }
        }

        // 2. Универсальный метод для выполнения SQL-запросов обновления (миграций)
        public void ExecuteMigrationSql(string sql)
        {
            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // 3. Главный метод наката обновлений структуры и справочников
        public void RunDatabaseMigrations()
        {
            try
            {
                string currentVersion = GetDatabaseVersion();

                // === МИГРАЦИЯ 1.1: Исправление пробелов в Транспортных расходах ===
                if (currentVersion == "1.0")
                {
                    // Очищаем категорию в справочнике от скрытых символов и лишних пробелов
                    ExecuteMigrationSql(@"
                        UPDATE expense_categories 
                        SET name = 'Транспортные расходы', report_code = 'EXP_TRANSP' 
                        WHERE id = 2;");

                    // Подтягиваем старые записи в ордерах под чистое имя
                    ExecuteMigrationSql(@"
                        UPDATE expense_items 
                        SET category = 'Транспортные расходы' 
                        WHERE category LIKE '%Транспорт%';");

                    // Записываем новую версию структуры в БД клиента
                    ExecuteMigrationSql("UPDATE db_version SET version = '1.1';");
                    currentVersion = "1.1";
                }

                // === МИГРАЦИЯ 1.2: Пример добавления новой глобальной категории на будущее ===
                if (currentVersion == "1.1")
                {
                    // INSERT OR IGNORE застрахует от дублирования строк
                    ExecuteMigrationSql(@"
                        INSERT OR IGNORE INTO expense_categories (id, name, report_code, parent_id, is_active) 
                        VALUES (18, 'Новые Епархиальные сборы', 'EXP_NEW_DIOC', 0, 1);");

                    ExecuteMigrationSql("UPDATE db_version SET version = '1.2';");
                    currentVersion = "1.2";
                }

                // Сюда в будущем будете дописывать: if (currentVersion == "1.2") { ... }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DBService Error] Ошибка обновления базы: {ex.Message}");
            }
        }
    }
}
