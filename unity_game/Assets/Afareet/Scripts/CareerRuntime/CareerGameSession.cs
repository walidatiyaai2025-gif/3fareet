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
        private readonly CareerNavigationService navigationService = new CareerNavigationService();

        private IReadOnlyList<CareerNodeDefinition> definitions;
        private CareerMap navigationMap;
        private CareerPlayerProfileStore profileStore;
        private ICareerTrackRuntime trackRuntime;
        private ICareerBossVehicleRuntime bossVehicleRuntime;
        private RaceRoundController round;
        private RaceDirector race;
        private RacePerformanceMetricsTracker performance;
        private RaceRoundCareerSessionAdapter adapter;
        private CareerNodeDefinition activeDefinition;
        private bool configured;

        public CareerPlayerProfile Profile { get; private set; }
        public CareerProgress Progress => Profile?.Career;
        public CareerNodeDefinition ActiveDefinition => activeDefinition;
        public CareerEventSettlement LastSettlement { get; private set; }
        public RaceRewardSettlement LastCoinRewardSettlement { get; private set; }
        public CareerNavigationSnapshot Navigation { get; private set; }
        public RaceChallengeConfiguration ActiveChallengeConfiguration => race?.ChallengeConfiguration ?? RaceChallengeConfiguration.Standard;
        public string ActiveTrackId => trackRuntime?.ActiveTrackId;
        public string ActiveBossVehicleId => bossVehicleRuntime?.ActiveBossVehicleId;
        public bool HasActiveEvent => activeDefinition != null;
        public bool CampaignComplete { get; private set; }
        public bool RecoveredInvalidSave { get; private set; }
        public string SaveRecoveryError { get; private set; }

        public event Action<CareerProgress> ProgressChanged;
        public event Action<CareerNodeDefinition> ActiveEventChanged;
        public event Action<CareerNavigationSnapshot> NavigationChanged;
        public event Action<CareerEventSettlement> SettlementReady;
        public event Action CampaignCompleted;

        public void Configure(
            RaceRoundController roundController,
            RaceDirector director,
            RacePerformanceMetricsTracker performanceTracker,
            ICareerProgressStorage storage)
        {
            Configure(
                roundController,
                director,
                performanceTracker,
                storage,
                new PassiveCareerTrackRuntime(),
                new PassiveCareerBossVehicleRuntime());
        }

        public void Configure(
            RaceRoundController roundController,
            RaceDirector director,
            RacePerformanceMetricsTracker performanceTracker,
            ICareerProgressStorage storage,
            ICareerTrackRuntime careerTrackRuntime)
        {
            Configure(
                roundController,
                director,
                performanceTracker,
                storage,
                careerTrackRuntime,
                new PassiveCareerBossVehicleRuntime());
        }

        public void Configure(
            RaceRoundController roundController,
            RaceDirector director,
            RacePerformanceMetricsTracker performanceTracker,
            ICareerProgressStorage storage,
            ICareerTrackRuntime careerTrackRuntime,
            ICareerBossVehicleRuntime careerBossVehicleRuntime)
        {
            if (roundController == null) throw new ArgumentNullException(nameof(roundController));
            if (director == null) throw new ArgumentNullException(nameof(director));
            if (storage == null) throw new ArgumentNullException(nameof(storage));
            if (careerTrackRuntime == null) throw new ArgumentNullException(nameof(careerTrackRuntime));
            if (careerBossVehicleRuntime == null) throw new ArgumentNullException(nameof(careerBossVehicleRuntime));

            Unbind();
            round = roundController;
            race = director;
            performance = performanceTracker;
            trackRuntime = careerTrackRuntime;
            bossVehicleRuntime = careerBossVehicleRuntime;
            profileStore = new CareerPlayerProfileStore(storage);
            var chapter = ChapterOneCareerContent.CreateFoundation();
            definitions = ChapterOneCareerEventContent.CreateDefinitions();
            navigationMap = new CareerMap(new[] { chapter });

            var load = profileStore.Load();
            Profile = load.Profile;
            RecoveredInvalidSave = load.RecoveredFromInvalidPayload;
            SaveRecoveryError = load.Error;
            LastSettlement = null;
            LastCoinRewardSettlement = null;

            activeDefinition = FindFirstPlayableIncomplete();
            CampaignComplete = activeDefinition == null && AreAllNodesCompleted();
            Navigation = navigationService.Build(navigationMap, Progress, activeDefinition?.Node.Id);
            if (activeDefinition != null)
            {
                trackRuntime.ApplyTrack(activeDefinition.Node.TrackId);
                ApplyChallengeConfiguration(activeDefinition);
                ApplyBossVehicleConfiguration(activeDefinition);
                BindAdapter(activeDefinition);
            }
            else
            {
                bossVehicleRuntime.ClearBossVehicle();
            }

            round.ResultsReady += OnResultsReady;
            configured = true;
        }

        public bool RestartCurrentEvent()
        {
            EnsureConfigured();
            if (activeDefinition == null)
                return false;

            var restarted = race.RestartRace();
            if (restarted)
            {
                LastSettlement = null;
                LastCoinRewardSettlement = null;
            }
            return restarted;
        }

        public CareerNavigationSnapshot SelectCareerNode(string nodeId)
        {
            EnsureConfigured();
            SetNavigation(navigationService.Select(Navigation, nodeId));
            return Navigation;
        }

        public CareerNavigationSnapshot MoveCareerSelection(int delta)
        {
            EnsureConfigured();
            SetNavigation(navigationService.Move(Navigation, delta));
            return Navigation;
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
            var previousChallenge = race.ChallengeConfiguration;
            var previousTrackId = trackRuntime.ActiveTrackId;
            var previousBossVehicleId = bossVehicleRuntime.ActiveBossVehicleId;
            var trackChanged = trackRuntime.ApplyTrack(next.Node.TrackId);
            ApplyChallengeConfiguration(next);
            ApplyBossVehicleConfiguration(next);

            var advanced = trackChanged
                ? StartFreshRaceAfterTrackChange()
                : race.RestartRace();
            if (!advanced)
            {
                if (trackChanged && !string.IsNullOrWhiteSpace(previousTrackId))
                    trackRuntime.ApplyTrack(previousTrackId);
                RestoreBossVehicle(previousBossVehicleId);
                race.ApplyChallengeConfiguration(previousChallenge);
                BindAdapter(activeDefinition);
                return false;
            }

            activeDefinition = next;
            LastSettlement = null;
            LastCoinRewardSettlement = null;
            BindAdapter(activeDefinition);
            RefreshNavigation(activeDefinition.Node.Id);
            ActiveEventChanged?.Invoke(activeDefinition);
            return true;
        }

        public void SaveNow()
        {
            EnsureConfigured();
            profileStore.Save(Profile);
        }

        private bool StartFreshRaceAfterTrackChange()
        {
            if (race.Phase != RaceRoundPhase.Ready)
                return false;
            race.StartRace();
            return race.Phase == RaceRoundPhase.Countdown;
        }

        private void OnResultsReady(float finishTime)
        {
            if (!configured || activeDefinition == null || Progress == null)
                return;

            var outcome = new CareerEventOutcome(
                finished: !race.WasPlayerEliminated,
                restartCount: adapter?.RestartCount ?? 0,
                finishTimeSeconds: Math.Max(0d, finishTime),
                finalPosition: Math.Max(1, race.Position),
                driftScore: performance?.DriftScore ?? 0);

            var settlement = settlementService.Settle(activeDefinition, outcome, Progress);
            LastSettlement = settlement;
            LastCoinRewardSettlement = null;

            if (!ReferenceEquals(settlement.Progress, Progress) || settlement.GrantedAnyReward)
            {
                var selectedNodeId = Navigation?.SelectedNodeId;
                var settledCoinsGranted = settlement.CoinsGranted;
                if (settlement.CoinsGranted > 0 && outcome.Finished)
                {
                    LastCoinRewardSettlement = race.SettlePlayerFinishReward(settlement.CoinsGranted);
                    settledCoinsGranted = LastCoinRewardSettlement.SettledRewardUnits;
                }

                Profile = CareerPlayerProfileRewardApplication.ApplyWithSettledCoins(
                    Profile,
                    settlement,
                    settledCoinsGranted);
                RefreshNavigation(selectedNodeId);
                profileStore.Save(Profile);
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

        private void ApplyChallengeConfiguration(CareerNodeDefinition definition)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            race.ApplyChallengeConfiguration(CareerChallengeBalancePolicy.Resolve(definition.Node));
        }

        private void ApplyBossVehicleConfiguration(CareerNodeDefinition definition)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (string.IsNullOrWhiteSpace(definition.Node.BossVehicleId))
            {
                bossVehicleRuntime.ClearBossVehicle();
                return;
            }
            bossVehicleRuntime.ApplyBossVehicle(definition.Node.BossVehicleId);
        }

        private void RestoreBossVehicle(string previousBossVehicleId)
        {
            if (string.IsNullOrWhiteSpace(previousBossVehicleId))
                bossVehicleRuntime.ClearBossVehicle();
            else
                bossVehicleRuntime.ApplyBossVehicle(previousBossVehicleId);
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

        private void RefreshNavigation(string preferredNodeId)
        {
            SetNavigation(navigationService.Build(navigationMap, Progress, preferredNodeId));
        }

        private void SetNavigation(CareerNavigationSnapshot next)
        {
            if (next == null) throw new ArgumentNullException(nameof(next));
            if (ReferenceEquals(Navigation, next))
                return;
            Navigation = next;
            NavigationChanged?.Invoke(Navigation);
        }

        private void EnsureConfigured()
        {
            if (!configured || round == null || race == null || profileStore == null || Profile == null ||
                navigationMap == null || Navigation == null || trackRuntime == null || bossVehicleRuntime == null)
            {
                throw new InvalidOperationException("CareerGameSession must be configured before use.");
            }
        }

        private void Unbind()
        {
            if (round != null)
                round.ResultsReady -= OnResultsReady;
            adapter?.Dispose();
            adapter = null;
            configured = false;
            navigationMap = null;
            Navigation = null;
            trackRuntime = null;
            bossVehicleRuntime = null;
        }

        private void OnDestroy()
        {
            Unbind();
        }
    }
}
