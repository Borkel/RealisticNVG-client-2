using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Spt.Tables;

namespace BorkelRNVGServer
{
    [Injectable(TypePriority = OnLoadOrder.Preload + 1)]
    public class BorkelRNVG(TemplateTable templateTable) : IOnLoad
    {
        public Task OnLoadAsync(CancellationToken cancellationToken)
        {
            MongoId adapterId = "5c0695860db834001b735461";
            MongoId n15Id = "5c066e3a0db834001b7353f0";
            
            Dictionary<MongoId, TemplateItem> items = templateTable.Items;
            items.TryGetValue(adapterId, out TemplateItem? adapterTemplate);

            if (adapterTemplate != null)
            {
                // ????? - pein
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
