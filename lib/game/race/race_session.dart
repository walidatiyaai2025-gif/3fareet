import 'package:afareet_asphalt/game/drift/spirit_system.dart';
import 'package:afareet_asphalt/game/input/game_input.dart';
import 'package:afareet_asphalt/game/race/race_controller.dart';
import 'package:afareet_asphalt/game/race/track_definition.dart';
import 'package:afareet_asphalt/game/vehicle/vehicle_definition.dart';
import 'package:afareet_asphalt/game/vehicle/vehicle_simulator.dart';

class RaceSession {
  RaceSession({
    required VehicleDefinition vehicleDefinition,
    required this.track,
    SpiritBalance spiritBalance = SpiritBalance.prototype,
  })  : vehicle = VehicleSimulator(definition: vehicleDefinition),
        spirit = SpiritSystem(balance: spiritBalance),
        race = RaceController(track: track);

  factory RaceSession.prototype({required VehicleDefinition vehicleDefinition}) {
    return RaceSession(
      vehicleDefinition: vehicleDefinition,
      track: TrackDefinition.cairoPrototype,
    );
  }

  final VehicleSimulator vehicle;
  final SpiritSystem spirit;
  final RaceController race;
  final TrackDefinition track;

  double distanceAlongLapMeters = 0;

  void step(GameInputSnapshot input, double dt) {
    race.step(dt);
    if (race.phase != RacePhase.racing) {
      return;
    }

    final driftIntensity = (vehicle.state.lateralSlipMps.abs() /
            vehicle.definition.maxLateralSlipMps)
        .clamp(0.0, 1.0)
        .toDouble();
    final nitroAcceleration = spirit.step(
      dt: dt,
      isDrifting: vehicle.state.isDrifting,
      speedMps: vehicle.state.speedMps.abs(),
      driftIntensity: driftIntensity,
      nitroPressed: input.nitro,
    );

    vehicle.step(
      input,
      dt,
      nitroAccelerationMps2: nitroAcceleration,
    );

    race.wrongWay = vehicle.state.speedMps < -0.5;
    if (vehicle.state.speedMps <= 0) {
      distanceAlongLapMeters =
          (distanceAlongLapMeters + (vehicle.state.speedMps * dt))
              .clamp(0.0, track.totalLengthMeters)
              .toDouble();
      return;
    }

    final previousDistance = distanceAlongLapMeters;
    var nextDistance = previousDistance + (vehicle.state.speedMps * dt);

    while (race.nextCheckpointIndex < track.checkpoints.length) {
      final checkpoint = track.checkpoints[race.nextCheckpointIndex];
      if (previousDistance < checkpoint.distanceMeters &&
          nextDistance >= checkpoint.distanceMeters) {
        race.registerCheckpoint(race.nextCheckpointIndex);
        continue;
      }
      break;
    }

    if (nextDistance >= track.totalLengthMeters) {
      final crossed = race.crossFinish();
      if (crossed && race.phase == RacePhase.racing) {
        nextDistance %= track.totalLengthMeters;
      } else if (race.phase == RacePhase.finished) {
        nextDistance = track.totalLengthMeters;
      }
    }

    distanceAlongLapMeters = nextDistance;
  }

  void restart() {
    race.restart();
    spirit.reset();
    distanceAlongLapMeters = 0;
    vehicle.resetToSafePoint(track.startGrid.first.toSafePoint());
  }

  void resetVehicleToSafePoint() {
    distanceAlongLapMeters = race.safeRespawnDistance();
    vehicle.resetToSafePoint(race.safeRespawnPoint());
  }
}
