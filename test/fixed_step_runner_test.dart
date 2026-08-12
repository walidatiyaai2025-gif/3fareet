import 'package:afareet_asphalt/game/core/fixed_step_runner.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  test('fixed step runner consumes deterministic steps', () {
    final runner = FixedStepRunner(stepSeconds: 0.02, maxCatchUpSteps: 4);
    var simulated = 0.0;

    final steps = runner.advance(0.05, (step) => simulated += step);

    expect(steps, 2);
    expect(simulated, closeTo(0.04, 0.000001));
    expect(runner.interpolationAlpha, closeTo(0.5, 0.000001));
  });

  test('fixed step runner caps catch-up work', () {
    final runner = FixedStepRunner(stepSeconds: 0.01, maxCatchUpSteps: 3);
    var calls = 0;

    final steps = runner.advance(1, (_) => calls += 1);

    expect(steps, 3);
    expect(calls, 3);
  });
}
