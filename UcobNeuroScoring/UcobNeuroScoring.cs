using Dalamud.Game.Command;
using Dalamud.Interface.Style;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using ECommons;
using ECommons.DalamudServices;
using ECommons.ImGuiMethods;
using OtterGui.Classes;
using UcobNeuroScoring.Services;
using UcobNeuroScoring.UI;

namespace UcobNeuroScoring
{
    public class UcobNeuroScoring : IDalamudPlugin
    {
        public string Name => "Ucob Neuro Scoring";
        private const string SettingsCommand = "/ucobscoring";

        internal static UcobNeuroScoring P = null;
        internal SettingsUI SettingsUI;

        internal WindowSystem ws;
        internal Configuration Config;
        internal StyleModel Style;

        internal UcobNeuroService? UcobScoring = null;

        public UcobNeuroScoring(IDalamudPluginInterface pluginInterface)
        {
            ECommonsMain.Init(pluginInterface, this, Module.All);

            P = this;
            P.Config = Configuration.Load();

            ws = new();
            Config = P.Config;
            SettingsUI = new();

            UcobScoring = new();

            Svc.Commands.AddHandler(SettingsCommand, new CommandInfo(OpenPluginUICmd)
            {
                HelpMessage = "Opens the Scoring UI.\n",
                ShowInHelp = true,
            });

            Svc.PluginInterface.UiBuilder.OpenMainUi += OpenPluginUI;
            Svc.PluginInterface.UiBuilder.OpenConfigUi += OpenPluginUI;
            Svc.PluginInterface.UiBuilder.Draw += ws.Draw;

            Style = StyleModel.GetFromCurrent()!;
        }

        public void Dispose()
        {
            Svc.Log.Debug($"Disabling UcobNeuroScoring");
            Svc.PluginInterface.UiBuilder.Draw -= ws.Draw;
            Svc.PluginInterface.UiBuilder.OpenMainUi -= OpenPluginUI;
            Svc.PluginInterface.UiBuilder.OpenConfigUi -= OpenPluginUI;

            GenericHelpers.Safe(() => Svc.Commands.RemoveHandler(SettingsCommand));

            ws?.RemoveAllWindows();
            ws = null!;

            if (UcobScoring != null)
            {
                UcobScoring.Dispose();
                UcobScoring = null;
            }

            ECommonsMain.Dispose();
            P = null!;
        }

        private void OpenPluginUICmd(string command, string args) => OpenPluginUI();
        public void OpenPluginUI()
        {
            Svc.Log.Debug($"Opening UcobNeuroScoring PluginUI | Window count: {ws.Windows.Count}");
            foreach (var window in ws.Windows)
            {
                Svc.Log.Debug($"Window {window.WindowName} ({window.Namespace})");
            }
            SettingsUI.IsOpen = true;
        }
    }
}
