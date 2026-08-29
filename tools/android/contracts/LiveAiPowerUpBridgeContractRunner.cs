using System;
using System.Linq;
using Afareet.Race;

internal static class LiveAiPowerUpBridgeContractRunner
{
    private static int Main()
    {
        try
        {
            PrototypeRulesContract();
            LiveSnapshotContract();
            EarlyRaceEstimateContract();
            ValidationContract();
            Console.WriteLine("Live AI power-up bridge behavior contract: PASS");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Live AI power-up bridge behavior contract: FAIL: {ex.Message}");
            return 1;
        }
    }

    private static void PrototypeRulesContract()
    {
        var rules = PowerUpRuntimeDefaults.CreatePrototypeRuleset().Snapshot();
        Require(rules.Count == 5, "prototype rules must cover all five power-up kinds");
        Require(rules.Select(value => value.Kind).Distinct().Count() == 5, "prototype rules must have unique kinds");

        var nitro = rules.Single(value => value.Kind == PowerUpKind.NitroSpirit);
        Require(nitro.InitialCharges == 2, "Nitro Spirit prototype must start with two charges");
        Require(nitro.TargetMode == PowerUpRuntimeTargetMode.Self, "Nitro Spirit must self-target");

        var traffic = rules.Single(value => value.Kind == PowerUpKind.TrafficCurse);
        Require(traffic.InitialCharges == 1, "Traffic Curse prototype must start with one charge");
        Require(traffic.TargetMode == PowerUpRuntimeTargetMode.Opponent, "Traffic Curse must opponent-target");
    }

    private static void LiveSnapshotContract()
    {
        var snapshot = AiPowerUpLiveSnapshotBuilder.Build(
            position: 2,
            fieldSize: 4,
            acceptedCheckpoints: 2,
            checkpointCount: 4,
            segmentProgress: .5d,
            ownSpeedKph: 72d,
            hasTargetAhead: true,
            targetDistanceMeters: 20d,
            targetSpeedKph: 60d,
            hasChaserBehind: true,
            chaserDistanceMeters: 10d,
            incomingHostilePressure: false,
            elapsedRaceSeconds: 50d);

        Require(Math.Abs(snapshot.NormalizedProgress - .625d) < .0001d, "normalized progress must combine checkpoint and segment progress");
        Require(Math.Abs(snapshot.SpeedRatio - 1.2d) < .0001d, "speed ratio must use target-ahead telemetry");
        Require(Math.Abs(snapshot.GapToTargetSeconds - 1d) < .0001d, "target gap must be distance over reference speed");
        Require(Math.Abs(snapshot.GapFromChaserSeconds - .5d) < .0001d, "chaser gap must be distance over reference speed");
        Require(Math.Abs(snapshot.RemainingRaceSeconds - 30d) < .0001d, "remaining time estimate must use observed progress pace");
    }

    private static void EarlyRaceEstimateContract()
    {
        var remaining = AiPowerUpLiveSnapshotBuilder.EstimateRemainingRaceSeconds(3d, .02d);
        Require(
            Math.Abs(remaining - AiPowerUpLiveSnapshotBuilder.UnknownRemainingRaceSeconds) < .0001d,
            "very early race progress must not fabricate a final-push remaining-time estimate");
    }

    private static void ValidationContract()
    {
        var threw = false;
        try
        {
            AiPowerUpLiveSnapshotBuilder.Build(
                position: 1,
                fieldSize: 2,
                acceptedCheckpoints: 5,
                checkpointCount: 4,
                segmentProgress: 0d,
                ownSpeedKph: 0d,
                hasTargetAhead: false,
                targetDistanceMeters: 0d,
                targetSpeedKph: 0d,
                hasChaserBehind: false,
                chaserDistanceMeters: 0d,
                incomingHostilePressure: false,
                elapsedRaceSeconds: 0d);
        }
        catch (ArgumentOutOfRangeException)
        {
            threw = true;
        }

        Require(threw, "invalid checkpoint telemetry must fail closed");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}
