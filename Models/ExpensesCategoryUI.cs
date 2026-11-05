using System.Collections.Generic; // Не забудьте добавить этот string в самый верх файла
using System.Windows;
using static ChurchBudget_v3_WPF.Services.ExpensesCategoryService;

namespace ChurchBudget_v3_WPF.Models
{
    public class ExpensesCategoryUI
    {
        // Ссылка на оригинальный объект с данными из БД 
        public ExpensesCategoryItem DbItem { get; set; }

        // Проброс свойств для DataGrid (XAML) 
        public int id => DbItem.Id;
        public string name => DbItem.Name;
        public int? parent_id => DbItem.ParentId;

        // Связываем новые свойства с объектом из БД 
        public string description => DbItem.Description;
        public string report_code => DbItem.ReportCode;
        public bool is_active => DbItem.IsActive;

        // Свойства иерархии (сдвиг для подкатегорий больше не нужен, но можно оставить) 
        public int Level { get; set; }
        public Thickness MarginLevel => new Thickness(Level * 20, 0, 0, 0);

        // ВАЖНО: Добавьте это свойство для хранения подкатегорий!
        public List<ExpensesCategoryUI> Children { get; set; } = new List<ExpensesCategoryUI>();
    }
}
