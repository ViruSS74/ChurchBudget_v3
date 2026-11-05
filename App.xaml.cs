using ChurchBudget_v3_WPF.Views;
using ChurchBudget_v3_WPF.Services;
using QuestPDF.Infrastructure;
using System;
using System.Windows;
using System.IO;
using System.Runtime.InteropServices; // ОБЯЗАТЕЛЬНО: для импорта системных функций Win32 API

[assembly: System.Runtime.CompilerServices.SuppressIldasmAttribute()]

namespace ChurchBudget_v3_WPF
{
    public partial class App : Application
    {
        // Подключаем функцию из системной библиотеки user32.dll для принудичного отключения DPI-масштабирования Windows
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetProcessDPIAware();

        protected override void OnStartup(StartupEventArgs e)
        {
            // МАГИЯ ГЛОБАЛЬНОГО DPI: Отключаем автоматическое системное раздувание окон!
            // Теперь абсолютно все окна (Главное, Расходы, Доходы, Лицензии) откроются в масштабе 100% пиксель-в-пиксель
            try
            {
                SetProcessDPIAware();
            }
            catch { }

            base.OnStartup(e);

            string dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "church.db");

            // 1. Проверяем полную лицензию в реестре
            if (!LicenseManager.CheckLicenseInRegistry(dbPath))
            {
                // 2. Лицензии нет — проверяем триал (31 день от даты первого ордера)
                DateTime trialStart;
                int daysLeft;

                if (!LicenseManager.CheckTrialPeriod(dbPath, out trialStart, out daysLeft))
                {
                    // 3. Триал истек — принудительно требуем мастер-ключ активации
                    var licWindow = new LicenseWindow(dbPath);

                    if (licWindow.ShowDialog() != true)
                    {
                        Environment.Exit(0); // Гасим, если закрыли окно
                        return;
                    }
                }
            }

            // РУБЕЖИ ПРОЙДЕНЫ — ЗАПУСКАЕМ ПРОГРАММУ
            QuestPDF.Settings.License = LicenseType.Community;

            MainWindow mainWin = new MainWindow();
            mainWin.Show();
        }
    }
}
