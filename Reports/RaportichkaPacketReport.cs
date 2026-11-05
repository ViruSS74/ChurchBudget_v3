using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Collections.Generic;

namespace ChurchBudget_v3_WPF.Reports
{
    public class RaportichkaPacketReport : IDocument
    {
        private readonly List<RaportichkaDataModel> _models;

        public RaportichkaPacketReport(List<RaportichkaDataModel> models)
        {
            _models = models;
        }

        public DocumentSettings GetSettings() => DocumentSettings.Default;
        public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

        public void Compose(IDocumentContainer container)
        {
            // Рендерим каждую Рапортичку как отдельную независимую страницу формата А5 прямо в корень пакета
            for (int i = 0; i < _models.Count; i++)
            {
                var singleReport = new RaportichkaReport(_models[i]);

                // Передаем корневой контейнер пакета напрямую в метод отрисовки одиночной Рапортички
                singleReport.Compose(container);
            }
        }
    }
}
