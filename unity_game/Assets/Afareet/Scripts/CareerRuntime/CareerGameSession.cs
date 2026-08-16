using System;
using System.Collections.Generic;
using Afareet.Progression;
using Afareet.Race;
using UnityEngine;

namespace Afareet.CareerRuntime
{
    public sealed class CareerGameSession : MonoBehaviour
    {
        private readonly CareerProgressionService progression = new CareerProgressionService();
        private readonly CareerEventSettlementService settlementService = new CareerEventSettlementService();

        private IReadOnlyList<CareerNodeDefinition> definitions;
        private CareerProgressStore store;
        private RaceRoundController round;
        private RaceDirector race;
        private RacePerformanceMetricsTracker performance;
        private RaceRoundCareerSessionAdapter adapter;
        private CareerNodeDefinition activeDefinition;
        private bool configured;

        public CareerProgress Progress { get; private set; }
        public CareerNodeDefinition ActiveDefinition => activeDefinition;
        public CareerEventSettlement LastSettlement { get; private set; }
        public bool HasActiveEvent => activeDefinition != null;
        public bool CampaignComplete { get; private set; }
        public bool RecoveredInvalidSave { get; private set; }
        public string SaveRecoveryError { get; private set; }

        public event Action<CareerProgress> ProgressChanged;
        public event Action<CareerNodeDefinition> ActiveEventChanged;
        public event Action<CareerEventSettlement> SettlementReady;
        public event Action CampaignCompleted;

        public void Configure(
            RaceRoundController roundController,
            RaceDirector director,
            RacePerformanceMetricsTracker performanceTracker,
            ICareerProgressStorage storage)
        {
            if (roundController == null) throw new ArgumentNullException(nameof(roundController));
            if (director == null) throw new ArgumentNullException(nameof(director));
            if (storage == null) throw new ArgumentNullException(nameof(storage));

            Unbind();
            round = roundController;
            race = director;
            performance = performanceTracker;
            store = new CareerProgressStore(storage);
            definitions = ChapterOneCareerEventContent.CreateDefinitions();

            var load = store.Load();
            Progress = load.Progress;
            RecoveredInvalidSave = load.RecoveredFromInvalidPayload;
            SaveRecoveryError = load.Error;
            LastSettlement = null;

            activeDefinition = FindFirstPlayableIncomplete();
            CampaignComplete = activeDefinition == null && AreAllNodesCompleted();
            if (activeDefinition != null)
                BindAdapter(activeDefinition);

            round.ResultsReady += OnResultsReady;
            configured = true;
        }

        public bool RestartCurrentEvent()
        {
            EnsureConfigured();
            if (activeDefinition == null)
                return false;
            return race.RestartRace();
        }

        public bool TryAdvanceToNextEvent()
        {
            EnsureConfigured();
            if (CampaignComplete || activeDefinition == null || race.Phase != RaceRoundPhase.Results)
                return false;
            if (!Progress.IsNodeCompleted(activeDefinition.Node.Id))
                return false;

            var next = FindFirstPlayableIncomplete();
            if (next == null)
            {
                MarkCampaignCompleteIfNeeded();
                return false;
            }

            adapter?.Dispose();
            adapter = null;
            if (!race.RestartRace())
            {
                BindAdapter(activeDefinition);
                return false;
            }

            activeDefinition = next;
            LastSettlement = null;
            BindAdapter(activeDefinition);
            ActiveEventChanged?.Invoke(activeDefinition);
            return true;
        }

        public void SaveNow()
        {
            EnsureConfigured();
            store.Save(Progress);
        }

        private void OnResultsReady(float finishTime)
        {
            if (!configured || activeDefinition == null || Progress == null)
                return;

            var outcome = new CareerEventOutcome(
                finished: true,
                restartCount: adapter?.RestartCount ?? 0,
                finishTimeSeconds: Math.Max(0d, finishTime),
                finalPosition: Math.Max(1, race.Position),
                driftScore: performance?.DriftScore ?? 0);

            var settlement = settlementService.Settle(activeDefinition, outcome, Progress);
            LastSettlement = settlement;

            if (!ReferenceEquals(settlement.Progress, Progress))
            {
                Progress = settlement.Progress;
                store.Save(Progress);
                ProgressChanged?.Invoke(Progress);
            }

            MarkCampaignCompleteIfNeeded();
            SettlementReady?.Invoke(settlement);
        }

        private void MarkCampaignCompleteIfNeeded()
        {
            if (CampaignComplete || !AreAllNodesCompleted())
                return;
            CampaignComplete = true;
            CampaignCompleted?.Invoke();
        }

        private void BindAdapter(CareerNodeDefinition definition)
        {
            var metrics = new RaceDirectorCareerMetricsSource(race, performance);
            adapter = new RaceRoundCareerSessionAdapter(round, definition, metrics);
        }

        private CareerNodeDefinition FindFirstPlayableIncomplete()
        {
            if (definitions == null || Progress == null)
                return null;

            for (var index = 0; index < definitions.Count; index++)
            {
                var definition = definitions[index];
                if (Progress.IsNodeCompleted(definition.Node.Id))
                    continue;
                if (progression.CanEnter(definition.Node, Progress))
                    return definition;
            }

            return null;
        }

        private bool AreAllNodesCompleted()
        {
            if (definitions == null || Progress == null)
                return false;
            for (var index = 0; index < definitions.Count; index++)
                if (!Progress.IsNodeCompleted(definitions[index].Node.Id))
                    return false;
            return definitions.Count > 0;
        }

        private void EnsureConfigured()
        {
            if (!configured || round == null || race == null || store == null || Progress == null)
                throw new InvalidOperationException("CareerGameSession must be configured before use.");
        }

        private void Unbind()
        {
            if (round != null)
                round.ResultsReady -= OnResultsReady;
            adapter?.Dispose();
            adapter = null;
            configured = false;
        }

        private void OnDestroy()
        {
            Unbind();
        }
    }
}
