using Blasphemous.ModdingAPI.Persistence;
using Gameplay.UI.Others.MenuLogic;
using System.Collections.Generic;

namespace Blasphemous.InventorySorting.Configs;

public class SaveSlotConfig : SlotSaveData
{
    public Dictionary<NewInventoryWidget.TabType, SortingData> itemTypeToSortingData = new()
    {
        { NewInventoryWidget.TabType.Rosary, new SortingData() },
        { NewInventoryWidget.TabType.Reliquary, new SortingData() },
        { NewInventoryWidget.TabType.Quest, new SortingData() },
        { NewInventoryWidget.TabType.Sword, new SortingData() },
        { NewInventoryWidget.TabType.Prayers, new SortingData() },
        { NewInventoryWidget.TabType.Collectables, new SortingData() },
    };
}
