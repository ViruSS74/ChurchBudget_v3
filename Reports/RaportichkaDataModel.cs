using System;
using System.Data;
using static ChurchBudget_v3_WPF.Services.ListOfDocsService;

namespace ChurchBudget_v3_WPF.Reports
{
    public class RaportichkaDataModel
    {
        public string OrganizationName { get; set; }
        public string LocationText { get; set; }
        public string DocNumber { get; set; }
        public DateTime DocDate { get; set; }
        public DataTable ItemsTable { get; set; }
        public string TreasurerName { get; set; }
    }
}
