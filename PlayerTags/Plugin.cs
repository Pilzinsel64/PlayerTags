using Dalamud.Game.Command;
using Dalamud.Logging;
using Dalamud.Plugin;
using Dalamud.Plugin.Internal;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
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
        public string Name => "Player Tags";
        private const string c_CommandName = "/playertags";
        private const string c_SubCommandName_EnableGlobal = "enableglobal";
        private const string c_CommandArg_On = "on";
        private const string c_CommandArg_Off = "off";
        private const string c_CommandArg_toggle = "toggle";

        private PluginConfiguration m_PluginConfiguration;
        private PluginData m_PluginData;
        private PluginConfigurationUI m_PluginConfigurationUI;

        private CustomTagsContextMenuFeature m_CustomTagsContextMenuFeature;
        private NameplateTagTargetFeature m_NameplatesTagTargetFeature;
        private ChatTagTargetFeature m_ChatTagTargetFeature;

        public Plugin(DalamudPluginInterface pluginInterface)
        {
            PluginServices.Initialize(pluginInterface);
            Pilz.Dalamud.PluginServices.Initialize(pluginInterface);

            m_PluginConfiguration = PluginConfiguration.LoadPluginConfig() ?? new PluginConfiguration();
            m_PluginData = new PluginData(m_PluginConfiguration);
            m_PluginConfigurationUI = new PluginConfigurationUI(m_PluginConfiguration, m_PluginData);

            Localizer.SetLanguage(PluginServices.DalamudPluginInterface.UiLanguage);
            PluginServices.DalamudPluginInterface.LanguageChanged += DalamudPluginInterface_LanguageChanged;

            PluginServices.DalamudPluginInterface.UiBuilder.Draw += UiBuilder_Draw;
            PluginServices.DalamudPluginInterface.UiBuilder.OpenConfigUi += UiBuilder_OpenConfigUi;
            PluginServices.CommandManager.AddHandler(c_CommandName, new CommandInfo(CommandManager_Handler)
            {
                HelpMessage = Resources.Strings.Loc_Command_playertags_v2
            });
            m_CustomTagsContextMenuFeature = new CustomTagsContextMenuFeature(m_PluginConfiguration, m_PluginData);
            m_NameplatesTagTargetFeature = new NameplateTagTargetFeature(m_PluginConfiguration, m_PluginData);
            m_ChatTagTargetFeature = new ChatTagTargetFeature(m_PluginConfiguration, m_PluginData);
        }

        public void Dispose()
        {
            m_ChatTagTargetFeature.Dispose();
            m_NameplatesTagTargetFeature.Dispose();
            m_CustomTagsContextMenuFeature.Dispose();
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
                                                m_PluginConfiguration.EnabledGlobal = true;
                                                break;
                                            case c_CommandArg_Off:
                                                m_PluginConfiguration.EnabledGlobal = false;
                                                break;
                                            case c_CommandArg_toggle:
                                                m_PluginConfiguration.EnabledGlobal = !m_PluginConfiguration.EnabledGlobal;
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
            if (m_PluginConfiguration.IsVisible)
                m_PluginConfigurationUI.Draw();
        }

        private void UiBuilder_OpenConfigUi()
        {
            m_PluginConfiguration.IsVisible = true;
            m_PluginConfiguration.Save(m_PluginData);
        }
    }
}
