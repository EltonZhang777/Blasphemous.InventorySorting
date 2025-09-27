using System.Collections.Generic;

namespace Blasphemous.InventorySorting.Configs;

public class SortingData
{
    public SortingMode sortingMode = SortingMode.ByAcquisitionOrder;
    public List<string> customSortOrder = new();
    public Dictionary<SortingMode, bool> isAscending = new()
    {
        { SortingMode.ByAcquisitionOrder, true },
        { SortingMode.ByName, true },
        { SortingMode.ById, true },
    };

    public enum SortingMode
    {
        ByAcquisitionOrder,
        ByName,
        ById,
        Custom
    }

    internal static Dictionary<SortingMode, string> sortingModeToDisplayName = new()
    {
        {SortingMode.ByAcquisitionOrder, "By Acquisition Time"},
        {SortingMode.ByName, "By Name"},
        {SortingMode.ById, "By ID"},
        {SortingMode.Custom, "Custom"},
    };
}
