using System;
using System.Collections.Generic;

namespace Afareet.Progression
{
    public sealed class CareerEventOutcome
    {
        public bool Finished { get; }
        public int RestartCount { get; }
        public double? FinishTimeSeconds { get; }
        public int? FinalPosition { get; }
        public int DriftScore { get; }

        public CareerEventOutcome(
            bool finished,
            int restartCount,
            double? finishTimeSeconds = null,
            int? finalPosition = null,
            int driftScore = 0)
        {
            if (restartCount < 0) throw new ArgumentOutOfRangeException(nameof(restartCount));
            if (finishTimeSeconds.HasValue &&
                (double.IsNaN(finishTimeSeconds.Value) || double.IsInfinity(finishTimeSeconds.Value) || finishTimeSeconds.Value < 0d))
                throw new ArgumentOutOfRangeException(nameof(finishTimeSeconds));
            if (finalPosition.HasValue && finalPosition.Value < 1) throw new ArgumentOutOfRangeException(nameof(finalPosition));
            if (driftScore < 0) throw new ArgumentOutOfRangeException(nameof(driftScore));

            Finished = finished;
            RestartCount = restartCount;
            FinishTimeSeconds = finishTimeSeconds;
            FinalPosition = finalPosition;
            DriftScore = driftScore;
        }
    }

    public sealed class CareerObjectiveEvaluationEntry
    {
        public string ObjectiveId { get; }
        public double Value { get; }
        public double Target { get; }
        public bool IsComplete => Value >= Target;

        public CareerObjectiveEvaluationEntry(string objectiveId, double value, double target)
        {
            if (string.IsNullOrWhiteSpace(objectiveId)) throw new ArgumentException("Career objective evaluation id is required.", nameof(objectiveId));
            if (double.IsNaN(value) || double.IsInfinity(value) || value < 0d) throw new ArgumentOutOfRangeException(nameof(value));
            if (double.IsNaN(target) || double.IsInfinity(target) || target <= 0d) throw new ArgumentOutOfRangeException(nameof(target));
            ObjectiveId = objectiveId;
            Value = value;
            Target = target;
        }
    }

    public sealed class CareerObjectiveEvaluation
    {
        private readonly IReadOnlyList<CareerObjectiveEvaluationEntry> entries;
        public IReadOnlyList<CareerObjectiveEvaluationEntry> Entries => entries;
        public int CompletedCount { get; }
        public bool AllCompleted => entries.Count > 0 && CompletedCount == entries.Count;

        public CareerObjectiveEvaluation(IEnumerable<CareerObjectiveEvaluationEntry> entries)
        {
            if (entries == null) throw new ArgumentNullException(nameof(entries));
            var list = new List<CareerObjectiveEvaluationEntry>(entries);
            if (list.Count == 0) throw new ArgumentException("Career objective evaluation requires at least one entry.", nameof(entries));

            var completedCount = 0;
            var ids = new HashSet<string>(StringComparer.Ordinal);
            for (var index = 0; index < list.Count; index++)
            {
                var entry = list[index];
                if (entry == null) throw new ArgumentException("Career objective evaluation contains a null entry.", nameof(entries));
                if (!ids.Add(entry.ObjectiveId)) throw new ArgumentException($"Duplicate Career objective evaluation id '{entry.ObjectiveId}'.", nameof(entries));
                if (entry.IsComplete) completedCount++;
            }

            this.entries = list.AsReadOnly();
            CompletedCount = completedCount;
        }
    }

    public static class CareerObjectiveEvaluationPolicy
    {
        private const double BinaryObjectiveTarget = 1d;

        public static CareerObjectiveEvaluation Evaluate(CareerNodeDefinition definition, CareerEventOutcome outcome)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (outcome == null) throw new ArgumentNullException(nameof(outcome));

            // Keep these explicit definition-based ids as stable source-contract markers.
            var finishId = $"finish_{definition.Node.Id}";
            var cleanId = $"clean_{definition.Node.Id}";
            var node = definition.Node;
            var timeId = $"time_{node.Id}";
            var driftId = $"drift_{node.Id}";
            var winId = $"win_{node.Id}";
            var entries = new List<CareerObjectiveEvaluationEntry>(definition.Objectives.Count);

            for (var index = 0; index < definition.Objectives.Count; index++)
            {
                var objective = definition.Objectives[index];
                if (objective.Target != BinaryObjectiveTarget)
                    throw new InvalidOperationException($"Career objective '{objective.Id}' uses unsupported non-binary target {objective.Target}.");

                double value;
                if (StringComparer.Ordinal.Equals(objective.Id, finishId))
                    value = outcome.Finished ? 1d : 0d;
                else if (StringComparer.Ordinal.Equals(objective.Id, cleanId))
                    value = outcome.Finished && outcome.RestartCount == 0 ? 1d : 0d;
                else if (StringComparer.Ordinal.Equals(objective.Id, timeId))
                {
                    if (!node.TargetTimeSeconds.HasValue) throw new InvalidOperationException($"Time objective requires target time for node '{node.Id}'.");
                    value = outcome.Finished && outcome.FinishTimeSeconds.HasValue && outcome.FinishTimeSeconds.Value <= node.TargetTimeSeconds.Value ? 1d : 0d;
                }
                else if (StringComparer.Ordinal.Equals(objective.Id, driftId))
                {
                    if (!node.TargetDriftScore.HasValue) throw new InvalidOperationException($"Drift objective requires target score for node '{node.Id}'.");
                    value = outcome.Finished && outcome.DriftScore >= node.TargetDriftScore.Value ? 1d : 0d;
                }
                else if (StringComparer.Ordinal.Equals(objective.Id, winId))
                    value = outcome.Finished && outcome.FinalPosition == 1 ? 1d : 0d;
                else
                    throw new InvalidOperationException($"Career objective '{objective.Id}' is not supported for node '{node.Id}'.");

                entries.Add(new CareerObjectiveEvaluationEntry(objective.Id, value, objective.Target));
            }

            return new CareerObjectiveEvaluation(entries);
        }
    }
}
