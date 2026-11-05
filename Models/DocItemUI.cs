using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ChurchBudget_v3_WPF.Models
{
    // Реализуем INotifyPropertyChanged, чтобы при изменении суммы в ячейке Итог пересчитывался мгновенно
    public class DocItemUI : INotifyPropertyChanged
    {
        private decimal _amount;
        private string _comment;

        public int CategoryId { get; set; }
        public string CategoryName { get; set; }

        public decimal Amount
        {
            get => _amount;
            set
            {
                if (_amount != value)
                {
                    _amount = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Comment
        {
            get => _comment;
            set
            {
                if (_comment != value)
                {
                    _comment = value;
                    OnPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}