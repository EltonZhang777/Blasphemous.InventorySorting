using Blasphemous.InventorySorting.Components;
using Blasphemous.InventorySorting.Configs;
using Blasphemous.InventorySorting.Events;
using Blasphemous.InventorySorting.Extensions;
using Blasphemous.ModdingAPI;
using Blasphemous.ModdingAPI.Persistence;
using Gameplay.UI.Others.MenuLogic;
using System.Collections.Generic;
using UnityEngine;

namespace Blasphemous.InventorySorting;

public class InventorySorting : BlasMod, IPersistentMod
{
    internal SaveSlotConfig currentSaveConfig;
    internal MasterConfig masterConfig;
    internal EventsHandler eventsHandler = new();
    internal Dictionary<string, KeyCode> keybidings;
    internal InventorySorterWidget inventorySorterWidget;

    internal NewInventoryWidget VanillaInventoryWidget => GameObject.Find("/Game UI/Content/UI_NEWINVENTORY")?.GetComponent<NewInventoryWidget>();

    public string PersistentID => ModInfo.MOD_ID;

    internal InventorySorting() : base(ModInfo.MOD_ID, ModInfo.MOD_NAME, ModInfo.MOD_AUTHOR, ModInfo.MOD_VERSION) { }

    /// <inheritdoc/>
    public void LoadGame(SaveData data)
    {
        currentSaveConfig = data as SaveSlotConfig;
    }

    /// <inheritdoc/>
    public SaveData SaveGame()
    {
        return currentSaveConfig;
    }

    /// <inheritdoc/>
    public void ResetGame()
    {
        currentSaveConfig = new();
    }

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
        keybidings = InputHandler.GetAllKeybindings();
    }

    protected override void OnAllInitialized()
    {
        inventorySorterWidget = new();
    }

    protected override void OnUpdate()
    {
        eventsHandler.Update();
    }
}
