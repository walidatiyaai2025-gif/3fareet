import 'dart:math' as math;

class CameraShakeSample {
  const CameraShakeSample({required this.x, required this.y});

  final double x;
  final double y;

  static const CameraShakeSample zero = CameraShakeSample(x: 0, y: 0);
}

class RacingCameraState {
  const RacingCameraState({
    required this.followDistanceMeters,
    required this.lateralOffsetMeters,
    required this.lookAheadMeters,
    required this.zoom,
    required this.fovDegrees,
    required this.rollRadians,
    required this.pitchDegrees,
    required this.shakeX,
    required this.shakeY,
  });

  static const RacingCameraState initial = RacingCameraState(
    followDistanceMeters: 0,
    lateralOffsetMeters: 0,
    lookAheadMeters: 8,
    zoom: 1,
    fovDegrees: 58,
    rollRadians: 0,
    pitchDegrees: 0,
    shakeX: 0,
    shakeY: 0,
  );

  final double followDistanceMeters;
  final double lateralOffsetMeters;
  final double lookAheadMeters;
  final double zoom;
  final double fovDegrees;
  final double rollRadians;
  final double pitchDegrees;
  final double shakeX;
  final double shakeY;
}

class CameraShakeManager {
  bool enabled = true;
  double _phase = 0;
  double _impulse = 0;

  void addImpulse(double strength) {
    if (!strength.isFinite) {
      return;
    }
    _impulse = (_impulse + strength.abs()).clamp(0.0, 2.5).toDouble();
  }

  CameraShakeSample step(double dt, {required double speedKph}) {
    if (!enabled || dt <= 0 || !dt.isFinite) {
      _impulse = math.max(0, _impulse - (dt.isFinite ? dt : 0) * 3);
      return CameraShakeSample.zero;
    }

    final safeSpeed = speedKph.isFinite ? speedKph.abs() : 0.0;
    final roadTexture = (safeSpeed / 280).clamp(0.0, 1.0).toDouble() * 0.12;
    final amplitude = roadTexture + _impulse;
    _phase += dt * (18 + (safeSpeed * 0.06));
    final sample = CameraShakeSample(
      x: math.sin(_phase * 1.37) * amplitude,
      y: math.cos(_phase * 1.91) * amplitude * 0.65,
    );
    _impulse = math.max(0, _impulse - (dt * 3.4));
    return sample;
  }

  void reset() {
    _phase = 0;
    _impulse = 0;
  }
}

class RacingCameraController {
  final CameraShakeManager shake = CameraShakeManager();

  RacingCameraState state = RacingCameraState.initial;

  bool get shakeEnabled => shake.enabled;

  void setShakeEnabled(bool enabled) {
    shake.enabled = enabled;
    if (!enabled) {
      state = RacingCameraState(
        followDistanceMeters: state.followDistanceMeters,
        lateralOffsetMeters: state.lateralOffsetMeters,
        lookAheadMeters: state.lookAheadMeters,
        zoom: state.zoom,
        fovDegrees: state.fovDegrees,
        rollRadians: state.rollRadians,
        pitchDegrees: state.pitchDegrees,
        shakeX: 0,
        shakeY: 0,
      );
    }
  }

  void registerCrash(double strength) {
    shake.addImpulse(strength);
  }

  void step({
    required double dt,
    required double trackDistanceMeters,
    required double lateralOffsetMeters,
    required double speedKph,
    required double driftIntensity,
    required double driftDirection,
    required bool nitroActive,
    required bool airborne,
  }) {
    if (dt <= 0 || !dt.isFinite) {
      return;
    }

    final safeDistance = _finite(trackDistanceMeters, state.followDistanceMeters)
        .clamp(-100, 100000)
        .toDouble();
    final safeLateral = _finite(lateralOffsetMeters, 0)
        .clamp(-20, 20)
        .toDouble();
    final safeSpeed = _finite(speedKph, 0).abs().clamp(0, 420).toDouble();
    final speedFactor = (safeSpeed / 280).clamp(0.0, 1.0).toDouble();
    final drift = _finite(driftIntensity, 0).clamp(0.0, 1.0).toDouble();
    final direction = _finite(driftDirection, 0).clamp(-1.0, 1.0).toDouble();

    final desiredLookAhead = 8 +
        (18 * speedFactor) +
        (nitroActive ? 8 : 0) +
        (airborne ? 3 : 0);
    final desiredDistance = safeDistance + desiredLookAhead;
    final desiredLateral = safeLateral + (direction * drift * 2.8);
    final damping = (1 - math.exp(-dt * 6.5)).clamp(0.0, 1.0).toDouble();

    final baseFov = 58 + (15 * speedFactor);
    final desiredFov = (baseFov + (nitroActive ? 7 : 0) + (airborne ? 2 : 0))
        .clamp(52.0, 82.0)
        .toDouble();
    final desiredZoom = (1.16 - ((desiredFov - 52) / 55))
        .clamp(0.68, 1.18)
        .toDouble();
    final desiredRoll = direction * drift * 0.095;
    final desiredPitch = airborne ? -4.5 : 0.0;
    final shakeSample = shake.step(dt, speedKph: safeSpeed);

    state = RacingCameraState(
      followDistanceMeters: _lerp(
        state.followDistanceMeters,
        desiredDistance,
        damping,
      ).clamp(-100, 100000).toDouble(),
      lateralOffsetMeters: (_lerp(
                state.lateralOffsetMeters,
                desiredLateral,
                damping,
              ) +
              shakeSample.x)
          .clamp(-24, 24)
          .toDouble(),
      lookAheadMeters: desiredLookAhead.clamp(4, 40).toDouble(),
      zoom: _lerp(state.zoom, desiredZoom, damping).clamp(0.65, 1.2).toDouble(),
      fovDegrees: _lerp(state.fovDegrees, desiredFov, damping)
          .clamp(52, 82)
          .toDouble(),
      rollRadians: _lerp(state.rollRadians, desiredRoll, damping)
          .clamp(-0.15, 0.15)
          .toDouble(),
      pitchDegrees: _lerp(state.pitchDegrees, desiredPitch, damping)
          .clamp(-8, 4)
          .toDouble(),
      shakeX: shakeSample.x,
      shakeY: shakeSample.y,
    );
  }

  void reset({double trackDistanceMeters = 0}) {
    shake.reset();
    final safeDistance = _finite(trackDistanceMeters, 0);
    state = RacingCameraState(
      followDistanceMeters: safeDistance,
      lateralOffsetMeters: 0,
      lookAheadMeters: 8,
      zoom: 1,
      fovDegrees: 58,
      rollRadians: 0,
      pitchDegrees: 0,
      shakeX: 0,
      shakeY: 0,
    );
  }

  double _finite(double value, double fallback) {
    return value.isFinite ? value : fallback;
  }

  double _lerp(double from, double to, double t) {
    return from + ((to - from) * t);
  }
}
