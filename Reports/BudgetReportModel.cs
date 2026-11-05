using System;
using System.Collections.Generic;

namespace ChurchBudget_v3_WPF.Reports
{
    public class BudgetReportModel
    {
        public string PeriodText { get; set; } // Например, "январь 2026 г." или "2026 год"
        public string OrganizationName { get; set; }
        public string Location { get; set; }
        public string Deanery { get; set; } // Благочиние
        public string Diocese { get; set; }  // Епархия

        public decimal OpeningBalance { get; set; }
        public decimal ClosingBalance { get; set; }

        // Сюда загрузим данные из GetIncomeByReportCode / GetExpenseByReportCode
        public Dictionary<string, decimal> IncomeByCode { get; set; } = new Dictionary<string, decimal>();
        public Dictionary<string, decimal> ExpenseByCode { get; set; } = new Dictionary<string, decimal>();

        // Подписи
        public Dictionary<string, string> Signatures { get; set; } = new Dictionary<string, string>();
    }
}
