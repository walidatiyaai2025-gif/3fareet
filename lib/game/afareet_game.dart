import 'dart:async';

import 'package:afareet_asphalt/game/audio/prototype_music_controller.dart';
import 'package:afareet_asphalt/game/camera/racing_camera_controller.dart';
import 'package:afareet_asphalt/game/core/fixed_step_runner.dart';
import 'package:afareet_asphalt/game/core/game_bootstrap.dart';
import 'package:afareet_asphalt/game/core/game_telemetry.dart';
import 'package:afareet_asphalt/game/input/game_input.dart';
import 'package:afareet_asphalt/game/race/race_controller.dart';
import 'package:afareet_asphalt/game/race/race_session.dart';
import 'package:afareet_asphalt/game/scenes/prototype_scene.dart';
import 'package:afareet_asphalt/game/ui/ui_overlay_keys.dart';
import 'package:afareet_asphalt/game/vehicle/vehicle_definition.dart';
import 'package:afareet_asphalt/game/vehicle/vehicle_tuning.dart';
import 'package:flame/game.dart';
import 'package:flutter/material.dart';
import 'package:flutter/services.dart';

class AfareetGame extends FlameGame with SingleGameInstance {
  AfareetGame({required this.bootstrap})
    : musicController = PrototypeMusicController(bundle: rootBundle);

  final GameBootstrap bootstrap;
  final PrototypeMusicController musicController;
  final GameInputState input = GameInputState();
  final RacingCameraController racingCamera = RacingCameraController();
  final VehicleTuningController vehicleTuning = VehicleTuningController(
    PrototypeVehiclePreset.definition,
  );
  final ValueNotifier<GameTelemetry> telemetry = ValueNotifier<GameTelemetry>(
    GameTelemetry.initial,
  );

  FixedStepRunner? _fixedStepRunner;
  RaceSession? _raceSession;
  bool _simulationPaused = false;
  bool _resultOverlayShown = false;
  double _telemetryWindowSeconds = 0;
  int _telemetryFrames = 0;

  RaceSession get raceSession {
    final session = _raceSession;
    if (session == null) {
      throw StateError('RaceSession requested before game load completed.');
    }
    return session;
  }

  bool get simulationPaused => _simulationPaused;
  bool get cameraShakeEnabled => racingCamera.shakeEnabled;

  @override
  Future<void> onLoad() async {
    await super.onLoad();
    await bootstrap.initialize();

    final config = bootstrap.config;
    _fixedStepRunner = FixedStepRunner(
      stepSeconds: config.fixedStepSeconds,
      maxCatchUpSteps: config.maxCatchUpSteps,
    );
    _raceSession = RaceSession.prototype(
      vehicleDefinition: vehicleTuning.definition,
    )..restart();

    try {
      await musicController.start();
    } on Object {
      // Audio must never block the playable prototype from booting.
    }

    // The visual prototype is attached to the viewport so the deterministic
    // camera controller can keep evolving without moving the 2.5D projection.
    await camera.viewport.add(PrototypeScene(trackId: config.prototypeTrackId));
  }

  @override
  void update(double dt) {
    if (_simulationPaused) {
      super.update(0);
      return;
    }

    final runner = _fixedStepRunner;
    if (runner == null) {
      super.update(dt);
      return;
    }

    runner.advance(dt, _simulateFixedStep);
    super.update(dt);

    _telemetryFrames += 1;
    _telemetryWindowSeconds += dt;
    if (_telemetryWindowSeconds >= 0.25) {
      final fps = _telemetryFrames / _telemetryWindowSeconds;
      final session = raceSession;
      final progress = session.track.totalLengthMeters <= 0
          ? 0.0
          : (session.distanceAlongLapMeters / session.track.totalLengthMeters)
                .clamp(0.0, 1.0)
                .toDouble();
      telemetry.value = telemetry.value.copyWith(
        fps: fps,
        frameTimeMs: fps > 0 ? 1000 / fps : 0,
        position: session.ai.playerPosition(session.distanceAlongLapMeters),
        speedKph: session.vehicle.state.speedKph,
        spirit: session.spirit.normalizedEnergy,
        raceTimeSeconds: session.race.raceTimeSeconds,
        raceProgress: progress,
        lap: session.race.currentLap,
        totalLaps: session.track.totalLaps,
        racePhase: session.race.phase.name,
      );
      _telemetryFrames = 0;
      _telemetryWindowSeconds = 0;
    }
  }

  void _simulateFixedStep(double stepSeconds) {
    final snapshot = input.consumeSnapshot();
    if (snapshot.pausePressed) {
      pauseSimulation();
      return;
    }

    final session = raceSession;
    session.step(snapshot, stepSeconds);
    final maxSlip = session.vehicle.definition.maxLateralSlipMps;
    final driftIntensity = maxSlip <= 0
        ? 0.0
        : (session.vehicle.state.lateralSlipMps.abs() / maxSlip)
              .clamp(0.0, 1.0)
              .toDouble();
    racingCamera.step(
      dt: stepSeconds,
      trackDistanceMeters: session.distanceAlongLapMeters,
      lateralOffsetMeters: session.vehicle.state.lateralSlipMps * 0.12,
      speedKph: session.vehicle.state.speedKph,
      driftIntensity: driftIntensity,
      driftDirection: snapshot.steering,
      nitroActive: session.spirit.nitroActive,
      airborne: false,
    );
    final cameraState = racingCamera.state;
    camera.viewfinder.position.setValues(
      cameraState.followDistanceMeters,
      cameraState.lateralOffsetMeters + cameraState.shakeY,
    );
    camera.viewfinder.zoom = cameraState.zoom;
    camera.viewfinder.angle = cameraState.rollRadians;

    if (session.race.phase == RacePhase.finished && !_resultOverlayShown) {
      _resultOverlayShown = true;
      overlays.add(UiOverlayKeys.raceResult);
    }
  }

  void pauseSimulation({bool showOverlay = true}) {
    _simulationPaused = true;
    if (showOverlay) {
      overlays.add(UiOverlayKeys.pauseMenu);
    }
  }

  void resumeSimulation() {
    _simulationPaused = false;
    overlays.remove(UiOverlayKeys.pauseMenu);
    _fixedStepRunner?.reset();
  }

  void togglePauseSimulation() {
    if (_simulationPaused) {
      resumeSimulation();
    } else {
      pauseSimulation();
    }
  }

  void restartRace() {
    input.reset();
    raceSession.restart();
    racingCamera.reset();
    _fixedStepRunner?.reset();
    _resultOverlayShown = false;
    overlays.remove(UiOverlayKeys.raceResult);
    telemetry.value = GameTelemetry.initial;
  }

  void dismissRuntimeMenus() {
    overlays.remove(UiOverlayKeys.pauseMenu);
    overlays.remove(UiOverlayKeys.raceResult);
    _resultOverlayShown = false;
  }

  void resetVehicleToSafePoint() {
    raceSession.resetVehicleToSafePoint();
    racingCamera.reset(trackDistanceMeters: raceSession.distanceAlongLapMeters);
  }

  void registerCrashFeedback(double strength) {
    racingCamera.registerCrash(strength);
  }

  void setCameraShakeEnabled(bool enabled) {
    racingCamera.setShakeEnabled(enabled);
  }

  void updateMaxSpeedKph(double value) {
    vehicleTuning.setMaxSpeedKph(value);
    _syncVehicleDefinition();
  }

  void updateAcceleration(double value) {
    vehicleTuning.setAcceleration(value);
    _syncVehicleDefinition();
  }

  void updateGripRecovery(double value) {
    vehicleTuning.setGripRecovery(value);
    _syncVehicleDefinition();
  }

  void updateDriftSlipBuild(double value) {
    vehicleTuning.setDriftSlipBuild(value);
    _syncVehicleDefinition();
  }

  void _syncVehicleDefinition() {
    final session = _raceSession;
    if (session != null) {
      session.vehicle.definition = vehicleTuning.definition;
    }
  }

  @override
  Color backgroundColor() => const Color(0xFF050A16);

  @override
  void onRemove() {
    unawaited(musicController.dispose());
    bootstrap.dispose();
    telemetry.dispose();
    super.onRemove();
  }
}
