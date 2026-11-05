using System;
using System.Data;
using System.Data.SQLite;

public class IncomeDocService
{
    private string _connectionString;

    public IncomeDocService(string dbPath)
    {
        _connectionString = string.Format("Data Source={0};Version=3;", dbPath);
    }

    /// <summary>
    /// Сохраняет документ и все его позиции в БД одной транзакцией
    /// </summary>
    public bool SaveDocument(string docNumber, string date, double total, DataTable items)
    {
        using (SQLiteConnection conn = new SQLiteConnection(_connectionString))
        {
            conn.Open();
            using (SQLiteTransaction transaction = conn.BeginTransaction())
            {
                try
                {
                    // 1. Сохраняем заголовок документа
                    long docId = InsertDocumentHeader(conn, transaction, docNumber, date, total);

                    // 2. Сохраняем все строки из таблицы (правая часть формы)
                    foreach (DataRow row in items.Rows)
                    {
                        // Пропускаем пустые строки, если они есть
                        if (row["Category"] == DBNull.Value || row["Amount"] == DBNull.Value) continue;

                        InsertDocumentItem(conn, transaction, docId,
                            row["Category"].ToString(),
                            Convert.ToDouble(row["Amount"]),
                            row["Description"] != DBNull.Value ? row["Description"].ToString() : "");
                    }

                    transaction.Commit();
                    return true;
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    throw new Exception("Ошибка при сохранении документа: " + ex.Message);
                }
            }
        }
    }

    private long InsertDocumentHeader(SQLiteConnection conn, SQLiteTransaction trans, string number, string date, double total)
    {
        string sql = "INSERT INTO income_docs (doc_number, date, total) VALUES (@num, @date, @total); SELECT last_insert_rowid();";
        using (SQLiteCommand cmd = new SQLiteCommand(sql, conn, trans))
        {
            cmd.Parameters.AddWithValue("@num", number);
            cmd.Parameters.AddWithValue("@date", date);
            cmd.Parameters.AddWithValue("@total", total);
            return (long)cmd.ExecuteScalar();
        }
    }

    private void InsertDocumentItem(SQLiteConnection conn, SQLiteTransaction trans, long docId, string category, double amount, string desc)
    {
        string sql = "INSERT INTO income_items (doc_id, category, amount, description) VALUES (@docId, @cat, @amount, @desc);";
        using (SQLiteCommand cmd = new SQLiteCommand(sql, conn, trans))
        {
            cmd.Parameters.AddWithValue("@docId", docId);
            cmd.Parameters.AddWithValue("@cat", category);
            cmd.Parameters.AddWithValue("@amount", amount);
            cmd.Parameters.AddWithValue("@desc", desc);
            cmd.ExecuteNonQuery();
        }
    }
}