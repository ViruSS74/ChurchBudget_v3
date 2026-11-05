using ChurchBudget_v3_WPF.Reports;
using ChurchBudget_v3_WPF.Services;
using Microsoft.Win32;
using System;
using System.Windows;
using System.Windows.Input;

namespace ChurchBudget_v3_WPF.Views
{
    public partial class LicenseWindow : Window
    {
        private readonly string _dbPath;

        public LicenseWindow(string dbPath)
        {
            InitializeComponent();
            _dbPath = dbPath;
            LoadChurchData();

            // ЕСЛИ ВЛАДЕЛЕЦ НЕ ЗАДАН (СТАРТ ПРОГРАММЫ), ЦЕНТРИРУЕМ ПО ЭКРАНУ:
            if (this.Owner == null)
            {
                this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            }
        }

        private void LoadChurchData()
        {
            var (name, location) = LicenseManager.GetChurchDataFromDb(_dbPath);
            if (!string.IsNullOrEmpty(name))
            {
                txtChurchName.Text = name;
                txtLocation.Text = location;
                lblStatusText.Text = "Программа работает в режиме проверки лицензионного ключа.";
            }
            else
            {
                MessageBox.Show("Данные прихода не найдены в базе данных!\nСначала заполните карточку вашей организации в настройках.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                this.Close();
            }
        }

        private void btnActivate_Click(object sender, RoutedEventArgs e)
        {
            string church = txtChurchName.Text.Trim();
            string loc = txtLocation.Text.Trim();
            string key = txtLicenseKey.Text.Trim().ToUpper(); // Возвращаем приведение к верхнему регистру

            if (string.IsNullOrEmpty(key))
            {
                MessageBox.Show("Введите лицензионный ключ активации!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Вычисляем эталонный ключ для этой конкретной организации
            string validKey = LicenseManager.GenerateKey(church, loc);

            if (key == validKey)
            {
                try
                {
                    // Создаем персональную изолированную подпапку прихода в реестре
                    string subKeyName = $@"Software\ChurchBudgetSettings\{church}_{loc}".Replace('\\', '_').Replace('/', '_');

                    using (RegistryKey regKey = Registry.CurrentUser.CreateSubKey(subKeyName))
                    {
                        regKey.SetValue("LicensedChurchName", church);
                        regKey.SetValue("LicensedLocation", loc);
                        regKey.SetValue("LicenseKey", key);
                        regKey.SetValue("HardwareHash", LicenseManager.GetHardwareId()); // Намертво привязываем к процессору!
                    }

                    MessageBox.Show("Программа успешно активирована для данного прихода на этом компьютере!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    this.DialogResult = true;
                    this.Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка записи лицензии в реестр: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("Введен неверный лицензионный ключ!\nПроверьте данные организации или обратитесь к разработчику.", "Ошибка активации", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}
