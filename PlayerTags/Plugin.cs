using Dalamud.Game.Command;
using Dalamud.Logging;
using Dalamud.Plugin;
using Dalamud.Plugin.Internal;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using Lumina.Excel.GeneratedSheets;
using PlayerTags.Configuration;
using PlayerTags.Data;
using PlayerTags.Features;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;

namespace PlayerTags
{
    public sealed class Plugin : IDalamudPlugin
    {
        private const string c_CommandName = "/playertags";
        private const string c_SubCommandName_EnableGlobal = "enableglobal";
        private const string c_CommandArg_On = "on";
        private const string c_CommandArg_Off = "off";
        private const string c_CommandArg_toggle = "toggle";

        private PluginConfiguration pluginConfiguration = null;
        private PluginData pluginData = null;
        private PluginConfigurationUI pluginConfigurationUI = null;
        private DalamudPluginInterface pluginInterface = null;

        private CustomTagsContextMenuFeature customTagsContextMenuFeature;
        private NameplateTagTargetFeature nameplatesTagTargetFeature;
        private ChatTagTargetFeature chatTagTargetFeature;

        public Plugin(DalamudPluginInterface pluginInterface)
        {
            PluginServices.Initialize(pluginInterface);
            Pilz.Dalamud.PluginServices.Initialize(pluginInterface);

            pluginConfiguration = PluginConfiguration.LoadPluginConfig() ?? new PluginConfiguration();
            pluginData = new PluginData(pluginConfiguration);
            pluginConfigurationUI = new PluginConfigurationUI(pluginConfiguration, pluginData);

            Localizer.SetLanguage(PluginServices.DalamudPluginInterface.UiLanguage);
            PluginServices.DalamudPluginInterface.LanguageChanged += DalamudPluginInterface_LanguageChanged;

            PluginServices.DalamudPluginInterface.UiBuilder.Draw += UiBuilder_Draw;
            PluginServices.DalamudPluginInterface.UiBuilder.OpenConfigUi += UiBuilder_OpenConfigUi;
            PluginServices.CommandManager.AddHandler(c_CommandName, new CommandInfo(CommandManager_Handler)
            {
                HelpMessage = Resources.Strings.Loc_Command_playertags_v2
            });
            customTagsContextMenuFeature = new CustomTagsContextMenuFeature(pluginConfiguration, pluginData, pluginInterface);
            nameplatesTagTargetFeature = new NameplateTagTargetFeature(pluginConfiguration, pluginData);
            chatTagTargetFeature = new ChatTagTargetFeature(pluginConfiguration, pluginData);
        }

        public void Dispose()
        {
            chatTagTargetFeature.Dispose();
            nameplatesTagTargetFeature.Dispose();
            customTagsContextMenuFeature.Dispose();
            PluginServices.DalamudPluginInterface.LanguageChanged -= DalamudPluginInterface_LanguageChanged;
            PluginServices.CommandManager.RemoveHandler(c_CommandName);
            PluginServices.DalamudPluginInterface.UiBuilder.OpenConfigUi -= UiBuilder_OpenConfigUi;
            PluginServices.DalamudPluginInterface.UiBuilder.Draw -= UiBuilder_Draw;
        }

        private void DalamudPluginInterface_LanguageChanged(string langCode)
        {
            Localizer.SetLanguage(langCode);
        }

        private void CommandManager_Handler(string command, string arguments)
        {
            switch (command)
            {
                case c_CommandName:
                    if (string.IsNullOrWhiteSpace(arguments))
                        UiBuilder_OpenConfigUi();
                    else
                    {
                        var lowerArgs = arguments.ToLower().Split(' ');
                        if (lowerArgs.Length >= 1)
                        {
                            switch (lowerArgs[0])
                            {
                                case c_SubCommandName_EnableGlobal:
                                    if (lowerArgs.Length >= 2)
                                    {
                                        switch (lowerArgs[0])
                                        {
                                            case c_CommandArg_On:
                                                pluginConfiguration.EnabledGlobal = true;
                                                break;
                                            case c_CommandArg_Off:
                                                pluginConfiguration.EnabledGlobal = false;
                                                break;
                                            case c_CommandArg_toggle:
                                                pluginConfiguration.EnabledGlobal = !pluginConfiguration.EnabledGlobal;
                                                break;
                                        }
                                    }
                                    break;
                            }
                        }
                    }
                    break;
            }
        }

        private void UiBuilder_Draw()
        {
            if (pluginConfiguration.IsVisible)
                pluginConfigurationUI.Draw();
        }

        private void UiBuilder_OpenConfigUi()
        {
            pluginConfiguration.IsVisible = true;
            pluginConfiguration.Save(pluginData);
        }
    }
}
