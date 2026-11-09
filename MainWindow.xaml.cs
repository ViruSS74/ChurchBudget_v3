using ChurchBudget_v3_WPF.Services;
using System;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Shapes;

namespace ChurchBudget_v3_WPF
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            // 1. Путь к базе данных SQLite (как у вас и было)
            string dbPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "church.db");
            string connString = $"Data Source={dbPath};Version=3;";

            // 2. Инициализируем наш НОВЫЙ системный сервис обновлений
            var dbService = new ChurchBudget_v3_WPF.Services.DBService(connString);

            // 3. СРАЗУ запускаем проверку и обновление справочников клиента
            dbService.RunDatabaseMigrations();

            // 4. Дальше идет инициализация ваших обычных рабочих сервисов...
            // _service = new CashbookService(connString);

            // Подписываемся на событие полной загрузки окна
            this.Loaded += MainWindow_Loaded;

            // Подписываемся на событие закрытия окна для автоматического бэкапа
            this.Closing += MainWindow_Closing;
        }

        // Этот метод сработает автоматически, когда окно полностью появится на экране
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            CheckDatabaseStatus();
        }

        // =========================================================================
        // ЛОГИКА РАБОТЫ С БАЗОЙ ДАННЫХ И АРХИВАЦИЕЙ
        // =========================================================================
        private void CheckDatabaseStatus()
        {
            // Формируем чистый абсолютный путь к файлу
            string dbPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "church.db");

            if (File.Exists(dbPath))
            {
                tblDBStatus.Text = "БД: Подключена";
                tblDBStatus.Foreground = System.Windows.Media.Brushes.DarkGreen;
                LoadOrganizationName(dbPath);
            }
            else
            {
                tblDBStatus.Text = "БД: Файл не найден! Ожидалось: " + dbPath;
                tblDBStatus.Foreground = System.Windows.Media.Brushes.Red;
                tblOrgName.Text = "Файл church.db отсутствует в папке Data";
            }
        }
        private void LoadOrganizationName(string dbPath)
        {
            try
            {
                using (var conn = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
                {
                    conn.Open();

                    // Запрос: получаем имя из таблицы 'organizations'
                    string sql = "SELECT name FROM organizations LIMIT 1;";

                    using (var cmd = new SQLiteCommand(sql, conn))
                    {
                        var result = cmd.ExecuteScalar();

                        if (result != null && result != DBNull.Value && !string.IsNullOrWhiteSpace(result.ToString()))
                        {
                            tblOrgName.Text = result.ToString();
                        }
                        else
                        {
                            tblOrgName.Text = "Таблица организаций пуста в БД";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Если что-то пойдет не так, мы увидим точную ошибку на экране
                tblOrgName.Text = "Ошибка чтения структуры: " + ex.Message;
            }
            // Гарантированно включаем видимость текстового блока на экране
            tblOrgName.Visibility = Visibility.Visible;
        }
        private bool CreateBackup()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string sourceFile = System.IO.Path.Combine(baseDir, "Data\\church.db");
            string archiveFolder = System.IO.Path.Combine(baseDir, "Data\\Archive");

            try
            {
                if (!File.Exists(sourceFile))
                    return false;

                if (!Directory.Exists(archiveFolder))
                    Directory.CreateDirectory(archiveFolder);

                string destFile = System.IO.Path.Combine(archiveFolder, "church_backup_" + DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + ".db");

                // --- ИСПОЛЬЗУЕМ СЕЙФОВЫЙ БЭКАП SQLITE ---
                // Открываем подключение к рабочей БД и к будущему файлу бэкапа
                using (var sourceConnection = new SQLiteConnection($"Data Source={sourceFile};Version=3;"))
                using (var backupConnection = new SQLiteConnection($"Data Source={destFile};Version=3;"))
                {
                    sourceConnection.Open();
                    backupConnection.Open();

                    // Вызываем встроенное API резервного копирования
                    sourceConnection.BackupDatabase(backupConnection, "main", "main", -1, null, 0);
                }
                // ----------------------------------------

                // Очистка старых бэкапов (исправлено ранжирование по дате)
                var filesToDelete = new DirectoryInfo(archiveFolder)
                    .GetFiles("*.db")
                    .OrderByDescending(f => f.LastWriteTime) // Надежнее ориентироваться на время изменения
                    .Skip(10)
                    .ToList();

                foreach (var file in filesToDelete)
                {
                    file.Delete();
                }

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка бэкапа: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        // =========================================================================
        // ОБРАБОТЧИКИ ИНТЕРФЕЙСА ОКНА
        // =========================================================================
        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e) { if (e.ChangedButton == MouseButton.Left) this.DragMove(); }
        private void MinimizeButton_Click(object sender, RoutedEventArgs e) { this.WindowState = WindowState.Minimized; }
        private void CloseButton_Click(object sender, RoutedEventArgs e) { this.Close(); }
        private void ExitToolStripMenuItem_Click(object sender, RoutedEventArgs e) { this.Close(); }
        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show(
                "Выйти?\nБудет создан бэкап БД.",
                "Выход",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes) { CreateBackup(); }
            else { e.Cancel = true; }
        }

        // =========================================================================
        // КОД ДЛЯ ПУНКТОВ МЕНЮ
        // =========================================================================
        // === Документы ===
        // === Новый доход ===
        private void NewIncomeToolStripMenuItem_Click(object sender, RoutedEventArgs e)
        {
            // 1. Создаем экземпляр нового окна из папки Views
            Views.NewIncomeWindow incomeWindow = new Views.NewIncomeWindow();
            // 2. Привязываем владельца, чтобы оно открывалось ровно по центру главного окна
            incomeWindow.Owner = this;
            // 3. Открываем окно в модальном режиме
            incomeWindow.ShowDialog();
        }

        // === Новый расход ===
        private void NewExpensesToolStripMenuItem_Click(object sender, RoutedEventArgs e)
        {
            // 1. Создаем экземпляр нового окна из папки Views
            Views.NewExpensesWindows expensesWindow = new Views.NewExpensesWindows();
            // 2. Привязываем владельца, чтобы оно открывалось ровно по центру главного окна
            expensesWindow.Owner = this;
            // 3. Открываем окно в модальном режиме
            expensesWindow.ShowDialog();
        }

        // === Ввод начальных остатков ===
        private void OpeningBalanceToolStripMenuItem_Click(object sender, RoutedEventArgs e)
        {
            // 1. Создаем экземпляр нового окна из папки Views
            Views.OpeningBalanceWindow openbalanceWindow = new Views.OpeningBalanceWindow();
            // 2. Привязываем владельца, чтобы оно открывалось ровно по центру главного окна
            openbalanceWindow.Owner = this;
            // 3. Открываем окно в модальном режиме
            openbalanceWindow.ShowDialog();
        }

        // === Отчеты ===
        // === Список документов ===
        private void ListOfDocsToolStripMenuItem_Click(object sender, RoutedEventArgs e)
        {
            // 1. Создаем экземпляр нового окна из папки Views
            Views.ListOfDocsWindow listofdocsWindow = new Views.ListOfDocsWindow();
            // 2. Привязываем владельца, чтобы оно открывалось ровно по центру главного окна
            listofdocsWindow.Owner = this;
            // 3. Открываем окно в модальном режиме
            listofdocsWindow.ShowDialog();
        }

        // === Кассовая книга ===
        private void CashbookToolStripMenuItem_Click(object sender, RoutedEventArgs e)
        {
            // 1. Создаем экземпляр нового окна из папки Views
            Views.CashBookWindow csWindow = new Views.CashBookWindow();
            // 2. Привязываем владельца, чтобы оно открывалось ровно по центру главного окна
            csWindow.Owner = this;
            // 3. Открываем окно в модальном режиме
            csWindow.ShowDialog();
        }

        // === Финансовый отчет ===
        private void FinanceReportToolStripMenuItem_Click(object sender, RoutedEventArgs e)
        {
            // 1. Создаем экземпляр нового окна из папки Views
            Views.FinanceReportWindow frWindow = new Views.FinanceReportWindow();
            // 2. Привязываем владельца, чтобы оно открывалось ровно по центру главного окна
            frWindow.Owner = this;
            // 3. Открываем окно в модальном режиме
            frWindow.ShowDialog();
        }

        // === Пакетная печать ===
        private void BatchPrintToolStripMenuItem_Click(object sender, RoutedEventArgs e)
        {
            // 1. Создаем экземпляр нового окна из папки Views
            Views.BatchPrintWindow bpWindow = new Views.BatchPrintWindow();
            // 2. Привязываем владельца, чтобы оно открывалось ровно по центру главного окна
            bpWindow.Owner = this;
            // 3. Открываем окно в модальном режиме
            bpWindow.ShowDialog();
        }

        // === Справочники ===
        // === Организации ===
        private void OrganizationDirToolStripMenuItem_Click(object sender, RoutedEventArgs e)
        {
            // 1. Создаем экземпляр нового окна из папки Views
            Views.OrganizationDirWindow orgWindow = new Views.OrganizationDirWindow();
            // 2. Привязываем владельца, чтобы оно открывалось ровно по центру главного окна
            orgWindow.Owner = this;
            // 3. Открываем окно в модальном режиме
            orgWindow.ShowDialog();
        }

        // === Сотрудники ===
        private void EmployeeDirToolStripMenuItem_Click(object sender, RoutedEventArgs e)
        {
            // 1. Создаем экземпляр нового окна из папки Views
            Views.EmployeeDirWindow empWindow = new Views.EmployeeDirWindow();
            // 2. Привязываем владельца, чтобы оно открывалось ровно по центру главного окна
            empWindow.Owner = this;
            // 3. Открываем окно в модальном режиме
            empWindow.ShowDialog();
        }

        // ===  ИД Документы ===
        private void IDDocsDirToolStripMenuItem_Click(object sender, RoutedEventArgs e)
        {
            // 1. Создаем экземпляр нового окна из папки Views
            Views.IDDocsDirWindow iddocsWindow = new Views.IDDocsDirWindow();
            // 2. Привязываем владельца, чтобы оно открывалось ровно по центру главного окна
            iddocsWindow.Owner = this;
            // 3. Открываем окно в модальном режиме
            iddocsWindow.ShowDialog();
        }

        // === Типы ИД документов ===
        private void TypesIDDocsDirToolStripMenuItem_Click(object sender, RoutedEventArgs e)
        {
            // 1. Создаем экземпляр нового окна из папки Views
            Views.TypesIDDocsDirWindow tiddsWindow = new Views.TypesIDDocsDirWindow();
            // 2. Привязываем владельца, чтобы оно открывалось ровно по центру главного окна
            tiddsWindow.Owner = this;
            // 3. Открываем окно в модальном режиме
            tiddsWindow.ShowDialog();
        }

        // === Категория доходов ===
        private void IncomeCatDirToolStripMenuItem_Click(object sender, RoutedEventArgs e)
        {
            // 1. Создаем экземпляр нового окна из папки Views
            Views.IncomeCatDirWindow incatWindow = new Views.IncomeCatDirWindow();
            // 2. Привязываем владельца, чтобы оно открывалось ровно по центру главного окна
            incatWindow.Owner = this;
            // 3. Открываем окно в модальном режиме
            incatWindow.ShowDialog();
        }

        // === Категория расходов ===
        private void ExpensesCatDirToolStripMenuItem_Click(object sender, RoutedEventArgs e)
        {
            // 1. Создаем экземпляр нового окна из папки Views
            Views.ExpensesCatDirWindow expcatWindow = new Views.ExpensesCatDirWindow();
            // 2. Привязываем владельца, чтобы оно открывалось ровно по центру главного окна
            expcatWindow.Owner = this;
            // 3. Открываем окно в модальном режиме
            expcatWindow.ShowDialog();
        }

        // === Сервис ===
        // === Восстановление БД ===
        private void RestoreDBToolStripMenuItem_Click(object sender, RoutedEventArgs e)
        {
            // Используем System.IO для путей, чтобы избежать неоднозначности с фигурами WPF
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string dbPath = System.IO.Path.Combine(baseDir, "Data", "church.db");
            string archiveFolder = System.IO.Path.Combine(baseDir, "Data", "Archive");

            // 1. Вызываем стандартный WPF-диалог выбора файла
            Microsoft.Win32.OpenFileDialog ofd = new Microsoft.Win32.OpenFileDialog();

            // Если папка автоматических архивов существует — стартуем сразу с неё
            if (System.IO.Directory.Exists(archiveFolder))
                ofd.InitialDirectory = archiveFolder;

            ofd.Filter = "SQLite Database (*.db)|*.db";
            ofd.Title = "Выберите резервную копию базы данных для восстановления";

            if (ofd.ShowDialog() == true)
            {
                // 2. Запрашиваем подтверждение у пользователя в стиле WPF MessageBox
                var result = MessageBox.Show(
                    "Вы действительно хотите заменить текущую рабочую базу данных выбранной резервной копией?\n\n" +
                    "Все изменения, внесенные после даты создания этого архива, будут безвозвратно утеряны!",
                    "Предупреждение",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        // Закрываем все соединения с базой данных перед копированием, если это необходимо
                        System.Data.SQLite.SQLiteConnection.ClearAllPools();

                        // 3. Копируем файл из архива поверх текущей рабочей БД
                        System.IO.File.Copy(ofd.FileName, dbPath, true);

                        MessageBox.Show(
                            "База данных успешно восстановлена из резервной копии!\n" +
                            "Для применения изменений программа сейчас автоматически перезапустится.",
                            "Успех",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);

                        // 4. БЕЗОПАСНЫЙ ПЕРЕЗАПУСК WPF ПРИЛОЖЕНИЯ:
                        // Запускаем новый процесс текущего приложения
                        System.Diagnostics.Process.Start(Application.ResourceAssembly.Location);

                        // Полностью и корректно закрываем текущую копию программы
                        Application.Current.Shutdown();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(
                            $"Не удалось восстановить базу данных из-за ошибки:\n{ex.Message}\n\n" +
                            "Убедитесь, что файл базы данных не заблокирован другими программами.",
                            "Ошибка восстановления",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
                    }
                }
            }
        }

        // === Активация программы ===
        private void ActivationToolStripMenuItem_Click(object sender, RoutedEventArgs e)
        {
            // Явно пишем System.IO.Path, чтобы убрать неоднозначность с фигурами WPF,
            // и создаем локальную переменную dbPath прямо внутри этого метода.
            string dbPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "church.db");

            Views.LicenseWindow activeWindow = new Views.LicenseWindow(dbPath);
            activeWindow.Owner = this;
            activeWindow.ShowDialog();
        }

        // === Обновление программы ===
        private async void MenuUpdateToolStripMenuItem_Click(object sender, RoutedEventArgs e)
        {
            // Передаем текущую версию приложения (её можно вынести в константы или читать из сборки)
            string currentAppVersion = "3.1.125";

            var updater = new UpdateService(currentAppVersion);

            // Запускаем асинхронную проверку интернета
            await updater.CheckAndRunUpdateAsync();
        }

        // === Справка ===
        // === Справка по программе ===
        private void HelpToolStripMenuItem_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 1. Собираем точный и надежный путь к файлу справки в корне приложения
                // Использование System.IO.Path защищает от конфликтов с фигурами WPF Shapes
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string helpFilePath = System.IO.Path.Combine(baseDir, "Help.pdf");

                // 2. Проверяем, существует ли файл физически на диске, чтобы программа не упала
                if (System.IO.File.Exists(helpFilePath))
                {
                    // 3. Запускаем файл через системный просмотрщик PDF по умолчанию
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = helpFilePath,
                        UseShellExecute = true // Железно заставляет Windows открыть файл внешней программой
                    });
                }
                else
                {
                    // Если файла нет (например, забыли положить в папку)
                    MessageBox.Show(
                        "Файл справки 'Help.pdf' не найден в корневой папке программы.\n" +
                        "Пожалуйста, убедитесь, что файл руководства пользователя находится в папке с приложением.",
                        "Файл не найден",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Не удалось открыть файл справки по следующей причине:\n{ex.Message}",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        // === О программе ===
        private void AboutBoxToolStripMenuItem_Click(object sender, RoutedEventArgs e)
        {
            // 1. Создаем экземпляр нового окна из папки Views
            Views.AboutWindow aboutWindow = new Views.AboutWindow();
            // 2. Привязываем владельца, чтобы оно открывалось ровно по центру главного окна
            aboutWindow.Owner = this;
            // 3. Открываем окно в модальном режиме
            aboutWindow.ShowDialog();
        }
    }
}
