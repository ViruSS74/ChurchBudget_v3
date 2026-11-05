using System;
using System.IO;
using System.Management;
using System.Security.Cryptography;
using System.Text;
using System.Data.SQLite;
using Microsoft.Win32;

namespace ChurchBudget_v3_WPF.Services
{
    public static class LicenseManager
    {
        // СЕКРЕТНАЯ ХЭШ-СОЛЬ (Измените эти буквы на любые свои, чтобы никто не подобрал алгоритм)
        private const string SecretSalt = "CHURCH_REMASTER_SECRET_SALT_2026";

        // 1. ПОЛУЧЕНИЕ УНИКАЛЬНОГО ID ЖЕЛЕЗА ПК (Hardware ID)
        public static string GetHardwareId()
        {
            try
            {
                string cpuId = string.Empty;
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("Select ProcessorId From Win32_Processor"))
                {
                    foreach (ManagementObject mo in searcher.Get())
                    {
                        cpuId = mo["ProcessorId"]?.ToString();
                        if (!string.IsNullOrEmpty(cpuId)) break;
                    }
                }

                if (string.IsNullOrEmpty(cpuId))
                {
                    using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("Select SerialNumber From Win32_BaseBoard"))
                    {
                        foreach (ManagementObject mo in searcher.Get())
                        {
                            cpuId = mo["SerialNumber"]?.ToString();
                            if (!string.IsNullOrEmpty(cpuId)) break;
                        }
                    }
                }

                using (MD5 md5 = MD5.Create())
                {
                    byte[] inputBytes = Encoding.UTF8.GetBytes(cpuId ?? "ChurchBudgetFallbackID");
                    byte[] hashBytes = md5.ComputeHash(inputBytes);
                    return BitConverter.ToString(hashBytes, 0, 4).Replace("-", "").ToUpper();
                }
            }
            catch { return "BASEPCID"; }
        }

        // 2. АЛГОРИТМИЧЕСКАЯ ГЕНЕРАЦИЯ КЛЮЧА НА ОСНОВЕ НАЗВАНИЯ ХРАМА
        public static string GenerateKey(string churchName, string location)
        {
            // Математически связываем имя прихода, адрес и нашу секретную соль
            string rawData = $"{churchName.Trim()}_{location.Trim()}_{SecretSalt}";

            using (MD5 md5 = MD5.Create())
            {
                byte[] inputBytes = Encoding.UTF8.GetBytes(rawData);
                byte[] hashBytes = md5.ComputeHash(inputBytes);
                // Возвращаем уникальный 8-значный ключ для этого прихода
                return BitConverter.ToString(hashBytes, 4, 4).Replace("-", "").ToUpper();
            }
        }

        // 3. БЕЗОПАСНАЯ ПРОВЕРКА ЛИЦЕНЗИИ В РЕЕСТРЕ
        public static bool CheckLicenseInRegistry(string dbPath)
        {
            try
            {
                var (dbChurch, dbLoc) = GetChurchDataFromDb(dbPath);
                if (string.IsNullOrEmpty(dbChurch)) return false;

                // Ищем лицензию в персональной подпапке конкретного прихода
                string subKeyName = $@"Software\ChurchBudgetSettings\{dbChurch.Trim()}_{dbLoc.Trim()}".Replace('\\', '_').Replace('/', '_');

                using (RegistryKey regKey = Registry.CurrentUser.OpenSubKey(subKeyName))
                {
                    if (regKey != null)
                    {
                        string regKeyStr = regKey.GetValue("LicenseKey")?.ToString();
                        string regHwHash = regKey.GetValue("HardwareHash")?.ToString();

                        if (string.IsNullOrEmpty(regKeyStr) || string.IsNullOrEmpty(regHwHash))
                            return false;

                        string currentHwId = GetHardwareId();

                        // Проверяем: валиден ли сохраненный ключ под имя базы И совпадает ли процессор ПК
                        if (regKeyStr == GenerateKey(dbChurch, dbLoc) && regHwHash == currentHwId)
                        {
                            return true; // Полная лицензия подтверждена!
                        }
                    }
                }
            }
            catch { return false; }
            return false;
        }

        // 4. ЧТЕНИЕ ДАННЫХ ОРГАНИЗАЦИИ ИЗ СТРУКТУРЫ ВАШЕЙ БАЗЫ
        public static (string name, string location) GetChurchDataFromDb(string dbPath)
        {
            string name = string.Empty;
            string location = string.Empty;
            try
            {
                if (File.Exists(dbPath))
                {
                    using (var conn = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
                    {
                        conn.Open();
                        string sql = "SELECT name, location FROM organizations LIMIT 1;";
                        using (var cmd = new SQLiteCommand(sql, conn))
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                name = reader["name"]?.ToString();
                                location = reader["location"]?.ToString();
                            }
                        }
                    }
                }
            }
            catch { }
            return (name, location);
        }

        // 5. КОНТРОЛЬ ТРИАЛА: 31 день от даты самого первого ордера
        public static bool CheckTrialPeriod(string dbPath, out DateTime trialStart, out int daysLeft)
        {
            trialStart = DateTime.Now.Date;
            daysLeft = 31;
            try
            {
                if (!File.Exists(dbPath)) return false;

                using (var conn = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
                {
                    conn.Open();

                    string sql = "SELECT MIN(date) FROM cash_orders WHERE date IS NOT NULL AND date != '';";
                    object firstDocResult = null;
                    using (var cmd = new SQLiteCommand(sql, conn)) { firstDocResult = cmd.ExecuteScalar(); }

                    if (firstDocResult != null && firstDocResult != DBNull.Value && !string.IsNullOrEmpty(firstDocResult.ToString()))
                    {
                        string dateStr = firstDocResult.ToString().Trim();
                        if (DateTime.TryParseExact(dateStr, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out DateTime parsedDate))
                        {
                            trialStart = parsedDate.Date;
                        }
                        else
                        {
                            DateTime.TryParse(dateStr, out trialStart);
                        }
                    }

                    int elapsedDays = (DateTime.Now.Date - trialStart.Date).Days;
                    daysLeft = 31 - elapsedDays;

                    return daysLeft >= 0;
                }
            }
            catch { return false; }
        }
    }
}
