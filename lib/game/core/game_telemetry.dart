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
    required this.raceProgress,
    required this.lap,
    required this.totalLaps,
    required this.racePhase,
  });

  static const GameTelemetry initial = GameTelemetry(
    fps: 0,
    frameTimeMs: 0,
    position: 1,
    speedKph: 0,
    spirit: 0,
    raceTimeSeconds: 0,
    raceProgress: 0,
    lap: 1,
    totalLaps: 1,
    racePhase: 'waiting',
  );

  final double fps;
  final double frameTimeMs;
  final int position;
  final double speedKph;
  final double spirit;
  final double raceTimeSeconds;
  final double raceProgress;
  final int lap;
  final int totalLaps;
  final String racePhase;

  GameTelemetry copyWith({
    double? fps,
    double? frameTimeMs,
    int? position,
    double? speedKph,
    double? spirit,
    double? raceTimeSeconds,
    double? raceProgress,
    int? lap,
    int? totalLaps,
    String? racePhase,
  }) {
    return GameTelemetry(
      fps: fps ?? this.fps,
      frameTimeMs: frameTimeMs ?? this.frameTimeMs,
      position: position ?? this.position,
      speedKph: speedKph ?? this.speedKph,
      spirit: spirit ?? this.spirit,
      raceTimeSeconds: raceTimeSeconds ?? this.raceTimeSeconds,
      raceProgress: raceProgress ?? this.raceProgress,
      lap: lap ?? this.lap,
      totalLaps: totalLaps ?? this.totalLaps,
      racePhase: racePhase ?? this.racePhase,
    );
  }
}
