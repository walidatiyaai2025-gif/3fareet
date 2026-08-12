import 'dart:convert';

import 'package:afareet_asphalt/game/garage/garage_controller.dart';

class GarageSaveData {
  const GarageSaveData({
    required this.version,
    required this.equippedVehicleId,
    required this.loadouts,
  });

  static const currentVersion = 1;

  final int version;
  final String? equippedVehicleId;
  final Map<String, GarageLoadout> loadouts;

  Map<String, Object?> toJson() => <String, Object?>{
        'version': version,
        'equippedVehicleId': equippedVehicleId,
        'loadouts': <String, Object?>{
          for (final entry in loadouts.entries)
            entry.key: <String, Object?>{
              'vehicleId': entry.value.vehicleId,
              'paintId': entry.value.paintId,
              'wheelId': entry.value.wheelId,
              'magicTrailId': entry.value.magicTrailId,
              'spiritCosmeticId': entry.value.spiritCosmeticId,
            },
        },
      };
}

class GarageSaveCodec {
  const GarageSaveCodec();

  String encode(GarageSaveData data) => jsonEncode(data.toJson());

  GarageSaveData decode(String source) {
    final root = jsonDecode(source);
    if (root is! Map<String, dynamic>) {
      throw const FormatException('Garage save root must be an object.');
    }
    final version = root['version'];
    if (version != GarageSaveData.currentVersion) {
      throw FormatException('Unsupported garage save version: $version');
    }
    final rawLoadouts = root['loadouts'];
    if (rawLoadouts is! Map<String, dynamic>) {
      throw const FormatException('Garage save loadouts must be an object.');
    }
    final loadouts = <String, GarageLoadout>{};
    for (final entry in rawLoadouts.entries) {
      final value = entry.value;
      if (value is! Map<String, dynamic>) {
        throw FormatException('Invalid loadout for ${entry.key}.');
      }
      String requiredString(String key) {
        final result = value[key];
        if (result is! String || result.trim().isEmpty) {
          throw FormatException('Invalid $key for ${entry.key}.');
        }
        return result;
      }
      loadouts[entry.key] = GarageLoadout(
        vehicleId: requiredString('vehicleId'),
        paintId: requiredString('paintId'),
        wheelId: requiredString('wheelId'),
        magicTrailId: requiredString('magicTrailId'),
        spiritCosmeticId: requiredString('spiritCosmeticId'),
      );
    }
    final equipped = root['equippedVehicleId'];
    return GarageSaveData(
      version: version as int,
      equippedVehicleId: equipped is String ? equipped : null,
      loadouts: loadouts,
    );
  }
}
