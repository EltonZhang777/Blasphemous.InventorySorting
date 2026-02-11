using Blasphemous.Framework.UI;
using Blasphemous.InventorySorting.Configs;
using Blasphemous.InventorySorting.Extensions;
using Framework.Inventory;
using Framework.Managers;
using Gameplay.UI;
using Gameplay.UI.Others.MenuLogic;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using static Blasphemous.InventorySorting.Configs.SortingData;

namespace Blasphemous.InventorySorting.Components;

[RequireComponent(typeof(Text))]
internal class InventorySorterWidget : MonoBehaviour
{
    private Text _infoText;
    private RectTransform _rectTransform;
    private NewInventoryWidget.TabType _currentTabType = NewInventoryWidget.TabType.Abilities;
    private NewInventoryWidget.TabType _previousTabType = NewInventoryWidget.TabType.Abilities;
    private NewInventoryWidget.MenuState _previousMenuState = NewInventoryWidget.MenuState.OFF;
    private string _previousLanguageCode;
    private bool _isCustomSortDragging = false;
    private int _slotOfDraggedItem;
    private int _currentSlot;
    private int _fontSize = 12;

    internal static readonly string keyBind_switchSortingMode = "Switch_Sorting_Mode";
    internal static readonly string keyBind_functionToggle = "Function_Toggle";

    internal static float ScreenWidthScale => Screen.width / Core.Screen.GameCamera.pixelWidth;
    internal static float ScreenHeightScale => Screen.height / Core.Screen.GameCamera.pixelHeight;
    internal static float GuiScale => (ScreenHeightScale > ScreenWidthScale) ? ScreenWidthScale : ScreenHeightScale;


    internal SortingData CurrentSortingData
    {
        get
        {
            return Main.InventorySorting.currentSaveConfig.itemTypeToSortingData[_currentTabType];
        }
        set
        {
            Main.InventorySorting.currentSaveConfig.itemTypeToSortingData[_currentTabType] = value;
        }
    }

    internal SortingMode CurrentSortingMode
    {
        get
        {
            return CurrentSortingData.sortingMode;
        }
        set
        {
            CurrentSortingData.sortingMode = value;
        }
    }

    private void Awake()
    {
        _infoText = gameObject.GetComponent<Text>();
        _rectTransform = gameObject.GetComponent<RectTransform>();
        _fontSize = Main.InventorySorting.masterConfig.fontSize;

        _infoText.SetAlignment(TextAnchor.UpperLeft)
            .SetFont(UIModder.Fonts.Blasphemous)
            .SetFontSize((int)(_fontSize * GuiScale))
            .SetWrapping(true)
            .SetColor(Color.white);

        _rectTransform.SetXRange(Vector2.zero)
            .SetYRange(Vector2.one)
            .SetPivot(new Vector2(0, 1))
            .SetPosition(Main.InventorySorting.masterConfig.widgetPosition)
            .SetSize(Main.InventorySorting.masterConfig.widgetSize);

        Main.InventorySorting.eventsHandler.OnInventoryToggle += OnInventoryToggle;
    }

    private void OnDestroy()
    {
        Main.InventorySorting.eventsHandler.OnInventoryToggle -= OnInventoryToggle;
    }

    private void Update()
    {
        // the widget should only be updating when the inventory is open
        if (!UIController.instance.IsShowingInventory)
            return;

        // checks if inventory is open and not in lore page or sword skill page
        NewInventoryWidget.MenuState currentMenuState = TraverseUtils.GetValue<NewInventoryWidget.MenuState>(Main.InventorySorting.VanillaInventoryWidget, "currentMenuState");
        if ((currentMenuState == NewInventoryWidget.MenuState.OFF)
            || (currentMenuState == NewInventoryWidget.MenuState.UnlockSkills))
            return;

        // update text if inventory is just opened or if language is changed
        string currentLanguageCode = Core.Localization.GetCurrentLanguageCode();
        if (((_previousMenuState != currentMenuState)
            && (currentMenuState == NewInventoryWidget.MenuState.Normal))
            || _previousLanguageCode != currentLanguageCode)
        {
            UpdateText();
            _previousMenuState = currentMenuState;
            _previousLanguageCode = currentLanguageCode;
        }

        // updates the text to the current tab type if tab type changes
        _previousTabType = _currentTabType;
        _currentTabType = TraverseUtils.GetValue<NewInventoryWidget.TabType>(Main.InventorySorting.VanillaInventoryWidget, "currentTabType");
        if (_previousTabType != _currentTabType)
        {
            // the inventory is switched to a different tab, check if new inventory items are added to this tab
            ProcessNewInventoryObjects();
            UpdateText();
            if (_isCustomSortDragging)
            {
                // tab is changed while dragging, abort the dragging process
                _isCustomSortDragging = false;
                // save the updated custom order of previous tab
                UpdateCustomSortOrder(_previousTabType);
            }
        }

        if (Main.InventorySorting.InputHandler.GetKeyDown(keyBind_switchSortingMode))
        {
            if (_isCustomSortDragging)
            {
                // sorting mode is changed while dragging, abort the dragging process
                _isCustomSortDragging = false;
                // save the updated custom order of current tab
                UpdateCustomSortOrder(_currentTabType);
            }
            CurrentSortingMode = CurrentSortingMode.GetNextEnumValue();
            UpdateText();
            SortInventoryTab(_currentTabType, CurrentSortingMode, CurrentSortingData.isAscending[CurrentSortingMode]);
        }

        if (Main.InventorySorting.InputHandler.GetKeyDown(keyBind_functionToggle))
        {
            if (CurrentSortingMode != SortingMode.Custom)
            {
                bool isAscending = CurrentSortingData.isAscending[CurrentSortingMode];
                CurrentSortingData.isAscending[CurrentSortingMode] = !isAscending;
                SortInventoryTab(_currentTabType, CurrentSortingMode, CurrentSortingData.isAscending[CurrentSortingMode]);
            }
            else
            {
                _isCustomSortDragging = !_isCustomSortDragging;
                if (_isCustomSortDragging)
                {
                    // initialize index and sort order for custom dragging 
                    _slotOfDraggedItem = Main.InventorySorting.VanillaInventoryWidget.Get_currentLayout().GetLastSlotSelected();
                }
                else
                {
                    // save the updated custom order of current tab
                    UpdateCustomSortOrder(_currentTabType);
                }
            }
            UpdateText();
        }

        if (_isCustomSortDragging)
        {
            do
            {
                _currentSlot = Main.InventorySorting.VanillaInventoryWidget.Get_currentLayout().GetLastSlotSelected();
                if (_currentSlot == _slotOfDraggedItem)
                    break;

                List<NewInventory_GridItem> cachedGridElements = TraverseUtils.GetValue<List<NewInventory_GridItem>>(Main.InventorySorting.VanillaInventoryWidget.Get_currentLayout(), "cachedGridElements");
                if (_currentSlot != Mathf.Clamp(_currentSlot, 0, cachedGridElements.Count))
                {
                    // _currentSlot is out of bounds, abort custom dragging
                    _isCustomSortDragging = false;
                    break;
                }
                if (cachedGridElements[_currentSlot].inventoryObject == null)
                {
                    // _currentSlot has no item, abort custom dragging
                    _isCustomSortDragging = false;
                    break;
                }

                ProcessDragging();
            } while (false);
        }
    }

    internal void OnInventoryToggle(bool active)
    {
        gameObject.SetActive(active);
    }

    internal void ProcessDragging()
    {
        Traverse traverse;
        switch (_currentTabType)
        {
            case NewInventoryWidget.TabType.Collectables:
                traverse = Traverse.Create(Core.InventoryManager);
                List<BlasCollectibleItem> collectibleItems = TraverseUtils.GetValue<List<BlasCollectibleItem>>(traverse, "ownCollectibleItems");
                collectibleItems.Move(_slotOfDraggedItem, _currentSlot);
                _slotOfDraggedItem = _currentSlot;
                traverse = Traverse.Create(Core.InventoryManager);
                TraverseUtils.SetValue(ref traverse, "ownCollectibleItems", collectibleItems);
                break;
            case NewInventoryWidget.TabType.Prayers:
                traverse = Traverse.Create(Core.InventoryManager);
                List<Prayer> prayers = TraverseUtils.GetValue<List<Prayer>>(traverse, "ownPrayers");
                prayers.Move(_slotOfDraggedItem, _currentSlot);
                _slotOfDraggedItem = _currentSlot;
                traverse = Traverse.Create(Core.InventoryManager);
                TraverseUtils.SetValue(ref traverse, "ownPrayers", prayers);
                break;
            case NewInventoryWidget.TabType.Quest:
                traverse = Traverse.Create(Core.InventoryManager);
                List<QuestItem> questItems = TraverseUtils.GetValue<List<QuestItem>>(traverse, "ownQuestItems");
                questItems.Move(_slotOfDraggedItem, _currentSlot);
                _slotOfDraggedItem = _currentSlot;
                traverse = Traverse.Create(Core.InventoryManager);
                TraverseUtils.SetValue(ref traverse, "ownQuestItems", questItems);
                break;
            case NewInventoryWidget.TabType.Reliquary:
                traverse = Traverse.Create(Core.InventoryManager);
                List<Relic> relics = TraverseUtils.GetValue<List<Relic>>(traverse, "ownRellics");
                relics.Move(_slotOfDraggedItem, _currentSlot);
                _slotOfDraggedItem = _currentSlot;
                traverse = Traverse.Create(Core.InventoryManager);
                TraverseUtils.SetValue(ref traverse, "ownRellics", relics);
                break;
            case NewInventoryWidget.TabType.Rosary:
                traverse = Traverse.Create(Core.InventoryManager);
                List<RosaryBead> rosaryBeads = TraverseUtils.GetValue<List<RosaryBead>>(traverse, "ownBeads");
                rosaryBeads.Move(_slotOfDraggedItem, _currentSlot);
                _slotOfDraggedItem = _currentSlot;
                traverse = Traverse.Create(Core.InventoryManager);
                TraverseUtils.SetValue(ref traverse, "ownBeads", rosaryBeads);
                break;
            case NewInventoryWidget.TabType.Sword:
                traverse = Traverse.Create(Core.InventoryManager);
                List<Sword> swordHearts = TraverseUtils.GetValue<List<Sword>>(traverse, "ownSwords");
                swordHearts.Move(_slotOfDraggedItem, _currentSlot);
                _slotOfDraggedItem = _currentSlot;
                traverse = Traverse.Create(Core.InventoryManager);
                TraverseUtils.SetValue(ref traverse, "ownSwords", swordHearts);
                break;
        }

        // refresh the widget to update the change
        RefreshNewInventoryWidget();
    }

    /// <summary>
    /// Modifies the modList to contain the same items to vanillaList by checking for differences. 
    /// Returns true if any differences is found.
    /// </summary>
    internal static bool ResolveNewInventoryObjects<T>(List<T> vanillaList, ref List<SortingData.ItemData> modList) where T : BaseInventoryObject
    {
        bool hasAnyDifference = false;
        // for any items in vanillaList that are not in modList,
        // these are newly-added items, add them to modList
        int currentMaxAcquisitionOrder = modList.Count == 0
            ? 0
            : modList.Select(x => x.acquisitionOrder).Max();
        List<string> ids = vanillaList.Select(x => x.id).Except(modList.Select(x => x.id)).ToList();
        foreach (string id in ids)
        {
            // newly-added items have the highest acquisition order (current largest order + 1)
            // and a custom order the same of acquisition order
            hasAnyDifference = true;
            currentMaxAcquisitionOrder++;
            modList.Add(new SortingData.ItemData()
            {
                id = id,
                acquisitionOrder = currentMaxAcquisitionOrder,
                customOrder = currentMaxAcquisitionOrder,
            });
        }

        // for any items in modList that are not in vanillaList,
        // these are removed items, remove them from modList
        ids = modList.Select(x => x.id).Except(vanillaList.Select(x => x.id)).ToList();
        foreach (string id in ids)
        {
            hasAnyDifference = true;
            modList.RemoveAll(x => x.id == id);
        }

        return hasAnyDifference;
    }

    internal void ProcessNewInventoryObjects()
    {
        bool hasAnyChangedItem = false;
        switch (_currentTabType)
        {
            case NewInventoryWidget.TabType.Abilities:
                break;
            case NewInventoryWidget.TabType.Collectables:
                hasAnyChangedItem = ResolveNewInventoryObjects(Core.InventoryManager.GetCollectibleItemOwned().ToList(), ref CurrentSortingData.itemDatas);
                break;
            case NewInventoryWidget.TabType.Prayers:
                hasAnyChangedItem = ResolveNewInventoryObjects(Core.InventoryManager.GetPrayersOwned().ToList(), ref CurrentSortingData.itemDatas);
                break;
            case NewInventoryWidget.TabType.Quest:
                hasAnyChangedItem = ResolveNewInventoryObjects(Core.InventoryManager.GetQuestItemOwned().ToList(), ref CurrentSortingData.itemDatas);
                break;
            case NewInventoryWidget.TabType.Reliquary:
                hasAnyChangedItem = ResolveNewInventoryObjects(Core.InventoryManager.GetRelicsOwned().ToList(), ref CurrentSortingData.itemDatas);
                break;
            case NewInventoryWidget.TabType.Rosary:
                hasAnyChangedItem = ResolveNewInventoryObjects(Core.InventoryManager.GetRosaryBeadOwned().ToList(), ref CurrentSortingData.itemDatas);
                break;
            case NewInventoryWidget.TabType.Sword:
                hasAnyChangedItem = ResolveNewInventoryObjects(Core.InventoryManager.GetSwordsOwned().ToList(), ref CurrentSortingData.itemDatas);
                break;
        }

        if (hasAnyChangedItem)
        {
            // re-sort the item order if there's any changes to items
            SortInventoryTab(_currentTabType, CurrentSortingMode, CurrentSortingData.isAscending[CurrentSortingMode]);
        }
    }

    internal void SortInventoryTab(NewInventoryWidget.TabType tabType, SortingMode sortingMode, bool ascending = true)
    {
        Traverse traverse;
        switch (tabType)
        {
            case NewInventoryWidget.TabType.Collectables:
                traverse = Traverse.Create(Core.InventoryManager);
                List<BlasCollectibleItem> collectibleItems = TraverseUtils.GetValue<List<BlasCollectibleItem>>(traverse, "ownCollectibleItems");
                collectibleItems.Sort(GetComparerBySortingMode<BlasCollectibleItem>(sortingMode, ascending));
                traverse = Traverse.Create(Core.InventoryManager);
                TraverseUtils.SetValue(ref traverse, "ownCollectibleItems", collectibleItems);
                break;
            case NewInventoryWidget.TabType.Prayers:
                traverse = Traverse.Create(Core.InventoryManager);
                List<Prayer> prayers = TraverseUtils.GetValue<List<Prayer>>(traverse, "ownPrayers");
                prayers.Sort(GetComparerBySortingMode<Prayer>(sortingMode, ascending));
                traverse = Traverse.Create(Core.InventoryManager);
                TraverseUtils.SetValue(ref traverse, "ownPrayers", prayers);
                break;
            case NewInventoryWidget.TabType.Quest:
                traverse = Traverse.Create(Core.InventoryManager);
                List<QuestItem> questItems = TraverseUtils.GetValue<List<QuestItem>>(traverse, "ownQuestItems");
                questItems.Sort(GetComparerBySortingMode<QuestItem>(sortingMode, ascending));
                traverse = Traverse.Create(Core.InventoryManager);
                TraverseUtils.SetValue(ref traverse, "ownQuestItems", questItems);
                break;
            case NewInventoryWidget.TabType.Reliquary:
                traverse = Traverse.Create(Core.InventoryManager);
                List<Relic> relics = TraverseUtils.GetValue<List<Relic>>(traverse, "ownRellics");
                relics.Sort(GetComparerBySortingMode<Relic>(sortingMode, ascending));
                traverse = Traverse.Create(Core.InventoryManager);
                TraverseUtils.SetValue(ref traverse, "ownRellics", relics);
                break;
            case NewInventoryWidget.TabType.Rosary:
                traverse = Traverse.Create(Core.InventoryManager);
                List<RosaryBead> rosaryBeads = TraverseUtils.GetValue<List<RosaryBead>>(traverse, "ownBeads");
                rosaryBeads.Sort(GetComparerBySortingMode<RosaryBead>(sortingMode, ascending));
                traverse = Traverse.Create(Core.InventoryManager);
                TraverseUtils.SetValue(ref traverse, "ownBeads", rosaryBeads);
                break;
            case NewInventoryWidget.TabType.Sword:
                traverse = Traverse.Create(Core.InventoryManager);
                List<Sword> swordHearts = TraverseUtils.GetValue<List<Sword>>(traverse, "ownSwords");
                swordHearts.Sort(GetComparerBySortingMode<Sword>(sortingMode, ascending));
                traverse = Traverse.Create(Core.InventoryManager);
                TraverseUtils.SetValue(ref traverse, "ownSwords", swordHearts);
                break;
        }

        // refresh inventory widget to apply the sorting changes
        RefreshNewInventoryWidget();
    }

    internal static void RefreshNewInventoryWidget()
    {
        Main.InventorySorting.VanillaInventoryWidget.StartCoroutine(RefreshCoroutine());

        IEnumerator RefreshCoroutine()
        {
            yield return new WaitForEndOfFrame();
            Main.InventorySorting.VanillaInventoryWidget.Show(false);
            yield return null;
            Main.InventorySorting.VanillaInventoryWidget.Show(true);
        }
    }

    private Comparison<T> GetComparerBySortingMode<T>(SortingMode sortingMode, bool ascending = true) where T : BaseInventoryObject
    {
        Comparison<T> comparerFunction = null;
        switch (sortingMode)
        {
            case SortingMode.ByAcquisitionOrder:
                comparerFunction = (a, b) =>
                {
                    int aIndex = CurrentSortingData.itemDatas.FindIndex(x => x.id == a.id);
                    int bIndex = CurrentSortingData.itemDatas.FindIndex(x => x.id == b.id);

                    if (aIndex == -1 || bIndex == -1)
                        return 0; // If either item is not found in itemData, maintain original order

                    ItemData aData = CurrentSortingData.itemDatas[aIndex];
                    ItemData bData = CurrentSortingData.itemDatas[bIndex];

                    int result = aData.acquisitionOrder.CompareTo(bData.acquisitionOrder);
                    return ascending ? result : -result;
                };
                break;
            case SortingMode.ById:
                comparerFunction = (a, b) =>
                {
                    int result = a.id.CompareTo(b.id);
                    return ascending ? result : -result;
                };
                break;
            case SortingMode.ByName:
                comparerFunction = (a, b) =>
                {
                    int aIndex = CurrentSortingData.itemDatas.FindIndex(x => x.id == a.id);
                    int bIndex = CurrentSortingData.itemDatas.FindIndex(x => x.id == b.id);

                    if (aIndex == -1 || bIndex == -1)
                        return 0; // If either item is not found in itemData, maintain original order

                    ItemData aData = CurrentSortingData.itemDatas[aIndex];
                    ItemData bData = CurrentSortingData.itemDatas[bIndex];

                    int result = aData.Name.CompareTo(bData.Name);
                    return ascending ? result : -result;
                };
                break;
            case SortingMode.Custom:
                comparerFunction = (a, b) =>
                {
                    int aIndex = CurrentSortingData.itemDatas.FindIndex(x => x.id == a.id);
                    int bIndex = CurrentSortingData.itemDatas.FindIndex(x => x.id == b.id);

                    if (aIndex == -1 || bIndex == -1)
                        return 0; // If either item is not found in itemData, maintain original order

                    ItemData aData = CurrentSortingData.itemDatas[aIndex];
                    ItemData bData = CurrentSortingData.itemDatas[bIndex];

                    int result = aData.customOrder.CompareTo(bData.customOrder);
                    return ascending ? result : -result;
                };
                break;
        }
        return comparerFunction;
    }

    /// <summary>
    /// Write the updated custom order of specified tab to SortingData
    /// </summary>
    internal void UpdateCustomSortOrder(NewInventoryWidget.TabType tabType)
    {
        Traverse traverse;
        switch (tabType)
        {
            case NewInventoryWidget.TabType.Collectables:
                traverse = Traverse.Create(Core.InventoryManager);
                List<BlasCollectibleItem> collectibleItems = TraverseUtils.GetValue<List<BlasCollectibleItem>>(traverse, "ownCollectibleItems");
                for (int i = 0; i < collectibleItems.Count; i++)
                {
                    string id = collectibleItems[i].id;
                    int index = CurrentSortingData.itemDatas.FindIndex(x => x.id == id);
                    if (index != -1)
                    {
                        CurrentSortingData.itemDatas[index].customOrder = i;
                    }
                }
                break;
            case NewInventoryWidget.TabType.Prayers:
                traverse = Traverse.Create(Core.InventoryManager);
                List<Prayer> prayers = TraverseUtils.GetValue<List<Prayer>>(traverse, "ownPrayers");
                for (int i = 0; i < prayers.Count; i++)
                {
                    string id = prayers[i].id;
                    int index = CurrentSortingData.itemDatas.FindIndex(x => x.id == id);
                    if (index != -1)
                    {
                        CurrentSortingData.itemDatas[index].customOrder = i;
                    }
                }
                break;
            case NewInventoryWidget.TabType.Quest:
                traverse = Traverse.Create(Core.InventoryManager);
                List<QuestItem> questItems = TraverseUtils.GetValue<List<QuestItem>>(traverse, "ownQuestItems");
                for (int i = 0; i < questItems.Count; i++)
                {
                    string id = questItems[i].id;
                    int index = CurrentSortingData.itemDatas.FindIndex(x => x.id == id);
                    if (index != -1)
                    {
                        CurrentSortingData.itemDatas[index].customOrder = i;
                    }
                }
                break;
            case NewInventoryWidget.TabType.Reliquary:
                traverse = Traverse.Create(Core.InventoryManager);
                List<Relic> relics = TraverseUtils.GetValue<List<Relic>>(traverse, "ownRellics");
                for (int i = 0; i < relics.Count; i++)
                {
                    string id = relics[i].id;
                    int index = CurrentSortingData.itemDatas.FindIndex(x => x.id == id);
                    if (index != -1)
                    {
                        CurrentSortingData.itemDatas[index].customOrder = i;
                    }
                }
                break;
            case NewInventoryWidget.TabType.Rosary:
                traverse = Traverse.Create(Core.InventoryManager);
                List<RosaryBead> rosaryBeads = TraverseUtils.GetValue<List<RosaryBead>>(traverse, "ownBeads");
                for (int i = 0; i < rosaryBeads.Count; i++)
                {
                    string id = rosaryBeads[i].id;
                    int index = CurrentSortingData.itemDatas.FindIndex(x => x.id == id);
                    if (index != -1)
                    {
                        CurrentSortingData.itemDatas[index].customOrder = i;
                    }
                }
                break;
            case NewInventoryWidget.TabType.Sword:
                traverse = Traverse.Create(Core.InventoryManager);
                List<Sword> swordHearts = TraverseUtils.GetValue<List<Sword>>(traverse, "ownSwords");
                for (int i = 0; i < swordHearts.Count; i++)
                {
                    string id = swordHearts[i].id;
                    int index = CurrentSortingData.itemDatas.FindIndex(x => x.id == id);
                    if (index != -1)
                    {
                        CurrentSortingData.itemDatas[index].customOrder = i;
                    }
                }
                break;
        }

    }

    private void UpdateText()
    {
        if (_currentTabType == NewInventoryWidget.TabType.Abilities)
        {
            _infoText.text = "";
            return;
        }

        // set font size
        _infoText.SetFontSize((int)(_fontSize * GuiScale));

        // switch to Arial font for Chinese as a temporary solution to missing characters in Blasphemous font
        if (Core.Localization.GetCurrentLanguageCode().Equals("zh"))
        {
            _infoText.SetFont(UIModder.Fonts.Arial);
        }
        else
        {
            _infoText.SetFont(UIModder.Fonts.Blasphemous);
        }

        StringBuilder sb = new();
        sb.AppendLine($"{Main.Localize("InventorySorterWidget.SortingMode.Header")}");
        sb.AppendLine($"  {SortingData.SortingModeToDisplayName[CurrentSortingMode]}");
        sb.AppendLine($"");

        if (CurrentSortingMode != SortingData.SortingMode.Custom)
        {
            sb.AppendLine($"{Main.Localize("InventorySorterWidget.Order.Header")}");
            sb.AppendLine($"  " + (Main.InventorySorting.currentSaveConfig.itemTypeToSortingData[_currentTabType].isAscending[CurrentSortingMode]
                ? Main.Localize("InventorySorterWidget.Order.Ascending")
                : Main.Localize("InventorySorterWidget.Order.Descending")));

            sb.AppendLine($"");
            sb.AppendLine($"{Main.Localize("InventorySorterWidget.Order.ToggleAscending").ReplaceWords(InventorySorting.replaceKeybindsToKeyNameInLocalization)}");
        }
        else
        {
            if (_isCustomSortDragging)
            {
                sb.AppendLine($"{Main.Localize("InventorySorterWidget.CustomSort.ToggleDrag.On").ReplaceWords(InventorySorting.replaceKeybindsToKeyNameInLocalization)}");
            }
            else
            {
                sb.AppendLine($"{Main.Localize("InventorySorterWidget.CustomSort.ToggleDrag.Off").ReplaceWords(InventorySorting.replaceKeybindsToKeyNameInLocalization)}");
            }
        }

        sb.AppendLine($"");
        sb.AppendLine($"{Main.Localize("InventorySorterWidget.SwitchSortingMode").ReplaceWords(InventorySorting.replaceKeybindsToKeyNameInLocalization)}");

        _infoText.text = sb.ToString();
    }
}
