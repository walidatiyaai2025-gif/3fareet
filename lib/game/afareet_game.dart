import 'package:afareet_asphalt/game/core/fixed_step_runner.dart';
import 'package:afareet_asphalt/game/core/game_bootstrap.dart';
import 'package:afareet_asphalt/game/core/game_telemetry.dart';
import 'package:afareet_asphalt/game/scenes/prototype_scene.dart';
import 'package:flame/game.dart';
import 'package:flutter/material.dart';

class AfareetGame extends FlameGame with SingleGameInstance {
  AfareetGame({required this.bootstrap});

  final GameBootstrap bootstrap;
  final ValueNotifier<GameTelemetry> telemetry = ValueNotifier<GameTelemetry>(
    GameTelemetry.initial,
  );

  FixedStepRunner? _fixedStepRunner;
  double _elapsedSeconds = 0;
  double _telemetryWindowSeconds = 0;
  int _telemetryFrames = 0;

  @override
  Future<void> onLoad() async {
    await super.onLoad();
    await bootstrap.initialize();

    final config = bootstrap.config;
    _fixedStepRunner = FixedStepRunner(
      stepSeconds: config.fixedStepSeconds,
      maxCatchUpSteps: config.maxCatchUpSteps,
    );

    await world.add(PrototypeScene(trackId: config.prototypeTrackId));
  }

  @override
  void update(double dt) {
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
      telemetry.value = telemetry.value.copyWith(
        fps: fps,
        frameTimeMs: fps > 0 ? 1000 / fps : 0,
        raceTimeSeconds: _elapsedSeconds,
      );
      _telemetryFrames = 0;
      _telemetryWindowSeconds = 0;
    }
  }

  void _simulateFixedStep(double stepSeconds) {
    _elapsedSeconds += stepSeconds;
  }

  @override
  Color backgroundColor() => const Color(0xFF050A16);

  @override
  void onRemove() {
    bootstrap.dispose();
    telemetry.dispose();
    super.onRemove();
  }
}
