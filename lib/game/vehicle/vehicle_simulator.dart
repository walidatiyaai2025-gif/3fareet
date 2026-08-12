import 'dart:math' as math;

import 'package:afareet_asphalt/game/input/game_input.dart';
import 'package:afareet_asphalt/game/vehicle/vehicle_definition.dart';
import 'package:afareet_asphalt/game/vehicle/vehicle_state.dart';

class VehicleSimulator {
  VehicleSimulator({
    required this.definition,
    VehicleState? state,
  }) : state = state ?? VehicleState();

  VehicleDefinition definition;
  final VehicleState state;

  void step(
    GameInputSnapshot input,
    double dt, {
    double nitroAccelerationMps2 = 0,
  }) {
    if (dt <= 0) {
      return;
    }

    _updateLongitudinalSpeed(input, dt, nitroAccelerationMps2);
    _updateDriftAndSlip(input, dt);
    _updateHeadingAndPosition(input, dt);
  }

  void _updateLongitudinalSpeed(
    GameInputSnapshot input,
    double dt,
    double nitroAccelerationMps2,
  ) {
    var speed = state.speedMps;

    if (input.throttle > 0) {
      if (speed < 0) {
        speed += definition.brakeDecelerationMps2 * input.throttle * dt;
      } else {
        speed += definition.accelerationMps2 * input.throttle * dt;
      }
    }

    if (input.brake > 0) {
      if (speed > 0.25) {
        speed -= definition.brakeDecelerationMps2 * input.brake * dt;
      } else {
        speed -= definition.reverseAccelerationMps2 * input.brake * dt;
      }
    }

    if (input.throttle == 0 && input.brake == 0) {
      speed = _moveTowards(
        speed,
        0,
        definition.coastDecelerationMps2 * dt,
      );
    }

    if (state.isOffTrack) {
      speed = _moveTowards(
        speed,
        0,
        definition.offTrackDecelerationMps2 * dt,
      );
    }

    if (speed >= 0 && nitroAccelerationMps2 > 0) {
      speed += nitroAccelerationMps2 * dt;
    }

    state.speedMps = speed.clamp(
      -definition.maxReverseSpeedMps,
      definition.maxForwardSpeedMps,
    );
  }

  void _updateDriftAndSlip(GameInputSnapshot input, double dt) {
    final wasDrifting = state.isDrifting;
    final speed = state.speedMps.abs();
    final steeringMagnitude = input.steering.abs();

    if (!state.isDrifting &&
        input.drift &&
        speed >= definition.driftEntryMinSpeedMps &&
        steeringMagnitude >= 0.12) {
      state.isDrifting = true;
    }

    if (state.isDrifting &&
        (!input.drift || speed < definition.driftExitMinSpeedMps)) {
      state.isDrifting = false;
    }

    if (state.isDrifting) {
      state.lateralSlipMps +=
          input.steering * definition.driftSlipBuildMps2 * dt;
      state.lateralSlipMps = _moveTowards(
        state.lateralSlipMps,
        0,
        definition.driftGripRecoveryPerSecond * dt,
      );
    } else {
      final recovery = definition.gripRecoveryPerSecond +
          (wasDrifting ? definition.driftExitStabilizationPerSecond : 0);
      state.lateralSlipMps = _moveTowards(
        state.lateralSlipMps,
        0,
        recovery * dt,
      );
    }

    state.lateralSlipMps = state.lateralSlipMps.clamp(
      -definition.maxLateralSlipMps,
      definition.maxLateralSlipMps,
    );
  }

  void _updateHeadingAndPosition(GameInputSnapshot input, double dt) {
    final speedRatio = (state.speedMps.abs() /
            definition.steeringHighSpeedStartMps)
        .clamp(0.0, 1.0);
    final steeringRate = _lerp(
      definition.steeringRateLowSpeedRad,
      definition.steeringRateHighSpeedRad,
      speedRatio,
    );
    final direction = state.speedMps >= 0 ? 1.0 : -0.65;
    final driftYaw = state.isDrifting ? state.lateralSlipMps * 0.018 : 0.0;

    state.headingRadians +=
        ((input.steering * steeringRate * direction) + driftYaw) * dt;

    final forwardX = math.cos(state.headingRadians);
    final forwardY = math.sin(state.headingRadians);
    final rightX = -forwardY;
    final rightY = forwardX;

    state.x +=
        ((forwardX * state.speedMps) + (rightX * state.lateralSlipMps)) * dt;
    state.y +=
        ((forwardY * state.speedMps) + (rightY * state.lateralSlipMps)) * dt;
  }

  void applyCollision(double severity) {
    final clampedSeverity = severity.clamp(0.0, 1.0);
    final retainedFraction = 1 -
        ((1 - definition.collisionSpeedRetention) * clampedSeverity);
    state.speedMps *= retainedFraction;
    state.lateralSlipMps *= -0.35 * clampedSeverity;
    state.isDrifting = false;
  }

  void setOffTrack(bool value) {
    state.isOffTrack = value;
  }

  void resetToSafePoint(VehicleSafePoint safePoint) {
    state.resetTo(safePoint);
  }

  static double _moveTowards(double value, double target, double delta) {
    if (value < target) {
      return math.min(value + delta, target);
    }
    return math.max(value - delta, target);
  }

  static double _lerp(double a, double b, double t) => a + ((b - a) * t);
}
