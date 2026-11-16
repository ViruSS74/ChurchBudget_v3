using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Navigation;

namespace ChurchBudget_v3_WPF.Views
{
    // имя класса строго совпадает с x:Class="ChurchBudget_v3_WPF.Views.AboutWindow"
    public partial class AboutWindow : Window
    {
        public AboutWindow()
        {
            InitializeComponent();
            // 1. Сначала пробуем прочитать актуальную версию из файла version.txt в папке программы
            string versionStr = "";
            string verFilePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "version.txt");

            if (System.IO.File.Exists(verFilePath))
            {
                versionStr = System.IO.File.ReadAllText(verFilePath).Trim();
            }

            // 2. Если файла нет — берем из свойств сборки AssemblyInfo.cs
            if (string.IsNullOrEmpty(versionStr))
            {
                var ver = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
                versionStr = $"{ver.Major}.{ver.Minor}.{ver.Build}";
            }

            if (txtAboutVersion != null)
            {
                txtAboutVersion.Text = $"Версия {versionStr} ({DateTime.Now.Year} год)";
            }


        }

        // Клик по ссылке почты: автоматически открывает почту Windows
        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
                e.Handled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось открыть почтовое приложение: {ex.Message}", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        // Перетаскивание окна мышкой за верхнюю шапку
        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        // Метод закрытия окна (работает для крестика и для кнопки Ok)
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
