import 'package:afareet_asphalt/game/garage/garage_controller.dart';
import 'package:flutter/material.dart';

class GarageScreen extends StatelessWidget {
  const GarageScreen({required this.controller, super.key});

  final GarageController controller;

  @override
  Widget build(BuildContext context) {
    return AnimatedBuilder(
      animation: controller,
      builder: (context, child) {
        return LayoutBuilder(
          builder: (context, constraints) {
            final compact = constraints.maxWidth < 720;
            final list = _GarageVehicleList(controller: controller);
            final detail = _GarageDetail(controller: controller);
            return ColoredBox(
              color: const Color(0xFF07101B),
              child: SafeArea(
                child: compact
                    ? Column(
                        children: <Widget>[
                          SizedBox(height: 190, child: list),
                          const Divider(height: 1),
                          Expanded(child: detail),
                        ],
                      )
                    : Row(
                        children: <Widget>[
                          SizedBox(width: 280, child: list),
                          const VerticalDivider(width: 1),
                          Expanded(child: detail),
                        ],
                      ),
              ),
            );
          },
        );
      },
    );
  }
}

class _GarageVehicleList extends StatelessWidget {
  const _GarageVehicleList({required this.controller});

  final GarageController controller;

  @override
  Widget build(BuildContext context) {
    return ListView(
      padding: const EdgeInsets.all(12),
      children: <Widget>[
        const Text(
          'GARAGE',
          style: TextStyle(fontSize: 24, fontWeight: FontWeight.w900),
        ),
        const SizedBox(height: 8),
        Text('Driver level ${controller.playerLevel}'),
        const SizedBox(height: 12),
        for (final vehicle in controller.vehicles)
          Card(
            child: ListTile(
              selected: vehicle.selected,
              onTap: () => controller.selectVehicle(vehicle.entry.id),
              leading: Icon(
                vehicle.unlocked ? Icons.directions_car : Icons.lock_outline,
              ),
              title: Text(vehicle.entry.displayName),
              subtitle: Text(
                vehicle.unlocked
                    ? vehicle.entry.vehicleClass.name.toUpperCase()
                    : 'Unlocks at level ${vehicle.entry.unlockLevel}',
              ),
              trailing: vehicle.equipped
                  ? const Icon(Icons.check_circle, color: Colors.greenAccent)
                  : null,
            ),
          ),
      ],
    );
  }
}

class _GarageDetail extends StatelessWidget {
  const _GarageDetail({required this.controller});

  final GarageController controller;

  @override
  Widget build(BuildContext context) {
    final preview = controller.preview;
    final unlocked = controller.isUnlocked(preview.vehicleId);
    return ListView(
      padding: const EdgeInsets.all(20),
      children: <Widget>[
        Text(
          preview.displayName,
          style: Theme.of(context).textTheme.headlineMedium?.copyWith(
                fontWeight: FontWeight.w900,
              ),
        ),
        const SizedBox(height: 16),
        _VehiclePreview(preview: preview),
        const SizedBox(height: 20),
        const Text('PERFORMANCE', style: TextStyle(fontWeight: FontWeight.bold)),
        const SizedBox(height: 10),
        for (final stat in controller.normalizedStats.entries)
          Padding(
            padding: const EdgeInsets.only(bottom: 10),
            child: Row(
              children: <Widget>[
                SizedBox(width: 100, child: Text(stat.key)),
                Expanded(child: LinearProgressIndicator(value: stat.value)),
                const SizedBox(width: 8),
                Text('${(stat.value * 100).round()}'),
              ],
            ),
          ),
        const SizedBox(height: 12),
        _ChoiceGroup(
          title: 'PAINT',
          values: controller.paintOptions,
          selected: preview.paintId,
          enabled: unlocked,
          onSelected: controller.setPaint,
        ),
        _ChoiceGroup(
          title: 'WHEELS',
          values: controller.wheelOptions,
          selected: preview.wheelId,
          enabled: unlocked,
          onSelected: controller.setWheel,
        ),
        _ChoiceGroup(
          title: 'MAGIC TRAIL',
          values: controller.magicTrailOptions,
          selected: preview.magicTrailId,
          enabled: unlocked,
          onSelected: controller.setMagicTrail,
        ),
        _ChoiceGroup(
          title: 'SPIRIT',
          values: controller.spiritCosmeticOptions,
          selected: preview.spiritCosmeticId,
          enabled: unlocked,
          onSelected: controller.setSpiritCosmetic,
        ),
        const SizedBox(height: 12),
        FilledButton.icon(
          onPressed: unlocked ? controller.equipSelected : null,
          icon: Icon(
            controller.equippedVehicleId == preview.vehicleId
                ? Icons.check_circle
                : Icons.bolt,
          ),
          label: Text(
            controller.equippedVehicleId == preview.vehicleId
                ? 'EQUIPPED'
                : unlocked
                    ? 'EQUIP CAR'
                    : 'LOCKED',
          ),
        ),
      ],
    );
  }
}

class _VehiclePreview extends StatelessWidget {
  const _VehiclePreview({required this.preview});

  final GaragePreviewModel preview;

  @override
  Widget build(BuildContext context) {
    return AspectRatio(
      aspectRatio: 16 / 8,
      child: DecoratedBox(
        decoration: BoxDecoration(
          borderRadius: BorderRadius.circular(22),
          gradient: const LinearGradient(
            colors: <Color>[Color(0xFF10283A), Color(0xFF0A1420)],
          ),
          border: Border.all(color: const Color(0xFF31D7FF).withValues(alpha: .35)),
        ),
        child: Stack(
          fit: StackFit.expand,
          children: <Widget>[
            if (preview.assetPath != null)
              ClipRRect(
                borderRadius: BorderRadius.circular(22),
                child: Image.asset(
                  preview.assetPath!,
                  fit: BoxFit.contain,
                  errorBuilder: (_, __, ___) => const SizedBox.shrink(),
                ),
              ),
            if (preview.assetPath == null)
              const Center(
                child: Icon(Icons.directions_car_filled, size: 96),
              ),
            Positioned(
              left: 14,
              bottom: 12,
              child: Text(
                '${preview.paintId} • ${preview.wheelId} • ${preview.magicTrailId}',
                style: const TextStyle(fontWeight: FontWeight.w700),
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _ChoiceGroup extends StatelessWidget {
  const _ChoiceGroup({
    required this.title,
    required this.values,
    required this.selected,
    required this.enabled,
    required this.onSelected,
  });

  final String title;
  final List<String> values;
  final String selected;
  final bool enabled;
  final bool Function(String value) onSelected;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.only(top: 12),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: <Widget>[
          Text(title, style: const TextStyle(fontWeight: FontWeight.bold)),
          const SizedBox(height: 8),
          Wrap(
            spacing: 8,
            runSpacing: 8,
            children: <Widget>[
              for (final value in values)
                ChoiceChip(
                  label: Text(value.replaceAll('_', ' ')),
                  selected: value == selected,
                  onSelected: enabled ? (_) => onSelected(value) : null,
                ),
            ],
          ),
        ],
      ),
    );
  }
}
