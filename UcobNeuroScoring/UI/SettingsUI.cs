using Dalamud.Interface.Windowing;
using ECommons.DalamudServices;
using Dalamud.Bindings.ImGui;
using System;
using System.Linq;
using UcobNeuroScoring.Services;
using System.Numerics;

namespace UcobNeuroScoring.UI
{
    class SettingsUI : Window
    {
        public SettingsUI() : base($"{P.Name} {P.GetType().Assembly.GetName().Version}###YhuUcobNeuroScoring")
        {
            SizeConstraints = new()
            {
                MinimumSize = new(540, 400),
            };

            Svc.Log.Debug($"Adding window to WS");
            P.ws.AddWindow(this);
        }

        public void Dispose() { }

        public override void Draw()
        {
            var scoringEnabled = P.Config.ScoringEnabled;
            var waymark1 = P.Config.NeurolinkWaymark1;
            var waymark2 = P.Config.NeurolinkWaymark2;
            var waymark3 = P.Config.NeurolinkWaymark3;
            var scoringType = P.Config.ScoringType;

            if (ImGui.Checkbox("Enable Scoring Plugin###UcobNeuroEnabled", ref scoringEnabled))
            {
                P.Config.ScoringEnabled = scoringEnabled;
                P.Config.Save();

                if (scoringEnabled)
                    P.UcobScoring?.Initialise();

                if (!scoringEnabled)
                    P.UcobScoring?.Dispose();
            }

            if (ImGui.BeginCombo($"Scoring Type###UcobNeuroScoringType", P.Config.ScoringType.ToString().Replace('_', ' ')))
            {
                foreach (var scoreType in Enum.GetValues(typeof(ScoringType)).Cast<ScoringType>())
                {
                    if (ImGui.Selectable($"{scoreType.ToString().Replace('_', ' ')}###ScoreType{scoreType.ToString()}", scoringType == scoreType))
                    {
                        P.Config.ScoringType = scoreType;
                        P.Config.Save();

                        P.UcobScoring?.ResetNeuros();
                    }
                }

                ImGui.EndCombo();
            }

            if (ImGui.BeginCombo($"Waymark 1###UcobNeuroWaymark1", P.Config.NeurolinkWaymark1.ToString()))
            {
                foreach (var waymark in Enum.GetValues(typeof(Waymarks)).Cast<Waymarks>())
                {
                    if (ImGui.Selectable($"{waymark.ToString()}###Waymark1{waymark.ToString()}", waymark1 == waymark))
                    {
                        P.Config.NeurolinkWaymark1 = waymark;
                        P.Config.Save();

                        P.UcobScoring?.ResetNeuros();
                    }
                }

                ImGui.EndCombo();
            }

            if (ImGui.BeginCombo($"Waymark 2###UcobNeuroWaymark2", P.Config.NeurolinkWaymark2.ToString()))
            {
                foreach (var waymark in Enum.GetValues(typeof(Waymarks)).Cast<Waymarks>())
                {
                    if (ImGui.Selectable($"{waymark.ToString()}###Waymark2{waymark.ToString()}", waymark2 == waymark))
                    {
                        P.Config.NeurolinkWaymark2 = waymark;
                        P.Config.Save();

                        P.UcobScoring?.ResetNeuros();
                    }
                }

                ImGui.EndCombo();
            }

            if (ImGui.BeginCombo($"Waymark 3###UcobNeuroWaymark3", P.Config.NeurolinkWaymark3.ToString()))
            {
                foreach (var waymark in Enum.GetValues(typeof(Waymarks)).Cast<Waymarks>())
                {
                    if (ImGui.Selectable($"{waymark.ToString()}###Waymark3{waymark.ToString()}", waymark3 == waymark))
                    {
                        P.Config.NeurolinkWaymark3 = waymark;
                        P.Config.Save();

                        P.UcobScoring?.ResetNeuros();
                    }
                }

                ImGui.EndCombo();
            }

            if (scoringEnabled && scoringType == ScoringType.Extreme)
            {
                ImGui.TextColored(new Vector4(1f, 0.2f, 0.2f, 1f), "Warning: Extreme Scoring is meant to be challenging.\n\t\tIt is recommended to play on Easy or Hard.");
            }
        }
    }

    public enum Waymarks
    {
        A,
        B,
        C,
        D,
        One,
        Two,
        Three,
        Four
    }
}
