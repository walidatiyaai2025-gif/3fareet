import 'package:afareet_asphalt/game/input/game_input.dart';
import 'package:afareet_asphalt/game/vehicle/vehicle_definition.dart';
import 'package:afareet_asphalt/game/vehicle/vehicle_simulator.dart';
import 'package:afareet_asphalt/game/vehicle/vehicle_state.dart';
import 'package:flutter_test/flutter_test.dart';

GameInputSnapshot input({
  double steering = 0,
  double throttle = 0,
  double brake = 0,
  bool drift = false,
  bool nitro = false,
}) {
  return GameInputSnapshot(
    steering: steering,
    throttle: throttle,
    brake: brake,
    drift: drift,
    nitro: nitro,
    pausePressed: false,
  );
}

void main() {
  test('throttle accelerates and max speed limiter clamps', () {
    final simulator = VehicleSimulator(
      definition: PrototypeVehiclePreset.definition,
    );

    for (var i = 0; i < 1000; i += 1) {
      simulator.step(input(throttle: 1), 1 / 60);
    }

    expect(simulator.state.speedMps, PrototypeVehiclePreset.definition.maxForwardSpeedMps);
  });

  test('brake slows forward motion then engages reverse', () {
    final simulator = VehicleSimulator(
      definition: PrototypeVehiclePreset.definition,
    );
    simulator.state.speedMps = 12;

    for (var i = 0; i < 80; i += 1) {
      simulator.step(input(brake: 1), 1 / 60);
    }

    expect(simulator.state.speedMps, lessThan(0));
    expect(
      simulator.state.speedMps,
      greaterThanOrEqualTo(-PrototypeVehiclePreset.definition.maxReverseSpeedMps),
    );
  });

  test('steering response softens at high speed', () {
    final low = VehicleSimulator(definition: PrototypeVehiclePreset.definition);
    final high = VehicleSimulator(definition: PrototypeVehiclePreset.definition);
    low.state.speedMps = 8;
    high.state.speedMps = 42;

    low.step(input(steering: 1), 0.1);
    high.step(input(steering: 1), 0.1);

    expect(low.state.headingRadians, greaterThan(high.state.headingRadians));
  });

  test('drift enters sustains slip and stabilizes on exit', () {
    final simulator = VehicleSimulator(
      definition: PrototypeVehiclePreset.definition,
    );
    simulator.state.speedMps = 20;

    simulator.step(input(steering: 1, drift: true), 0.2);
    final slipDuringDrift = simulator.state.lateralSlipMps.abs();
    expect(simulator.state.isDrifting, isTrue);
    expect(slipDuringDrift, greaterThan(0));

    simulator.step(input(), 0.2);
    expect(simulator.state.isDrifting, isFalse);
    expect(simulator.state.lateralSlipMps.abs(), lessThan(slipDuringDrift));
  });

  test('collision off-track slowdown and safe reset work', () {
    final simulator = VehicleSimulator(
      definition: PrototypeVehiclePreset.definition,
    );
    simulator.state
      ..speedMps = 30
      ..lateralSlipMps = 5;

    simulator.applyCollision(1);
    expect(simulator.state.speedMps, lessThan(30));

    final afterCollision = simulator.state.speedMps;
    simulator.setOffTrack(true);
    simulator.step(input(), 0.5);
    expect(simulator.state.speedMps, lessThan(afterCollision));

    simulator.resetToSafePoint(
      const VehicleSafePoint(x: 4, y: 7, headingRadians: 1.2),
    );
    expect(simulator.state.x, 4);
    expect(simulator.state.y, 7);
    expect(simulator.state.speedMps, 0);
  });
}
