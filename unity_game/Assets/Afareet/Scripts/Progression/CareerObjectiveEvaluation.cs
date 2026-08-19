using System;
using System.Collections.Generic;

namespace Afareet.Progression
{
    public sealed class CareerEventOutcome
    {
        public bool Finished { get; }
        public int RestartCount { get; }

        public CareerEventOutcome(bool finished, int restartCount)
        {
            if (restartCount < 0)
                throw new ArgumentOutOfRangeException(nameof(restartCount));

            Finished = finished;
            RestartCount = restartCount;
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
            if (string.IsNullOrWhiteSpace(objectiveId))
                throw new ArgumentException("Career objective evaluation id is required.", nameof(objectiveId));
            if (double.IsNaN(value) || double.IsInfinity(value) || value < 0d)
                throw new ArgumentOutOfRangeException(nameof(value));
            if (double.IsNaN(target) || double.IsInfinity(target) || target <= 0d)
                throw new ArgumentOutOfRangeException(nameof(target));

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
            if (entries == null)
                throw new ArgumentNullException(nameof(entries));

            var list = new List<CareerObjectiveEvaluationEntry>(entries);
            if (list.Count == 0)
                throw new ArgumentException("Career objective evaluation requires at least one entry.", nameof(entries));

            var completedCount = 0;
            var ids = new HashSet<string>(StringComparer.Ordinal);
            for (var index = 0; index < list.Count; index++)
            {
                var entry = list[index];
                if (entry == null)
                    throw new ArgumentException("Career objective evaluation contains a null entry.", nameof(entries));
                if (!ids.Add(entry.ObjectiveId))
                    throw new ArgumentException($"Duplicate Career objective evaluation id '{entry.ObjectiveId}'.", nameof(entries));
                if (entry.IsComplete)
                    completedCount++;
            }

            this.entries = list.AsReadOnly();
            CompletedCount = completedCount;
        }
    }

    public static class CareerObjectiveEvaluationPolicy
    {
        private const double BinaryObjectiveTarget = 1d;

        public static CareerObjectiveEvaluation Evaluate(
            CareerNodeDefinition definition,
            CareerEventOutcome outcome)
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));
            if (outcome == null)
                throw new ArgumentNullException(nameof(outcome));

            var finishId = $"finish_{definition.Node.Id}";
            var cleanId = $"clean_{definition.Node.Id}";
            var entries = new List<CareerObjectiveEvaluationEntry>(definition.Objectives.Count);

            for (var index = 0; index < definition.Objectives.Count; index++)
            {
                var objective = definition.Objectives[index];
                if (objective.Target != BinaryObjectiveTarget)
                    throw new InvalidOperationException(
                        $"Career objective '{objective.Id}' uses unsupported non-binary target {objective.Target}.");

                double value;
                if (StringComparer.Ordinal.Equals(objective.Id, finishId))
                {
                    value = outcome.Finished ? 1d : 0d;
                }
                else if (StringComparer.Ordinal.Equals(objective.Id, cleanId))
                {
                    value = outcome.Finished && outcome.RestartCount == 0 ? 1d : 0d;
                }
                else
                {
                    throw new InvalidOperationException(
                        $"Career objective '{objective.Id}' is not supported for node '{definition.Node.Id}'.");
                }

                entries.Add(new CareerObjectiveEvaluationEntry(
                    objective.Id,
                    value,
                    objective.Target));
            }

            return new CareerObjectiveEvaluation(entries);
        }
    }
}
