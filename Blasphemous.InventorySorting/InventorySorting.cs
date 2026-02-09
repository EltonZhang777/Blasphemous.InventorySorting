using Blasphemous.Framework.UI;
using Blasphemous.InventorySorting.Components;
using Blasphemous.InventorySorting.Configs;
using Blasphemous.InventorySorting.Events;
using Blasphemous.InventorySorting.Extensions;
using Blasphemous.ModdingAPI;
using Blasphemous.ModdingAPI.Helpers;
using Blasphemous.ModdingAPI.Persistence;
using Gameplay.UI.Others.MenuLogic;
using System.Collections.Generic;
using UnityEngine;

namespace Blasphemous.InventorySorting;

public class InventorySorting : BlasMod, ISlotPersistentMod<SaveSlotConfig>
{
    internal SaveSlotConfig currentSaveConfig;
    internal MasterConfig masterConfig;
    internal EventsHandler eventsHandler = new();
    internal Dictionary<string, KeyCode> keybindings;
    internal InventorySorterWidget inventorySorterWidget;

    internal static Dictionary<string, string> replaceKeybindsToKeyNameInLocalization;

    internal NewInventoryWidget VanillaInventoryWidget => GameObject.Find("/Game UI/Content/UI_NEWINVENTORY")?.GetComponent<NewInventoryWidget>();

    public string PersistentID => ModInfo.MOD_ID;

    internal InventorySorting() : base(ModInfo.MOD_ID, ModInfo.MOD_NAME, ModInfo.MOD_AUTHOR, ModInfo.MOD_VERSION) { }

    /// <inheritdoc/>
    protected override void OnInitialize()
    {
        // load-in master config
        masterConfig = ConfigHandler.Load<MasterConfig>();
        ConfigHandler.Save(masterConfig);

        // initialize key bindings
        InputHandler.RegisterDefaultKeybindings(new Dictionary<string, KeyCode>()
        {
            { InventorySorterWidget.keyBind_switchSortingMode, KeyCode.LeftBracket },
            { InventorySorterWidget.keyBind_functionToggle, KeyCode.RightBracket },
        });
        keybindings = InputHandler.GetAllKeybindings();

        // initialize localization
        LocalizationHandler.RegisterDefaultLanguage("en");
        replaceKeybindsToKeyNameInLocalization = new()
        {
            { "<KeyBind_Switch_Sorting_Mode>", Main.InventorySorting.keybindings[InventorySorterWidget.keyBind_switchSortingMode].ToString() },
            { "<KeyBind_Function_Toggle>", Main.InventorySorting.keybindings[InventorySorterWidget.keyBind_functionToggle].ToString() },
        };
    }


    protected override void OnAllInitialized()
    {
        eventsHandler.OnFirstEnterMainMenu += () =>
        {
            // initialize InventorySorterWidget
            GameObject cameraObject = UIModder.Parents.CanvasHighRes.gameObject;
            GameObject widgetParent = new("Inventory Sorter Widget");
            widgetParent.transform.SetParent(cameraObject.transform, false);
            inventorySorterWidget = widgetParent.AddComponent<InventorySorterWidget>();
            ((RectTransform)(widgetParent.transform))
                .SetXRange(Vector2.zero)
                .SetYRange(Vector2.one)
                .SetPivot(new Vector2(0, 1))
                .SetPosition(new Vector2(30, -200))
                .SetSize(new Vector2(240, 800));
        };
    }

    protected override void OnLevelLoaded(string oldLevel, string newLevel)
    {
        if (SceneHelper.MenuSceneLoaded)
        {
            eventsHandler.FirstEnterMainMenu();
        }
    }

    protected override void OnUpdate()
    {
        eventsHandler.Update();
    }

    public SaveSlotConfig SaveSlot()
    {
        return currentSaveConfig;
    }

    public void LoadSlot(SaveSlotConfig data)
    {
        currentSaveConfig = data as SaveSlotConfig;
    }

    public void ResetSlot()
    {
        currentSaveConfig = new();
    }
}
