import 'package:afareet_asphalt/game/camera/racing_camera_controller.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  group('RacingCameraController', () {
    test('speed and nitro increase look ahead and FOV with damping', () {
      final camera = RacingCameraController();
      camera.step(
        dt: 0.1,
        trackDistanceMeters: 100,
        lateralOffsetMeters: 0,
        speedKph: 40,
        driftIntensity: 0,
        driftDirection: 0,
        nitroActive: false,
        airborne: false,
      );
      final slow = camera.state;

      camera.step(
        dt: 0.1,
        trackDistanceMeters: 110,
        lateralOffsetMeters: 0,
        speedKph: 220,
        driftIntensity: 0,
        driftDirection: 0,
        nitroActive: true,
        airborne: false,
      );
      final fast = camera.state;

      expect(fast.lookAheadMeters, greaterThan(slow.lookAheadMeters));
      expect(fast.fovDegrees, greaterThan(slow.fovDegrees));
      expect(fast.followDistanceMeters, lessThan(150));
    });

    test('drift, airborne and crash feedback are bounded', () {
      final camera = RacingCameraController()..registerCrash(1.5);
      camera.step(
        dt: 0.016,
        trackDistanceMeters: 50,
        lateralOffsetMeters: 2,
        speedKph: 150,
        driftIntensity: 0.9,
        driftDirection: -1,
        nitroActive: false,
        airborne: true,
      );

      expect(camera.state.rollRadians, lessThan(0));
      expect(camera.state.pitchDegrees, lessThan(0));
      expect(camera.state.shakeX.abs() + camera.state.shakeY.abs(), greaterThan(0));
      expect(camera.state.zoom, inInclusiveRange(0.65, 1.2));
    });

    test('accessibility toggle disables shake and invalid input is sanitized', () {
      final camera = RacingCameraController()
        ..registerCrash(2)
        ..setShakeEnabled(false);
      camera.step(
        dt: 0.016,
        trackDistanceMeters: double.nan,
        lateralOffsetMeters: double.infinity,
        speedKph: double.nan,
        driftIntensity: double.infinity,
        driftDirection: double.nan,
        nitroActive: true,
        airborne: false,
      );

      expect(camera.state.shakeX, 0);
      expect(camera.state.shakeY, 0);
      expect(camera.state.followDistanceMeters.isFinite, isTrue);
      expect(camera.state.lateralOffsetMeters.isFinite, isTrue);
    });
  });
}
