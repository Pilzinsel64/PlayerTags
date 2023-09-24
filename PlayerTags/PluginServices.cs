using Dalamud.Data;
using Dalamud.Game;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.ClientState.Party;
using Dalamud.Game.Command;
using Dalamud.Game.Gui;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;

namespace PlayerTags
{
    public class PluginServices
    {
        [PluginService] public static DalamudPluginInterface DalamudPluginInterface { get; set; } = null!;
        [PluginService] public static IChatGui ChatGui { get; set; } = null!;
        [PluginService] public static IClientState ClientState { get; set; } = null!;
        [PluginService] public static ICommandManager CommandManager { get; set; } = null!;
        [PluginService] public static IDataManager DataManager { get; set; } = null!;
        [PluginService] public static IFramework Framework { get; set; } = null!;
        [PluginService] public static IGameGui GameGui { get; set; } = null!;
        [PluginService] public static IObjectTable ObjectTable { get; set; } = null!;
        [PluginService] public static IPartyList PartyList { get; set; } = null!;
        [PluginService] public static IGameInteropProvider GameInteropProvider { get; set; } = null;

        public static void Initialize(DalamudPluginInterface pluginInterface)
        {
            pluginInterface.Create<PluginServices>();
        }
    }
}
