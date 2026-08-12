import 'package:flutter/foundation.dart';

@immutable
class GameTelemetry {
  const GameTelemetry({
    required this.fps,
    required this.frameTimeMs,
    required this.position,
    required this.speedKph,
    required this.spirit,
    required this.raceTimeSeconds,
  });

  static const GameTelemetry initial = GameTelemetry(
    fps: 0,
    frameTimeMs: 0,
    position: 1,
    speedKph: 0,
    spirit: 0,
    raceTimeSeconds: 0,
  );

  final double fps;
  final double frameTimeMs;
  final int position;
  final double speedKph;
  final double spirit;
  final double raceTimeSeconds;

  GameTelemetry copyWith({
    double? fps,
    double? frameTimeMs,
    int? position,
    double? speedKph,
    double? spirit,
    double? raceTimeSeconds,
  }) {
    return GameTelemetry(
      fps: fps ?? this.fps,
      frameTimeMs: frameTimeMs ?? this.frameTimeMs,
      position: position ?? this.position,
      speedKph: speedKph ?? this.speedKph,
      spirit: spirit ?? this.spirit,
      raceTimeSeconds: raceTimeSeconds ?? this.raceTimeSeconds,
    );
  }
}
