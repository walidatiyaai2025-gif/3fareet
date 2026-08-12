enum VehicleClass { street, muscle, exotic, spirit }

class VehicleStats {
  const VehicleStats({
    required this.topSpeed,
    required this.acceleration,
    required this.handling,
    required this.nitro,
  });

  final double topSpeed;
  final double acceleration;
  final double handling;
  final double nitro;

  bool get isValid => <double>[
        topSpeed,
        acceleration,
        handling,
        nitro,
      ].every((value) => value >= 0 && value <= 100);
}

class CarCatalogEntry {
  const CarCatalogEntry({
    required this.id,
    required this.displayName,
    required this.vehicleClass,
    required this.stats,
    required this.basePaintId,
    required this.unlockLevel,
    this.previewAsset,
  });

  final String id;
  final String displayName;
  final VehicleClass vehicleClass;
  final VehicleStats stats;
  final String basePaintId;
  final int unlockLevel;
  final String? previewAsset;

  bool get isValid =>
      id.trim().isNotEmpty &&
      displayName.trim().isNotEmpty &&
      basePaintId.trim().isNotEmpty &&
      unlockLevel >= 1 &&
      stats.isValid;
}

class CarCatalog {
  CarCatalog(Iterable<CarCatalogEntry> entries)
      : _entries = {for (final entry in entries) entry.id: entry} {
    if (_entries.length != entries.length) {
      throw ArgumentError('Car catalog IDs must be unique.');
    }
    if (_entries.values.any((entry) => !entry.isValid)) {
      throw ArgumentError('Car catalog contains an invalid entry.');
    }
  }

  final Map<String, CarCatalogEntry> _entries;

  List<CarCatalogEntry> get entries => List.unmodifiable(_entries.values);

  CarCatalogEntry? byId(String id) => _entries[id];

  List<CarCatalogEntry> unlockedAt(int level) => _entries.values
      .where((entry) => entry.unlockLevel <= level)
      .toList(growable: false);
}
