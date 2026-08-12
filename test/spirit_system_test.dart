import 'package:afareet_asphalt/game/drift/spirit_system.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  test('Spirit charge rejects low-speed drift abuse', () {
    final spirit = SpiritSystem();

    spirit.step(
      dt: 2,
      isDrifting: true,
      speedMps: 4,
      driftIntensity: 1,
      nitroPressed: false,
    );

    expect(spirit.energy, 0);
    expect(spirit.meterState, SpiritMeterState.empty);
  });

  test('Spirit charges with drift and exposes three feedback levels', () {
    final spirit = SpiritSystem();

    spirit.step(
      dt: 1,
      isDrifting: true,
      speedMps: 20,
      driftIntensity: 0.2,
      nitroPressed: false,
    );
    expect(spirit.driftFeedback, DriftFeedbackLevel.spark);

    spirit.step(
      dt: 1,
      isDrifting: true,
      speedMps: 20,
      driftIntensity: 0.5,
      nitroPressed: false,
    );
    expect(spirit.driftFeedback, DriftFeedbackLevel.arc);

    spirit.step(
      dt: 1,
      isDrifting: true,
      speedMps: 20,
      driftIntensity: 0.9,
      nitroPressed: false,
    );
    expect(spirit.driftFeedback, DriftFeedbackLevel.supernatural);
    expect(spirit.energy, greaterThan(0));
  });

  test('Nitro consumes energy fires hooks and applies cooldown', () {
    final trailStates = <bool>[];
    final cameraStates = <bool>[];
    final audioCues = <String>[];
    final spirit = SpiritSystem(
      hooks: SpiritFeedbackHooks(
        onTrailState: trailStates.add,
        onCameraBoost: cameraStates.add,
        onAudioCue: audioCues.add,
      ),
    );
    spirit.energy = 60;

    final acceleration = spirit.step(
      dt: 0.1,
      isDrifting: false,
      speedMps: 20,
      driftIntensity: 0,
      nitroPressed: true,
    );
    expect(acceleration, greaterThan(0));
    expect(spirit.nitroActive, isTrue);
    expect(spirit.meterState, SpiritMeterState.boosting);

    spirit.step(
      dt: 0.1,
      isDrifting: false,
      speedMps: 20,
      driftIntensity: 0,
      nitroPressed: false,
    );
    expect(spirit.nitroActive, isFalse);
    expect(spirit.cooldownRemaining, greaterThan(0));
    expect(trailStates, <bool>[true, false]);
    expect(cameraStates, <bool>[true, false]);
    expect(audioCues, <String>['nitro_start', 'nitro_end']);
  });
}
