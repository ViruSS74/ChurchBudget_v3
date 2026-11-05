using ChurchBudget_v3_WPF.Services;
using System;
using System.Data;
using System.Data.SQLite;

public class CashbookService
{
    private readonly string _connectionString;
    private readonly ConvertService _convert; // Добавляем поле

    public CashbookService(string connectionString)
    {
        _connectionString = connectionString;
        _convert = new ConvertService();
    }

    // Получить начальный остаток на начало года
    public decimal GetOpeningBalanceForYear(int year)
    {
        using (var conn = new SQLiteConnection(_connectionString))
        {
            conn.Open();
            string sql = @"SELECT amount FROM opening_balances 
                       WHERE balance_date = @date LIMIT 1";
            using (var cmd = new SQLiteCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@date", $"{year}-01-01");
                var result = cmd.ExecuteScalar();

                // ИСПРАВЛЕНО: Безопасный перевод начального остатка с поддержкой точек и запятых
                return _convert.SafeToDecimal(result);
            }
        }
    }

    // Получить все операции (ПКО и РКО)
    public DataTable GetOperationsForYear(int year)
    {
        using (var conn = new SQLiteConnection(_connectionString))
        {
            conn.Open();

            string startDate = $"{year}-01-01";
            string endDate = $"{year}-12-31";

            string sql = @"
            SELECT co.id, co.date, co.order_type, co.amount
            FROM cash_orders co
            WHERE co.date BETWEEN @startDate AND @endDate
            ORDER BY co.date, co.id";

            using (var cmd = new SQLiteCommand(sql, conn))
            {
                cmd.Parameters.Add("@startDate", System.Data.DbType.String).Value = startDate;
                cmd.Parameters.Add("@endDate", System.Data.DbType.String).Value = endDate;

                using (var adapter = new SQLiteDataAdapter(cmd))
                {
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);
                    return dt;
                }
            }
        }
    }

    public DataTable GetOperationsForMonth(int year, int month)
    {
        using (var conn = new SQLiteConnection(_connectionString))
        {
            conn.Open();
            string sql = @"
                SELECT 
                    co.id,
                    co.order_number,
                    co.date,
                    co.order_type,
                    co.amount,
                    COALESCE(co.person_name_manual, '') as counterparty,
                    COALESCE(co.base, '') as base,
                    COALESCE(co.appendix, '') as appendix
                FROM cash_orders co
                WHERE strftime('%Y', co.date) = @year
                AND strftime('%m', co.date) = @month
                ORDER BY co.date, co.id";

            using (var cmd = new SQLiteCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@year", year.ToString());
                cmd.Parameters.AddWithValue("@month", month.ToString("D2"));
                using (var adapter = new SQLiteDataAdapter(cmd))
                {
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);
                    return dt;
                }
            }
        }
    }

    // Получить операции за конкретный день
    // Получить операции за конкретный день
    public DataTable GetOperationsForDay(int year, int month, int day)
    {
        using (var conn = new SQLiteConnection(_connectionString))
        {
            conn.Open();

            // Форматируем дату в строку "YYYY-MM-DD". Пример: "2026-05-01"
            string dateStr = $"{year}-{month:D2}-{day:D2}";

            // Используем LIKE '@dateStr%', чтобы находить и чистую дату, 
            // и дату, внутри которой случайно записалось время (2026-05-01 12:00:00)
            string sql = @"
            SELECT 
                co.id,
                co.order_number,
                co.date,
                co.order_type,
                co.amount,
                COALESCE(co.person_name_manual, '') as counterparty,
                COALESCE(co.base, '') as base
            FROM cash_orders co
            WHERE co.date LIKE @dateParam
            ORDER BY co.id";

            using (var cmd = new SQLiteCommand(sql, conn))
            {
                // Явно указываем тип параметра как String и добавляем знак процента % для LIKE
                cmd.Parameters.Add("@dateParam", System.Data.DbType.String).Value = dateStr + "%";

                using (var adapter = new SQLiteDataAdapter(cmd))
                {
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);
                    return dt;
                }
            }
        }
    }

    public int GetOperationsCountBeforeDay(int year, int month, int day)
    {
        using (var conn = new SQLiteConnection(_connectionString))
        {
            conn.Open();
            // Формируем дату начала текущего дня
            string targetDateStr = $"{year}-{month:D2}-{day:D2}";
            // Формируем дату начала года
            string startOfYearStr = $"{year}-01-01";

            string sql = @"
            SELECT COUNT(*) 
            FROM cash_orders 
            WHERE date(date) >= date(@startOfYear) 
              AND date(date) < date(@targetDate)";

            using (var cmd = new SQLiteCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@startOfYear", startOfYearStr);
                cmd.Parameters.AddWithValue("@targetDate", targetDateStr);

                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }
    }

    // Расчет остатка на конец дня
    // Абсолютно надежный расчет остатка на конец дня
    public decimal CalculateClosingBalance(int year, int month, int day)
    {
        // 1. Получаем стартовый баланс года (метод уже защищен от пустых значений)
        decimal openingBalance = GetOpeningBalanceForYear(year);

        using (var conn = new SQLiteConnection(_connectionString))
        {
            conn.Open();

            // 2. Строго формируем дату в формате ISO (YYYY-MM-DD), как она лежит в базе TEXT
            string endDateStr = $"{year}-{month:D2}-{day:D2}";

            // 3. Используем прямое сравнение строк БЕЗ функций date(), чтобы исключить NULL
            string sqlPko = @"SELECT COALESCE(SUM(amount), 0) 
            FROM cash_orders 
            WHERE order_type = 'ПКО' 
            AND date <= @endDate";

            string sqlRko = @"SELECT COALESCE(SUM(amount), 0) 
            FROM cash_orders 
            WHERE order_type = 'РКО' 
            AND date <= @endDate";

            using (var cmdPko = new SQLiteCommand(sqlPko, conn))
            using (var cmdRko = new SQLiteCommand(sqlRko, conn))
            {
                // 4. ГЛАВНОЕ ИСПРАВЛЕНИЕ: Жестко задаем тип параметра как String.
                // Теперь драйвер не сможет перепутать типы данных, и базы сработают одинаково!
                cmdPko.Parameters.Add("@endDate", System.Data.DbType.String).Value = endDateStr;
                cmdRko.Parameters.Add("@endDate", System.Data.DbType.String).Value = endDateStr;

                // 5. Безопасно забираем суммы (даже если SQLite вернет тип double)
                // Использование становится очень коротким и изящным:
                decimal totalPko = _convert.SafeToDecimal(cmdPko.ExecuteScalar());
                decimal totalRko = _convert.SafeToDecimal(cmdRko.ExecuteScalar());

                return openingBalance + totalPko - totalRko;
            }
        }
    }

    // Получить информацию об организации
    public DataRow GetOrganizationInfo()
    {
        using (var conn = new SQLiteConnection(_connectionString))
        {
            conn.Open();
            string sql = "SELECT name, location FROM organizations LIMIT 1";
            DataTable dt = new DataTable();
            new SQLiteDataAdapter(sql, conn).Fill(dt);
            return dt.Rows.Count > 0 ? dt.Rows[0] : null;
        }
    }

    // Получить ФИО кассира и бухгалтера
    public (string Cashier, string Accountant) GetSignatures()
    {
        string cashier = "";
        string accountant = "";

        using (var conn = new SQLiteConnection(_connectionString))
        {
            conn.Open();
            string sql = "SELECT last_name, first_name, middle_name, role FROM personal";
            using (var cmd = new SQLiteCommand(sql, conn))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    string role = reader["role"].ToString().ToLower();
                    string fullName = $"{reader["last_name"]} {reader["first_name"]} {reader["middle_name"]}";

                    if (role.Contains("казначей"))
                        cashier = fullName;
                    else if (role.Contains("бухгалтер"))
                        accountant = fullName;
                }
            }
        }

        return (cashier, accountant);
    }

    // Получить ФИО Настоятеля (руководителя) и Казначея (главного бухгалтера)
    public (string Leader, string Accountant) GetStampSignatures()
    {
        string leader = "";
        string accountant = "";

        using (var conn = new SQLiteConnection(_connectionString))
        {
            conn.Open();
            string sql = "SELECT last_name, first_name, middle_name, role FROM personal";
            using (var cmd = new SQLiteCommand(sql, conn))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    string role = reader["role"].ToString().ToLower();
                    string fullName = $"{reader["last_name"]} {reader["first_name"]} {reader["middle_name"]}".Trim();

                    if (role.Contains("настоятель"))
                        leader = fullName;
                    else if (role.Contains("казначей"))
                        accountant = fullName;
                }
            }
        }

        return (leader, accountant);
    }


    // Получить количество дней в месяце
    public int GetDaysInMonth(int year, int month)
    {
        return DateTime.DaysInMonth(year, month);
    }

    // Проверить, есть ли операции за день
    public bool HasOperationsForDay(int year, int month, int day)
    {
        using (var conn = new SQLiteConnection(_connectionString))
        {
            conn.Open();
            string dateStr = $"{year}-{month:D2}-{day:D2}";
            string sql = "SELECT COUNT(*) FROM cash_orders WHERE date(date) = date(@dateStr)";
            using (var cmd = new SQLiteCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@dateStr", dateStr);
                return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
            }
        }
    }

    // Считает количество уникальных дней с операциями с начала года до указанной даты
    public int GetRealPagesCountBeforeDate(int year, int month, int day)
    {
        using (var conn = new SQLiteConnection(_connectionString))
        {
            conn.Open();
            string targetDateStr = $"{year}-{month:D2}-{day:D2}";

            // Считаем только те дни, когда реально были записи в кассе
            string sql = @"
            SELECT COUNT(DISTINCT date(date)) 
            FROM cash_orders 
            WHERE date(date) < date(@targetDate) 
              AND date(date) >= date(@startOfYear)";

            using (var cmd = new SQLiteCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@targetDate", targetDateStr);
                cmd.Parameters.AddWithValue("@startOfYear", $"{year}-01-01");
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }
    }
}