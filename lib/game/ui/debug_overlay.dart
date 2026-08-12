import 'package:afareet_asphalt/game/afareet_game.dart';
import 'package:afareet_asphalt/game/core/game_telemetry.dart';
import 'package:flutter/material.dart';

class GameDebugOverlay extends StatelessWidget {
  const GameDebugOverlay({required this.game, super.key});

  static const String overlayKey = 'debug-telemetry';

  final AfareetGame game;

  @override
  Widget build(BuildContext context) {
    return IgnorePointer(
      child: SafeArea(
        child: Align(
          alignment: Alignment.bottomLeft,
          child: ValueListenableBuilder<GameTelemetry>(
            valueListenable: game.telemetry,
            builder: (context, telemetry, child) {
              return Container(
                margin: const EdgeInsets.all(12),
                padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 7),
                decoration: BoxDecoration(
                  color: const Color(0xCC000000),
                  borderRadius: BorderRadius.circular(8),
                  border: Border.all(color: const Color(0x6600E5FF)),
                ),
                child: Text(
                  'FPS ${telemetry.fps.toStringAsFixed(1)}  |  ${telemetry.frameTimeMs.toStringAsFixed(2)} ms',
                  style: const TextStyle(
                    color: Color(0xFF7CFFB2),
                    fontSize: 11,
                    fontFeatures: <FontFeature>[FontFeature.tabularFigures()],
                  ),
                ),
              );
            },
          ),
        ),
      ),
    );
  }
}
