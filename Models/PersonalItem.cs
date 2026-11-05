using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChurchBudget_v3_WPF.Models
{
    public class PersonalItem
    {
        public long Id { get; set; }
        public string LastName { get; set; }
        public string FirstName { get; set; }
        public string MiddleName { get; set; }

        // Свойство автоматически собирает полное ФИО для выпадающего списка
        public string FullName => $"{LastName} {FirstName} {MiddleName}".Trim();
    }

}
