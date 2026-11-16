using ChurchBudget_v3_WPF.Services;
using ChurchBudget_v3_WPF.Views;
using QuestPDF.Infrastructure;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;

[assembly: System.Runtime.CompilerServices.SuppressIldasmAttribute()]

namespace ChurchBudget_v3_WPF
{
    public partial class App : Application
    {
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetProcessDPIAware();

        protected override void OnStartup(StartupEventArgs e)
        {
            try
            {
                SetProcessDPIAware();
            }
            catch { }

            base.OnStartup(e);

            // 1. СНАЧАЛА проверяем и разворачиваем вшитую чистую БД (до любых проверок лицензии!)
            string dataDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
            string dbPath = Path.Combine(dataDir, "church.db");

            if (!File.Exists(dbPath))
            {
                if (!Directory.Exists(dataDir))
                    Directory.CreateDirectory(dataDir);

                var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                string resourceName = Array.Find(assembly.GetManifestResourceNames(),
                    name => name.EndsWith("empty_template.db", StringComparison.OrdinalIgnoreCase));

                if (!string.IsNullOrEmpty(resourceName))
                {
                    using (var stream = assembly.GetManifestResourceStream(resourceName))
                    using (var fileStream = new FileStream(dbPath, FileMode.Create))
                    {
                        stream.CopyTo(fileStream);
                    }
                }
                else
                {
                    MessageBox.Show("Внимание: вшитый шаблон empty_template.db не найден в ресурсах программы! Проверьте свойство 'Внедренный ресурс'.", "Ошибка инициализации БД", MessageBoxButton.OK, MessageBoxImage.Error);
                    Environment.Exit(0);
                    return;
                }
            }

            // 2. ТЕПЕРЬ, когда база гарантированно существует, проверяем лицензию и триал
            if (!LicenseManager.CheckLicenseInRegistry(dbPath))
            {
                DateTime trialStart;
                int daysLeft;

                if (!LicenseManager.CheckTrialPeriod(dbPath, out trialStart, out daysLeft))
                {
                    this.ShutdownMode = ShutdownMode.OnExplicitShutdown;
                    var licWindow = new LicenseWindow(dbPath);
                    bool isActivated = false;

                    try
                    {
                        isActivated = (licWindow.ShowDialog() == true);
                    }
                    catch (InvalidOperationException)
                    {
                        // Окно закрылось в конструкторе после предупреждения о незаполненной организации
                    }

                    if (!isActivated && !LicenseManager.CheckTrialPeriod(dbPath, out trialStart, out daysLeft))
                    {
                        Environment.Exit(0);
                        return;
                    }

                    this.ShutdownMode = ShutdownMode.OnMainWindowClose;
                }
            }

            // 3. РУБЕЖИ ПРОЙДЕНЫ — ЗАПУСКАЕМ ПРОГРАММУ
            QuestPDF.Settings.License = LicenseType.Community;

            MainWindow mainWin = new MainWindow();
            mainWin.Show();
        }
    }
}
