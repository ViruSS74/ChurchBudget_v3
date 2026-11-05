using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ChurchBudget_v3_WPF.Models
{
    public class DocExpenseItemUI : INotifyPropertyChanged
    {
        private decimal _amount;
        private string _checkNumber;
        private string _basis;
        private string _receiptPath;

        public int CategoryId { get; set; }
        public string CategoryName { get; set; }

        public decimal Amount
        {
            get => _amount;
            set { if (_amount != value) { _amount = value; OnPropertyChanged(); } }
        }

        public string CheckNumber
        {
            get => _checkNumber;
            set { if (_checkNumber != value) { _checkNumber = value; OnPropertyChanged(); } }
        }

        public string Basis
        {
            get => _basis;
            set { if (_basis != value) { _basis = value; OnPropertyChanged(); } }
        }

        // Хранит имя файла чека (например, "photo_2026.png")
        public string ReceiptPath
        {
            get => _receiptPath;
            set { if (_receiptPath != value) { _receiptPath = value; OnPropertyChanged(); } }
        }

        // Вспомогательное свойство: возвращает true, если к строке прикреплен чек
        public bool HasReceipt => !string.IsNullOrEmpty(ReceiptPath);

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            if (propertyName == nameof(ReceiptPath)) OnPropertyChanged(nameof(HasReceipt));
        }
    }
}
