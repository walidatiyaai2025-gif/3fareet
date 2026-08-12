typedef FixedStepCallback = void Function(double stepSeconds);

class FixedStepRunner {
  FixedStepRunner({
    required this.stepSeconds,
    required this.maxCatchUpSteps,
  })  : assert(stepSeconds > 0),
        assert(maxCatchUpSteps > 0);

  static const double _stepEpsilon = 1e-9;

  final double stepSeconds;
  final int maxCatchUpSteps;

  double _accumulator = 0;

  double get interpolationAlpha =>
      (_accumulator / stepSeconds).clamp(0.0, 1.0).toDouble();

  int advance(double frameDeltaSeconds, FixedStepCallback onStep) {
    final clampedDelta = frameDeltaSeconds
        .clamp(0.0, stepSeconds * maxCatchUpSteps)
        .toDouble();
    _accumulator += clampedDelta;

    final availableSteps =
        ((_accumulator / stepSeconds) + _stepEpsilon).floor();
    final stepsToRun = availableSteps < maxCatchUpSteps
        ? availableSteps
        : maxCatchUpSteps;

    for (var step = 0; step < stepsToRun; step += 1) {
      onStep(stepSeconds);
    }
    _accumulator -= stepSeconds * stepsToRun;

    final zeroTolerance = stepSeconds * _stepEpsilon;
    if (_accumulator.abs() <= zeroTolerance) {
      _accumulator = 0;
    }

    if (availableSteps > maxCatchUpSteps) {
      _accumulator %= stepSeconds;
    }

    return stepsToRun;
  }

  void reset() {
    _accumulator = 0;
  }
}
