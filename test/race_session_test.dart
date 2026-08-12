import 'package:afareet_asphalt/game/input/game_input.dart';
import 'package:afareet_asphalt/game/race/race_controller.dart';
import 'package:afareet_asphalt/game/race/race_session.dart';
import 'package:afareet_asphalt/game/race/track_definition.dart';
import 'package:afareet_asphalt/game/vehicle/vehicle_definition.dart';
import 'package:afareet_asphalt/game/vehicle/vehicle_state.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  test('session completes deterministic one-lap race and restart resets state', () {
    final track = TrackDefinition(
      id: 'unit_track',
      totalLengthMeters: 60,
      halfWidthMeters: 8,
      totalLaps: 1,
      startGrid: const <StartGridSlot>[
        StartGridSlot(x: 0, y: 0, headingRadians: 0),
      ],
      checkpoints: const <TrackCheckpoint>[
        TrackCheckpoint(
          id: 'a',
          distanceMeters: 20,
          safePoint: VehicleSafePoint(x: 20, y: 0, headingRadians: 0),
        ),
        TrackCheckpoint(
          id: 'b',
          distanceMeters: 40,
          safePoint: VehicleSafePoint(x: 40, y: 0, headingRadians: 0),
        ),
      ],
    );
    final session = RaceSession(
      vehicleDefinition: PrototypeVehiclePreset.definition,
      track: track,
    )..restart();

    session.race.step(3.1);
    const throttle = GameInputSnapshot(
      steering: 0,
      throttle: 1,
      brake: 0,
      drift: false,
      nitro: false,
      pausePressed: false,
    );

    for (var i = 0; i < 600 && session.race.phase != RacePhase.finished; i += 1) {
      session.step(throttle, 1 / 60);
    }

    expect(session.race.phase, RacePhase.finished);
    expect(session.race.lapsCompleted, 1);
    expect(session.race.result, isNotNull);

    session.spirit.energy = 50;
    session.restart();
    expect(session.race.phase, RacePhase.countdown);
    expect(session.distanceAlongLapMeters, 0);
    expect(session.vehicle.state.speedMps, 0);
    expect(session.spirit.energy, 0);
  });
}
