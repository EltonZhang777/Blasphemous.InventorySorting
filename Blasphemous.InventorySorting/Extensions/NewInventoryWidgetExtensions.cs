using Gameplay.UI.Others.MenuLogic;
using System.Collections.Generic;
using UnityEngine;

namespace Blasphemous.InventorySorting.Extensions;

internal static class NewInventoryWidgetExtensions
{
    public static void SetLastSlotSelected(this NewInventory_LayoutGrid grid, int slotIndex)
    {
        List<NewInventory_GridItem> cachedGridElements = TraverseUtils.GetValue<List<NewInventory_GridItem>>(grid, "cachedGridElements");
        slotIndex = Mathf.Clamp(slotIndex, 0, cachedGridElements.Count - 1);
        slotIndex = cachedGridElements[slotIndex].inventoryObject == null ? 0 : slotIndex;
        TraverseUtils.SetValue(ref grid, "currentSelected", slotIndex);
    }

    public static NewInventory_Layout Get_currentLayout(this NewInventoryWidget widget)
    {
        return TraverseUtils.GetValue<NewInventory_Layout>(widget, "currentLayout");
    }
}
