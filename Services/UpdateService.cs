using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;

namespace ChurchBudget_v3_WPF.Services
{
    public class UpdateService
    {
        // Ссылка на папку на вашем сервере, где лежат обновления
        private const string UpdateUrl = "https://raw.githubusercontent.com/ViruSS74/church-updates/main/"; private readonly string _currentVersion;

        public UpdateService(string currentVersion)
        {
            _currentVersion = currentVersion;
        }

        public async Task CheckAndRunUpdateAsync()
        {
            try
            {
                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(15);

                    // 1. Скачиваем текстовый файл с актуальной версией на сервере
                    string remoteVersion = await client.GetStringAsync(UpdateUrl + "version.txt");
                    remoteVersion = remoteVersion.Trim();

                    // 2. Сравниваем версии. Если на сервере версия новее — запускаем процесс
                    if (remoteVersion != _currentVersion)
                    {
                        var result = MessageBox.Show(
                            $"Доступна новая версия программы ({remoteVersion}).\nЖелаете обновиться?\n\n(Ваша текущая версия: {_currentVersion})",
                            "Обновление программы",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Question);

                        if (result == MessageBoxResult.Yes)
                        {
                            string appDir = AppDomain.CurrentDomain.BaseDirectory;
                            string zipPath = Path.Combine(appDir, "update.zip");
                            string updaterPath = Path.Combine(appDir, "ChurchBudget.Updater.exe");

                            // Проверяем, на месте ли наш мини-апдейтер из Шага 1
                            if (!File.Exists(updaterPath))
                            {
                                MessageBox.Show("Не найден системный файл обновлений Updater.exe. Обновление невозможно.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                                return;
                            }

                            // 3. Скачиваем сам zip-архив с кодом
                            byte[] archiveBytes = await client.GetByteArrayAsync(UpdateUrl + "update.zip");
                            File.WriteAllBytes(zipPath, archiveBytes);

                            MessageBox.Show("Файлы успешно скачаны. Программа будет перезапущена для установки обновления.", "Обновление", MessageBoxButton.OK, MessageBoxImage.Information);

                            // 4. Запускаем внешний Updater.exe и тушим основную программу
                            System.Diagnostics.Process.Start(updaterPath);
                            Application.Current.Shutdown();
                        }
                    }
                    else
                    {
                        MessageBox.Show("У вас установлена самая свежая версия программы!", "Обновление", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось связаться с сервером обновлений: {ex.Message}", "Ошибка обновления", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}
