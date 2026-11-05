using System;
using System.Data;

namespace ChurchBudget_v3_WPF.Reports
{
    public class ExpenseDataModel
    {
        public string OrganizationName { get; set; }
        public string LocationText { get; set; }
        public string DocNumber { get; set; }
        public DateTime DocDate { get; set; }
        public DataTable ItemsTable { get; set; }
        public string TreasurerName { get; set; }
        public bool HasChecks { get; set; }
    }
}
