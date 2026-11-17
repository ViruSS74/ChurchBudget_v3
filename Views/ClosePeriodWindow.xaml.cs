using ChurchBudget_v3_WPF.Services;
using System;
using System.IO;
using System.Windows;
using System.Windows.Input;

namespace ChurchBudget_v3_WPF.Views
{
    public partial class ClosePeriodWindow : Window
    {
        private readonly ListOfDocsService _service;

        public ClosePeriodWindow()
        {
            InitializeComponent();
            string dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "church.db");
            _service = new ListOfDocsService($"Data Source={dbPath};Version=3;");
            LoadCurrentSetting();
        }

        private void LoadCurrentSetting()
        {
            DateTime? currentClosed = _service.GetClosedPeriodDate();
            if (currentClosed.HasValue)
            {
                lblCurrentStatus.Text = $"Текущий статус: период закрыт по {currentClosed.Value:dd.MM.yyyy} г.";
                lblCurrentStatus.Foreground = System.Windows.Media.Brushes.DarkRed;
                dpCloseDate.SelectedDate = currentClosed.Value;
            }
            else
            {
                lblCurrentStatus.Text = "Текущий статус: запрет не установлен (все периоды открыты)";
                lblCurrentStatus.Foreground = System.Windows.Media.Brushes.DarkGreen;
                // По умолчанию предлагаем 31 декабря прошлого года
                dpCloseDate.SelectedDate = new DateTime(DateTime.Now.Year - 1, 12, 31);
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (dpCloseDate.SelectedDate == null)
            {
                MessageBox.Show("Пожалуйста, выберите дату закрытия периода.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            DateTime selectedDate = dpCloseDate.SelectedDate.Value.Date;
            _service.SetClosedPeriodDate(selectedDate);
            LoadCurrentSetting();
            MessageBox.Show($"Период по {selectedDate:dd.MM.yyyy} г. успешно закрыт.\nСоздание, изменение и удаление документов за этот период заблокированы.", "Период закрыт", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            _service.SetClosedPeriodDate(null);
            LoadCurrentSetting();
            MessageBox.Show("Запрет редактирования снят. Все периоды снова открыты.", "Запрет снят", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left) this.DragMove();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
