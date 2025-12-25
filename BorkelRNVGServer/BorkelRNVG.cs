using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Logging;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Services;

namespace BorkelRNVGServer
{
    [Injectable(TypePriority = OnLoadOrder.PostDBModLoader + 1)]
    public class BorkelRNVG(DatabaseService databaseService) : IOnLoad
    {
        public Task OnLoad()
        {
            MongoId adapterId = "5c0695860db834001b735461";
            MongoId n15Id = "5c066e3a0db834001b7353f0";
            
            Dictionary<MongoId, TemplateItem> items = databaseService.GetItems();
            items.TryGetValue(adapterId, out TemplateItem? adapterTemplate);

            if (adapterTemplate != null)
            {
                // ?????
                adapterTemplate.Properties?.Slots?
                .FirstOrDefault()?
                .Properties?.Filters?
                .FirstOrDefault()?
                .Filter?.Add(n15Id);
            }
            
            return Task.CompletedTask;   
        }
    }
}
