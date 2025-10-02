using Blasphemous.InventorySorting.Extensions;
using Framework.Managers;
using System.Collections.Generic;
using static Framework.Managers.InventoryManager;

namespace Blasphemous.InventorySorting.Configs;

public class SortingData
{
    public SortingMode sortingMode = SortingMode.ByAcquisitionOrder;

    /// <summary>
    /// List of owned items and their order by acquisition time and custom order.
    /// </summary>
    public List<ItemData> itemDatas = new();

    public Dictionary<SortingMode, bool> isAscending = new()
    {
        { SortingMode.ByAcquisitionOrder, true },
        { SortingMode.ByName, true },
        { SortingMode.ById, true },
        { SortingMode.Custom, true },
    };

    internal static readonly Dictionary<ItemType, string> itemTypeToLocKeyHeader = new()
    {
        { ItemType.Relic, "Relic"},
        { ItemType.Bead, "RosaryBead"},
        { ItemType.Quest, "QuestItem"},
        { ItemType.Prayer, "Prayer"},
        { ItemType.Collectible, "CollectibleItem"},
        { ItemType.Sword, "Sword"},
    };

    internal static Dictionary<SortingMode, string> SortingModeToDisplayName => new()
    {
        {SortingMode.ByAcquisitionOrder, Main.InventorySorting.LocalizationHandler.Localize("SortingMode.ByAcquisitionOrder")},
        {SortingMode.ByName, Main.InventorySorting.LocalizationHandler.Localize("SortingMode.ByName")},
        {SortingMode.ById, Main.InventorySorting.LocalizationHandler.Localize("SortingMode.ById")},
        {SortingMode.Custom, Main.InventorySorting.LocalizationHandler.Localize("SortingMode.Custom")},
    };

    public enum SortingMode
    {
        ByAcquisitionOrder,
        ByName,
        ById,
        Custom
    }

    public class ItemData
    {
        public string id;
        public int acquisitionOrder;
        public int customOrder;
        public string Name
        {
            get
            {
                if (!Core.InventoryManager.TryGetItemTypeFromId(id, out ItemType itemType))
                    return $"KEY_ERROR";
                return Core.Localization.Get($"{itemTypeToLocKeyHeader[itemType]}/{id}_CAPTION");
            }
        }

    }
}
