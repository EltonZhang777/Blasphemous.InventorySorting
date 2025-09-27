using Blasphemous.Framework.UI;
using Blasphemous.InventorySorting.Configs;
using Blasphemous.InventorySorting.Extensions;
using Blasphemous.ModdingAPI;
using Gameplay.UI;
using Gameplay.UI.Others.MenuLogic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

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

    internal SortingData.SortingMode CurrentSortingMode
    {
        get
        {
            return Main.InventorySorting.currentSaveConfig.itemTypeToSortingData[_currentTabType].sortingMode;
        }
        set
        {
            Main.InventorySorting.currentSaveConfig.itemTypeToSortingData[_currentTabType].sortingMode = value;
        }
    }

    internal InventorySorterWidget()
    {
        Main.InventorySorting.eventsHandler.OnUpdate += OnUpdate;
    }

    internal void OnUpdate()
    {
        // the widget should only be visible when the inventory is open
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
            UpdateText();
        }

        if (Main.InventorySorting.InputHandler.GetKeyDown(keyBind_switchSortingMode))
        {
            CurrentSortingMode = Main.GetNextEnumValue(CurrentSortingMode);
            UpdateText();
        }

        if (Main.InventorySorting.InputHandler.GetKeyDown(keyBind_functionToggle))
        {
            if (CurrentSortingMode != SortingData.SortingMode.Custom)
            {
                bool isAscending = Main.InventorySorting.currentSaveConfig.itemTypeToSortingData[_currentTabType].isAscending[CurrentSortingMode];
                Main.InventorySorting.currentSaveConfig.itemTypeToSortingData[_currentTabType].isAscending[CurrentSortingMode] = !isAscending;
            }
            else
            {
                _isCustomSortDragging = !_isCustomSortDragging;
            }
            UpdateText();
        }
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
            FontSize = 14,
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

        InfoText.text = sb.ToString();
    }
}
