using Blasphemous.Framework.UI;
using Blasphemous.InventorySorting.Configs;
using Blasphemous.InventorySorting.Extensions;
using Blasphemous.ModdingAPI;
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

internal class InventorySorterWidget
{
    private GameObject _gameObject;
    private Text _infoText;
    private NewInventoryWidget.TabType _currentTabType = NewInventoryWidget.TabType.Abilities;
    private NewInventoryWidget.TabType _previousTabType = NewInventoryWidget.TabType.Abilities;
    private bool _isCustomSortDragging = false;

    internal static readonly string keyBind_switchSortingMode = "Switch_Sorting_Mode";
    internal static readonly string keyBind_functionToggle = "Function_Toggle";
    internal static readonly string gameObjectName = "Inventory Sorter Widget";

    internal GameObject GameObject
    {
        get
        {
            _gameObject ??= CreateGameObject();
            return _gameObject;
        }
    }

    internal Text InfoText
    {
        get
        {
            _infoText ??= GameObject.GetComponent<Text>();
            return _infoText;
        }
    }

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

    internal InventorySorterWidget()
    {
        Main.InventorySorting.eventsHandler.OnUpdate += OnUpdate;
    }

    internal void OnUpdate()
    {
        // the widget should only be updating when the inventory is open
        // short hand logic checks if game is paused. If not, inventory certainly isn't up
        if (!UIController.instance.Paused)
            return;

        // actually checks if inventory is open and not in lore page or sword skill page
        NewInventoryWidget.MenuState currentMenuState = TraverseUtils.GetValue<NewInventoryWidget.MenuState>(Main.InventorySorting.VanillaInventoryWidget, "currentMenuState");
        if (currentMenuState == NewInventoryWidget.MenuState.OFF || currentMenuState == NewInventoryWidget.MenuState.UnlockSkills)
            return;

        // updates the text to the current tab type if tab type changes
        _previousTabType = _currentTabType;
        _currentTabType = TraverseUtils.GetValue<NewInventoryWidget.TabType>(Main.InventorySorting.VanillaInventoryWidget, "currentTabType");
        if (_previousTabType != _currentTabType)
        {
            // the inventory is switched to a different tab, check if new inventory items are added to this tab
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

            UpdateText();
            if (_isCustomSortDragging)
            {
                // tab is changed while dragging, abort the dragging process
                _isCustomSortDragging = false;
            }
        }

        if (Main.InventorySorting.InputHandler.GetKeyDown(keyBind_switchSortingMode))
        {
            CurrentSortingMode = Main.GetNextEnumValue(CurrentSortingMode);
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
            }
            UpdateText();
        }
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
        foreach (string id in vanillaList.Select(x => x.id).Except(modList.Select(x => x.id)))
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
        foreach (string id in modList.Select(x => x.id).Except(vanillaList.Select(x => x.id)))
        {
            hasAnyDifference = true;
            modList.RemoveAll(x => x.id == id);
        }

        return hasAnyDifference;
    }

    internal void SortInventoryTab(NewInventoryWidget.TabType tabType, SortingMode sortingMode, bool ascending = true)
    {
#if DEBUG
        ModLog.Warn($"Starting to sort tab `{tabType}` by sorting mode `{sortingMode}`!");
#endif
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

    private GameObject CreateGameObject()
    {
        Transform parent = Main.InventorySorting.VanillaInventoryWidget.transform.Find("External/Background");
        if (parent == null)
            return null;

        Vector2 rectSize = new Vector2(90, 500);

        _infoText = UIModder.Create(new RectCreationOptions()
        {
            Name = gameObjectName,
            Parent = parent,
            XRange = Vector2.zero,
            YRange = Vector2.one,
            Pivot = new Vector2(0, 1),
            Position = new Vector2(0, 0),
            Size = rectSize,
        }).AddText(new TextCreationOptions()
        {
            Alignment = TextAnchor.UpperLeft,
            FontSize = 16,
            Font = UIModder.Fonts.Blasphemous,
            WordWrap = true,
            Color = Color.white,
        });

        _infoText.transform.SetAsLastSibling();

        RectTransform rt = _infoText.gameObject.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = new Vector2(0, -20);

#if DEBUG
        ModLog.Warn($"Created text GameObject at position {_infoText.transform.position} !");
#endif
        return _infoText.gameObject;
    }

    private void UpdateText()
    {
        if (_currentTabType == NewInventoryWidget.TabType.Abilities)
        {
            InfoText.text = "";
            return;
        }

        StringBuilder sb = new();
        sb.AppendLine($"Sorting Mode: ");
        sb.AppendLine($"  {SortingData.sortingModeToDisplayName[CurrentSortingMode]}");
        if (CurrentSortingMode != SortingData.SortingMode.Custom)
        {
            sb.AppendLine($"Order: ");
            sb.AppendLine($"  {(Main.InventorySorting.currentSaveConfig.itemTypeToSortingData[_currentTabType].isAscending[CurrentSortingMode] ? "Ascending" : "Descending")}");
            sb.AppendLine($"Press {Main.InventorySorting.keybidings[keyBind_functionToggle]} to reverse sorting order");
        }
        else
        {
            if (_isCustomSortDragging)
            {
                sb.AppendLine($"Press {Main.InventorySorting.keybidings[keyBind_functionToggle]} to release the current item");
            }
            else
            {
                sb.AppendLine($"Press {Main.InventorySorting.keybidings[keyBind_functionToggle]} to drag the current item");
            }
        }
        sb.AppendLine($"Press {Main.InventorySorting.keybidings[keyBind_switchSortingMode]} to switch sorting mode");

        InfoText.text = sb.ToString();
    }
}
