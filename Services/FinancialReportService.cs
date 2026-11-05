using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;

namespace ChurchBudget
{
    public class FinancialReport
    {
        private string _connectionString;

        public FinancialReport(string connectionString)
        {
            if (!string.IsNullOrEmpty(connectionString) && !connectionString.Contains("="))
            {
                _connectionString = "Data Source=" + connectionString;
            }
            else
            {
                _connectionString = connectionString;
            }
        }

        public decimal GetOpeningBalanceForPeriod(int orgId, DateTime periodStart)
        {
            if (periodStart.Month == 1 && periodStart.Day == 1)
            {
                decimal? manualBalance = GetManualOpeningBalance(orgId, periodStart);
                if (manualBalance.HasValue)
                    return manualBalance.Value;
            }
            return CalculateOpeningBalanceFromHistory(orgId, periodStart);
        }

        private decimal? GetManualOpeningBalance(int orgId, DateTime periodStart)
        {
            try
            {
                using (var conn = new SQLiteConnection(_connectionString))
                {
                    conn.Open();
                    string sql = @"
                        SELECT amount FROM opening_balances 
                        WHERE organization_id = @orgId AND balance_date <= @periodStart 
                        ORDER BY balance_date DESC LIMIT 1";
                    using (var cmd = new SQLiteCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@orgId", orgId);
                        cmd.Parameters.AddWithValue("@periodStart", periodStart.ToString("yyyy-MM-dd"));
                        var result = cmd.ExecuteScalar();
                        return result != null && result != DBNull.Value ? Convert.ToDecimal(result) : (decimal?)null;
                    }
                }
            }
            catch { }
            return null;
        }

        private decimal CalculateOpeningBalanceFromHistory(int orgId, DateTime periodStart)
        {
            int year = periodStart.Year;
            DateTime startOfYear = new DateTime(year, 1, 1);
            decimal openingBalance = 0;
            decimal? manualStart = GetManualOpeningBalance(orgId, startOfYear);
            if (manualStart.HasValue)
            {
                openingBalance = manualStart.Value;
            }
            decimal incomeBefore = 0;
            decimal expenseBefore = 0;

            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();
                string sqlIncome = @"
                    SELECT COALESCE(SUM(ii.amount), 0)
                    FROM income_items ii
                    INNER JOIN income_docs id ON ii.doc_id = id.id
                    WHERE id.date >= @startDate AND id.date < @beforeDate";
                using (var cmd = new SQLiteCommand(sqlIncome, conn))
                {
                    cmd.Parameters.AddWithValue("@startDate", startOfYear.ToString("yyyy-MM-dd"));
                    cmd.Parameters.AddWithValue("@beforeDate", periodStart.ToString("yyyy-MM-dd"));
                    var result = cmd.ExecuteScalar();
                    incomeBefore = result != null && result != DBNull.Value ? Convert.ToDecimal(result) : 0;
                }

                string sqlExpense = @"
                    SELECT COALESCE(SUM(ei.amount), 0)
                    FROM expense_items ei
                    INNER JOIN expense_docs ed ON ei.doc_id = ed.id
                    WHERE ed.date >= @startDate AND ed.date < @beforeDate";
                using (var cmd = new SQLiteCommand(sqlExpense, conn))
                {
                    cmd.Parameters.AddWithValue("@startDate", startOfYear.ToString("yyyy-MM-dd"));
                    cmd.Parameters.AddWithValue("@beforeDate", periodStart.ToString("yyyy-MM-dd"));
                    var result = cmd.ExecuteScalar();
                    expenseBefore = result != null && result != DBNull.Value ? Convert.ToDecimal(result) : 0;
                }
            }
            return openingBalance + incomeBefore - expenseBefore;
        }

        public Dictionary<string, decimal> GetIncomeByReportCode(DateTime start, DateTime end)
        {
            var result = new Dictionary<string, decimal>();
            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();
                // ИСПРАВЛЕНО: связываем ii.category с ic.name, так как в базе хранится текст, а не ID
                string sql = @"SELECT ic.report_code, COALESCE(SUM(ii.amount), 0) as total 
                       FROM income_items ii 
                       INNER JOIN income_docs id ON ii.doc_id = id.id 
                       INNER JOIN income_categories ic ON ii.category = ic.name 
                       WHERE id.date >= @start AND id.date <= @end AND ic.report_code IS NOT NULL 
                       GROUP BY ic.report_code";
                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    var pStart = cmd.Parameters.Add("@start", DbType.String);
                    pStart.Value = start.ToString("yyyy-MM-dd");

                    var pEnd = cmd.Parameters.Add("@end", DbType.String);
                    pEnd.Value = end.ToString("yyyy-MM-dd");

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string code = reader["report_code"].ToString();
                            decimal amount = Convert.ToDecimal(reader["total"]);
                            result[code] = amount;
                        }
                    }
                }
            }
            return result;
        }

        public Dictionary<string, decimal> GetExpenseByReportCode(DateTime start, DateTime end)
        {
            var result = new Dictionary<string, decimal>();
            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();
                // ИСПРАВЛЕНО: связываем ei.category с ec.name, так как в базе расходов тоже хранится текст
                string sql = @"SELECT ec.report_code, COALESCE(SUM(ei.amount), 0) as total 
                       FROM expense_items ei 
                       INNER JOIN expense_docs ed ON ei.doc_id = ed.id 
                       INNER JOIN expense_categories ec ON ei.category = ec.name 
                       WHERE ed.date >= @start AND ed.date <= @end AND ec.report_code IS NOT NULL 
                       GROUP BY ec.report_code";
                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    var pStart = cmd.Parameters.Add("@start", DbType.String);
                    pStart.Value = start.ToString("yyyy-MM-dd");

                    var pEnd = cmd.Parameters.Add("@end", DbType.String);
                    pEnd.Value = end.ToString("yyyy-MM-dd");

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string code = reader["report_code"].ToString();
                            decimal amount = Convert.ToDecimal(reader["total"]);
                            result[code] = amount;
                        }
                    }
                }
            }
            return result;
        }

        public Dictionary<string, string> GetOrganizationInfo(int orgId)
        {
            var info = new Dictionary<string, string>();
            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();
                string sql = "SELECT name, location, deanery, diocese FROM organizations WHERE id = @id";
                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@id", orgId);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            info["Name"] = reader["name"].ToString();
                            info["Location"] = reader["location"].ToString();
                            info["Blagochinie"] = reader["deanery"].ToString();
                            info["Diocese"] = reader["diocese"].ToString();
                        }
                    }
                }
            }
            return info;
        }

        public Dictionary<string, string> GetSignatures(int orgId)
        {
            var signatures = new Dictionary<string, string>();
            string rectorName = "";
            string treasurerName = "";
            string chairmanName = "";
            string auditorName = "";
            string sqlStaff = "SELECT role, last_name, first_name, middle_name FROM personal";
            try
            {
                using (var conn = new SQLiteConnection(_connectionString))
                {
                    conn.Open();
                    using (var cmdStaff = new SQLiteCommand(sqlStaff, conn))
                    {
                        using (var reader = cmdStaff.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string role = reader["role"] != DBNull.Value ? reader["role"].ToString().ToLower() : "";
                                string lName = reader["last_name"] != DBNull.Value ? reader["last_name"].ToString() : "";
                                string fName = reader["first_name"] != DBNull.Value ? reader["first_name"].ToString() : "";
                                string mName = reader["middle_name"] != DBNull.Value ? reader["middle_name"].ToString() : "";
                                string initFirst = fName.Length > 0 ? fName.Substring(0, 1) + "." : "";
                                string initMiddle = mName.Length > 0 ? mName.Substring(0, 1) + "." : "";
                                string shortName = string.Format("{0}{1} {2}", initFirst, initMiddle, lName).Trim();
                                if (string.IsNullOrEmpty(shortName)) continue; if (role.Contains("настоятел")) rectorName = shortName; if (role.Contains("казначей")) treasurerName = shortName; if (role.Contains("председатель")) chairmanName = shortName; if (role.Contains("ревизион") || role.Contains("ревизор") || role.Contains("комисси")) auditorName = shortName;
                            }
                        }
                    }
                }
            }
            catch 
            { 
            }
            if (string.IsNullOrEmpty(rectorName)) rectorName = "";
            if (string.IsNullOrEmpty(treasurerName)) treasurerName = "";
            if (string.IsNullOrEmpty(chairmanName)) chairmanName = rectorName;
            if (string.IsNullOrEmpty(auditorName)) auditorName = treasurerName;
            signatures["Настоятель храма"] = rectorName;
            signatures["Председатель приходского совета"] = chairmanName;
            signatures["Казначей"] = treasurerName;
            signatures["Член ревизионной комиссии"] = auditorName;
            return signatures;
        }
    }
}