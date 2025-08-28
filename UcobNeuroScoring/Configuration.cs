using Dalamud.Configuration;
using Dalamud.Plugin;
using ECommons.DalamudServices;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using UcobNeuroScoring.Services;
using UcobNeuroScoring.UI;

namespace UcobNeuroScoring;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 0;

    public bool ScoringEnabled { get; set; } = true;

    //NA Defaults
    public Waymarks NeurolinkWaymark1 { get; set; } = Waymarks.One;
    public Waymarks NeurolinkWaymark2 { get; set; } = Waymarks.Two;
    public Waymarks NeurolinkWaymark3 { get; set; } = Waymarks.Three;

    //Scoring version
    public ScoringType ScoringType { get; set; } = ScoringType.Easy;

    // the below exist just to make saving less cumbersome
    public void Save()
    {
        Svc.PluginInterface.SavePluginConfig(this);
    }

    public static Configuration Load()
    {
        try
        {
            var contents = File.ReadAllText(Svc.PluginInterface.ConfigFile.FullName);
            var json = JObject.Parse(contents);
            var version = (int?)json["Version"] ?? 0;
            return json.ToObject<Configuration>() ?? new();
        }
        catch (Exception e)
        {
            Svc.Log.Error($"Failed to load config from {Svc.PluginInterface.ConfigFile.FullName}: {e}");
            return new();
        }
    }
}
