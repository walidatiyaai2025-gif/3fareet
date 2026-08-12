import 'dart:convert';

import 'package:flutter/services.dart';

class GameConfig {
  const GameConfig({
    required this.targetFps,
    required this.fixedStepSeconds,
    required this.maxCatchUpSteps,
    required this.prototypeTrackId,
    required this.initialSpirit,
    required this.debugOverlayEnabled,
  });

  factory GameConfig.fromJson(Map<String, Object?> json) {
    return GameConfig(
      targetFps: (json['targetFps'] as num).toInt(),
      fixedStepSeconds: (json['fixedStepSeconds'] as num).toDouble(),
      maxCatchUpSteps: (json['maxCatchUpSteps'] as num).toInt(),
      prototypeTrackId: json['prototypeTrackId']! as String,
      initialSpirit: (json['initialSpirit'] as num).toDouble(),
      debugOverlayEnabled: json['debugOverlayEnabled']! as bool,
    );
  }

  final int targetFps;
  final double fixedStepSeconds;
  final int maxCatchUpSteps;
  final String prototypeTrackId;
  final double initialSpirit;
  final bool debugOverlayEnabled;
}

class GameConfigLoader {
  GameConfigLoader({
    required this.bundle,
    this.path = 'assets/config/game_config.json',
  });

  final AssetBundle bundle;
  final String path;

  Future<GameConfig> load() async {
    final raw = await bundle.loadString(path);
    final decoded = jsonDecode(raw);
    if (decoded is! Map<String, Object?>) {
      throw const FormatException('Game config root must be a JSON object.');
    }
    return GameConfig.fromJson(decoded);
  }
}
