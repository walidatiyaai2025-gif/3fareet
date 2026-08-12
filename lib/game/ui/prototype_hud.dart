import 'package:afareet_asphalt/game/afareet_game.dart';
import 'package:afareet_asphalt/game/core/game_telemetry.dart';
import 'package:afareet_asphalt/game/ui/ui_tokens.dart';
import 'package:flutter/material.dart';

class PrototypeHud extends StatelessWidget {
  const PrototypeHud({required this.game, super.key});

  static const String overlayKey = 'prototype-hud';

  final AfareetGame game;

  @override
  Widget build(BuildContext context) {
    return IgnorePointer(
      child: SafeArea(
        child: ValueListenableBuilder<GameTelemetry>(
          valueListenable: game.telemetry,
          builder: (context, telemetry, child) {
            return Padding(
              padding: const EdgeInsets.all(16),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: <Widget>[
                  Row(
                    mainAxisAlignment: MainAxisAlignment.spaceBetween,
                    children: <Widget>[
                      _HudChip(label: 'POS', value: '${telemetry.position}/4'),
                      _HudChip(label: 'LAP', value: '${telemetry.lap}/${telemetry.totalLaps}'),
                      _HudChip(
                        label: telemetry.racePhase.toUpperCase(),
                        value: telemetry.raceTimeSeconds.toStringAsFixed(1),
                      ),
                    ],
                  ),
                  const SizedBox(height: 10),
                  ClipRRect(
                    borderRadius: BorderRadius.circular(99),
                    child: LinearProgressIndicator(
                      value: telemetry.raceProgress.clamp(0.0, 1.0).toDouble(),
                      minHeight: 5,
                      backgroundColor: const Color(0x3300E5FF),
                      valueColor: const AlwaysStoppedAnimation<Color>(AfareetUiTokens.cyan),
                    ),
                  ),
                  const Spacer(),
                  Row(
                    crossAxisAlignment: CrossAxisAlignment.end,
                    mainAxisAlignment: MainAxisAlignment.spaceBetween,
                    children: <Widget>[
                      _SpiritMeter(value: telemetry.spirit),
                      _SpeedReadout(speedKph: telemetry.speedKph),
                    ],
                  ),
                ],
              ),
            );
          },
        ),
      ),
    );
  }
}

class _HudChip extends StatelessWidget {
  const _HudChip({required this.label, required this.value});

  final String label;
  final String value;

  @override
  Widget build(BuildContext context) {
    return DecoratedBox(
      decoration: BoxDecoration(
        color: AfareetUiTokens.surface,
        border: Border.all(color: AfareetUiTokens.cyan, width: 1.2),
        borderRadius: BorderRadius.circular(12),
      ),
      child: Padding(
        padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
        child: Text(
          '$label  $value',
          style: const TextStyle(fontWeight: FontWeight.w800),
        ),
      ),
    );
  }
}

class _SpiritMeter extends StatelessWidget {
  const _SpiritMeter({required this.value});

  final double value;

  @override
  Widget build(BuildContext context) {
    return SizedBox(
      width: 190,
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: <Widget>[
          const Text(
            'SPIRIT',
            style: TextStyle(color: AfareetUiTokens.gold, fontWeight: FontWeight.w900),
          ),
          const SizedBox(height: 6),
          LinearProgressIndicator(
            value: value.clamp(0.0, 1.0).toDouble(),
            minHeight: 10,
            backgroundColor: const Color(0x6600E5FF),
            valueColor: const AlwaysStoppedAnimation<Color>(AfareetUiTokens.gold),
            borderRadius: BorderRadius.circular(99),
          ),
        ],
      ),
    );
  }
}

class _SpeedReadout extends StatelessWidget {
  const _SpeedReadout({required this.speedKph});

  final double speedKph;

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.end,
      children: <Widget>[
        Text(
          speedKph.toStringAsFixed(0).padLeft(3, '0'),
          style: const TextStyle(fontSize: 42, fontWeight: FontWeight.w900, height: 0.9),
        ),
        const Text(
          'KM/H',
          style: TextStyle(color: AfareetUiTokens.cyan, letterSpacing: 2),
        ),
      ],
    );
  }
}
