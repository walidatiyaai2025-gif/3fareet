typedef FixedStepCallback = void Function(double stepSeconds);

class FixedStepRunner {
  FixedStepRunner({
    required this.stepSeconds,
    required this.maxCatchUpSteps,
  })  : assert(stepSeconds > 0),
        assert(maxCatchUpSteps > 0);

  final double stepSeconds;
  final int maxCatchUpSteps;

  double _accumulator = 0;

  double get interpolationAlpha => (_accumulator / stepSeconds).clamp(0.0, 1.0);

  int advance(double frameDeltaSeconds, FixedStepCallback onStep) {
    final clampedDelta = frameDeltaSeconds.clamp(0.0, stepSeconds * maxCatchUpSteps);
    _accumulator += clampedDelta;

    var steps = 0;
    while (_accumulator >= stepSeconds && steps < maxCatchUpSteps) {
      onStep(stepSeconds);
      _accumulator -= stepSeconds;
      steps += 1;
    }

    if (steps == maxCatchUpSteps && _accumulator >= stepSeconds) {
      _accumulator %= stepSeconds;
    }

    return steps;
  }

  void reset() {
    _accumulator = 0;
  }
}
