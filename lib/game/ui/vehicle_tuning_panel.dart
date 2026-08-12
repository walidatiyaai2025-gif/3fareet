import 'package:afareet_asphalt/game/afareet_game.dart';
import 'package:flutter/material.dart';

class VehicleTuningPanel extends StatefulWidget {
  const VehicleTuningPanel({required this.game, super.key});

  static const String overlayKey = 'vehicle-tuning';

  final AfareetGame game;

  @override
  State<VehicleTuningPanel> createState() => _VehicleTuningPanelState();
}

class _VehicleTuningPanelState extends State<VehicleTuningPanel> {
  @override
  Widget build(BuildContext context) {
    final definition = widget.game.vehicleTuning.definition;
    return SafeArea(
      child: Align(
        alignment: Alignment.centerRight,
        child: Container(
          width: 280,
          margin: const EdgeInsets.all(12),
          padding: const EdgeInsets.all(12),
          decoration: BoxDecoration(
            color: const Color(0xEE071326),
            border: Border.all(color: const Color(0xFF00E5FF)),
            borderRadius: BorderRadius.circular(12),
          ),
          child: Material(
            color: Colors.transparent,
            child: Column(
              mainAxisSize: MainAxisSize.min,
              children: <Widget>[
                const Text(
                  'VEHICLE TUNING',
                  style: TextStyle(fontWeight: FontWeight.w800),
                ),
                _slider(
                  'Max speed km/h',
                  definition.maxForwardSpeedMps * 3.6,
                  120,
                  260,
                  widget.game.updateMaxSpeedKph,
                ),
                _slider(
                  'Acceleration',
                  definition.accelerationMps2,
                  6,
                  24,
                  widget.game.updateAcceleration,
                ),
                _slider(
                  'Grip recovery',
                  definition.gripRecoveryPerSecond,
                  2,
                  16,
                  widget.game.updateGripRecovery,
                ),
                _slider(
                  'Drift slip',
                  definition.driftSlipBuildMps2,
                  5,
                  24,
                  widget.game.updateDriftSlipBuild,
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }

  Widget _slider(
    String label,
    double value,
    double min,
    double max,
    ValueChanged<double> onChanged,
  ) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: <Widget>[
        Text('$label  ${value.toStringAsFixed(1)}'),
        Slider(
          value: value.clamp(min, max).toDouble(),
          min: min,
          max: max,
          onChanged: (newValue) {
            onChanged(newValue);
            setState(() {});
          },
        ),
      ],
    );
  }
}
