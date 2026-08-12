import 'package:afareet_asphalt/game/afareet_game.dart';
import 'package:afareet_asphalt/game/input/game_input.dart';
import 'package:flutter/material.dart';

class PrototypeControls extends StatelessWidget {
  const PrototypeControls({required this.game, super.key});

  static const String overlayKey = 'prototype-controls';

  final AfareetGame game;

  @override
  Widget build(BuildContext context) {
    return SafeArea(
      child: Padding(
        padding: const EdgeInsets.all(14),
        child: Column(
          children: <Widget>[
            Row(
              mainAxisAlignment: MainAxisAlignment.end,
              children: <Widget>[
                _TapControl(label: 'PAUSE', onTap: game.togglePauseSimulation),
                const SizedBox(width: 8),
                _TapControl(label: 'RESET', onTap: game.resetVehicleToSafePoint),
                const SizedBox(width: 8),
                _TapControl(label: 'RESTART', onTap: game.restartRace),
              ],
            ),
            const Spacer(),
            Row(
              crossAxisAlignment: CrossAxisAlignment.end,
              children: <Widget>[
                _HoldControl(
                  label: '◀',
                  onChanged: (pressed) => game.input.setSteering(pressed ? -1 : 0),
                ),
                const SizedBox(width: 8),
                _HoldControl(
                  label: '▶',
                  onChanged: (pressed) => game.input.setSteering(pressed ? 1 : 0),
                ),
                const Spacer(),
                _HoldControl(
                  label: 'BRAKE',
                  onChanged: (pressed) =>
                      game.input.setAction(GameAction.brake, pressed),
                ),
                const SizedBox(width: 8),
                _HoldControl(
                  label: 'DRIFT',
                  onChanged: (pressed) =>
                      game.input.setAction(GameAction.drift, pressed),
                ),
                const SizedBox(width: 8),
                _HoldControl(
                  label: 'NITRO',
                  onChanged: (pressed) =>
                      game.input.setAction(GameAction.nitro, pressed),
                ),
                const SizedBox(width: 8),
                _HoldControl(
                  label: 'GO',
                  onChanged: (pressed) =>
                      game.input.setAction(GameAction.throttle, pressed),
                ),
              ],
            ),
          ],
        ),
      ),
    );
  }
}

class _HoldControl extends StatelessWidget {
  const _HoldControl({required this.label, required this.onChanged});

  final String label;
  final ValueChanged<bool> onChanged;

  @override
  Widget build(BuildContext context) {
    return Listener(
      onPointerDown: (_) => onChanged(true),
      onPointerUp: (_) => onChanged(false),
      onPointerCancel: (_) => onChanged(false),
      child: _ControlSurface(label: label),
    );
  }
}

class _TapControl extends StatelessWidget {
  const _TapControl({required this.label, required this.onTap});

  final String label;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    return GestureDetector(onTap: onTap, child: _ControlSurface(label: label));
  }
}

class _ControlSurface extends StatelessWidget {
  const _ControlSurface({required this.label});

  final String label;

  @override
  Widget build(BuildContext context) {
    return Container(
      constraints: const BoxConstraints(minWidth: 58, minHeight: 48),
      alignment: Alignment.center,
      padding: const EdgeInsets.symmetric(horizontal: 12),
      decoration: BoxDecoration(
        color: const Color(0xAA071326),
        border: Border.all(color: const Color(0xAA00E5FF)),
        borderRadius: BorderRadius.circular(14),
      ),
      child: Text(label, style: const TextStyle(fontWeight: FontWeight.w800)),
    );
  }
}
