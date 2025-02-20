using Dalamud.Game.ClientState.Objects.Types;
using ECommons.DalamudServices;
using ECommons.GameFunctions;
using ECommons.Throttlers;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using UcobNeuroScoring.UI;
using static FFXIVClientStructs.FFXIV.Client.System.Framework.Framework.Delegates;

namespace UcobNeuroScoring.Services
{
    internal unsafe class UcobNeuroService : IDisposable
    {
        private readonly ushort[] EnabledInstances = [97, 280];
        private const uint NEURO_DATA_ID = 2001151;

        internal List<IGameObject> NeurolinksScored;
        internal Dictionary<int, double> ScoreForEachWaymark;

        private const string Throttler = "SamplePlugin.UcobNeuroService";
        private bool WorkingThroughObjects = false;
        private int ThrottleTime = 500;

        public UcobNeuroService()
        {
            Initialise();
        }

        public void Initialise()
        {
            NeurolinksScored = new();
            ScoreForEachWaymark = new();

            if (P.Config.ScoringEnabled)
                Svc.Framework.Update += Tick;

            Svc.DutyState.DutyCompleted += ResetNeuros;
            //Svc.DutyState.DutyWiped += ResetNeuros;
        }

        public void ResetNeuros(object? sender, ushort e) => ResetNeuros();

        public void ResetNeuros()
        {
            Svc.Log.Debug($"Neuros reset.");
            NeurolinksScored.Clear();
            ScoreForEachWaymark.Clear();
        }

        public void Dispose()
        {
            Svc.Framework.Update -= Tick;

            Svc.DutyState.DutyCompleted -= ResetNeuros;
            //Svc.DutyState.DutyWiped -= ResetNeuros;

            NeurolinksScored.Clear();
            ScoreForEachWaymark.Clear();
        }

        public void ChangeThrottle(int throttle = 2000)
        {
            ThrottleTime = throttle;
            EzThrottler.Throttle(Throttler, ThrottleTime, true);
        }

        public void Tick(object _)
        {
            if (!P.Config.ScoringEnabled)
                return;

            if (!Svc.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.BoundByDuty] 
                && !Svc.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.BoundByDuty56]
                && !Svc.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.BoundByDuty95])
            {
                if (NeurolinksScored.Count > 0 || ScoreForEachWaymark.Count > 0)
                    ResetNeuros();

                return;
            }

            //Make sure we're in ucob (or T5 for testing).
            if (!EnabledInstances.Contains(GameMain.Instance()->CurrentContentFinderConditionId))
                return;

            //T5 specifically doesn't actually "wipe" so this is the cleanest way to handle that.
            //I need /somewhere/ to test 
            if (!Svc.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.InCombat])
            {
                if (NeurolinksScored.Count > 0 || ScoreForEachWaymark.Count > 0)
                    ResetNeuros();

                return;
            }

            //No more neuros to score.
            if (NeurolinksScored.Count == 3)
                return;
            
            //Do work every 500ms
            if (!EzThrottler.Throttle(Throttler, ThrottleTime)) return;

            //I hear your heartbeat to the beat of the drums.
            Svc.Log.Verbose("[UcobNeuroService] ♥");
            if (WorkingThroughObjects) return;

            WorkingThroughObjects = true;

            var neurosOnField = 
                Svc.Objects.Where(x => 
                    x.Name != null 
                    && (x.Name.ToString().Equals("neurolink", StringComparison.CurrentCultureIgnoreCase) 
                    || (string.IsNullOrWhiteSpace(x.Name.ToString()) && x.DataId == NEURO_DATA_ID))).OrderBy(x => x.EntityId);
            Svc.Log.Debug($"Neuros: {neurosOnField.Count()}");
            Svc.Log.Debug($"Scored Neuros: {NeurolinksScored.Count}");
            if (neurosOnField.Count() > NeurolinksScored.Count)
            {
                foreach (var neuro in neurosOnField)
                {
                    if (NeurolinksScored.Any(x => x.EntityId == neuro.EntityId)) continue;
                    //Svc.Log.Debug($"Scoring neuro with index: {neurosOnField.IndexOf(neuro)} (E: {neuro.EntityId})");
                    ScoreNeurolink(neurosOnField.IndexOf(neuro), neuro);
                }
            }

            WorkingThroughObjects = false;
        }

        public void ScoreNeurolink(int idx, IGameObject neuro)
        {
            switch (idx)
            {
                case 0:
                    CalculateScoreVersusWaymark((int)P.Config.NeurolinkWaymark1, neuro);
                    break;
                case 1:
                    CalculateScoreVersusWaymark((int)P.Config.NeurolinkWaymark2, neuro);
                    break;
                case 2:
                    CalculateScoreVersusWaymark((int)P.Config.NeurolinkWaymark3, neuro);
                    break;
                default:
                    return;
            }
        }

        public void CalculateScoreVersusWaymark(int waymarkIndex, IGameObject neuro)
        {
            try
            {
                var waymark = MarkingController.Instance()->FieldMarkers[waymarkIndex];
                if (!waymark.Active) return;

                var score = GetScore(waymark, neuro);

                NeurolinksScored.Add(neuro);
                ScoreForEachWaymark.Add(waymarkIndex, score);

                
                //Svc.Log.Debug($"Waymark {GetMarkerName(waymarkIndex)} score: {score}/100.");

                Svc.Chat.Print($"[{GetScoringName()}] Waymark {GetMarkerName(waymarkIndex)} score: {score}/100", "UcobScoring");
            }
            catch (Exception ex)
            {

            }
        }

        private double GetScore(FieldMarker waymark, IGameObject neuro)
        {
            switch (P.Config.ScoringType)
            {
                case ScoringType.Harder:
                    return HarderScoring(waymark, neuro);
                case ScoringType.Default:
                default:
                    return DefaultScoring(waymark, neuro);
            }
        }

        private double DefaultScoring(FieldMarker waymark, IGameObject neuro)
        {
            //To calc the score, we clamp the abs of (waymark.X - neuro.X) between 0 and 1 then * 100 and do 100 - that value. Repeat for Y, sum the values then divide by 2.
            var xDistance = Math.Round(Math.Abs(((double)waymark.X / 1000d) - neuro.Position.X), 3);
            var xScore = 100 - (Math.Clamp(xDistance, 0, 2) * 50);

            var yDistance = Math.Round(Math.Abs(((double)waymark.Z / 1000d) - neuro.Position.Z), 3);
            var yScore = 100 - (Math.Clamp(yDistance, 0, 2) * 50);

            var score = Math.Round((xScore + yScore) / 2, 2);

            Svc.Log.Verbose($"[Default Scoring] Waymark ({(double)waymark.X / 1000d}, {(double)waymark.Y / 1000d}) | Neuro ({Math.Round(neuro.Position.X, 3)}, {Math.Round(neuro.Position.Y, 3)}) | XDistance: {xDistance}, XScore: {xScore} | YDistance: {yDistance}, YScore: {yScore}");
            return score;
        }

        private double HarderScoring(FieldMarker waymark, IGameObject neuro)
        {
            //To calc the score, we clamp the abs of (waymark.X - neuro.X) between 0 and 1 then * 100 and do 100 - that value. Repeat for Y, sum the values then divide by 2.
            var xDistance = Math.Round(Math.Abs(((double)waymark.X / 1000d) - neuro.Position.X), 3);
            var xScore = 100 - (Math.Clamp(xDistance, 0, 1) * 100);

            var yDistance = Math.Round(Math.Abs(((double)waymark.Z / 1000d) - neuro.Position.Z), 3);
            var yScore = 100 - (Math.Clamp(yDistance, 0, 1) * 100);

            var score = Math.Round((xScore + yScore) / 2, 2);

            Svc.Log.Verbose($"[Harder Scoring] Waymark ({(double)waymark.X / 1000d}, {(double)waymark.Y / 1000d}) | Neuro ({Math.Round(neuro.Position.X, 3)}, {Math.Round(neuro.Position.Y, 3)}) | XDistance: {xDistance}, XScore: {xScore} | YDistance: {yDistance}, YScore: {yScore}");
            return score;
        }

        private double ExtremeScoring(FieldMarker waymark, IGameObject neuro)
        {
            return 0;
        }

        public static string GetMarkerName(int idx)
        {
            switch (idx)
            {
                case 0:
                    return "A";
                case 1:
                    return "B";
                case 2:
                    return "C";
                case 3:
                    return "D";
                case 4:
                    return "1";
                case 5:
                    return "2";
                case 6:
                    return "3";
                case 7:
                    return "4";
                default:
                    return "";
            }
        }

        public static string GetScoringName()
        {
            switch (P.Config.ScoringType)
            {
                case ScoringType.Harder:
                    return "Harder Scoring";
                case ScoringType.Default:
                default:
                    return "Default Scoring";
            }
        }
    
    }

    public enum ScoringType
    {
        Default,
        Harder
    }
}
