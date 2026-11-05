using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Collections.Generic;

namespace ChurchBudget_v3_WPF.Reports
{
    public class ExpensePacketReport : IDocument
    {
        private readonly List<ExpenseDataModel> _models;

        public ExpensePacketReport(List<ExpenseDataModel> models)
        {
            _models = models;
        }

        public DocumentSettings GetSettings() => DocumentSettings.Default;
        public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

        public void Compose(IDocumentContainer container)
        {
            // ИСПРАВЛЕНО: Рендерим каждый документ расхода как отдельную страницу формата А4 прямо в корень пакета
            for (int i = 0; i < _models.Count; i++)
            {
                var singleReport = new ExpenseReportA4(_models[i]);

                // Передаем корневой контейнер пакета напрямую в метод отрисовки одиночного расхода
                singleReport.Compose(container);
            }
        }
    }
}
