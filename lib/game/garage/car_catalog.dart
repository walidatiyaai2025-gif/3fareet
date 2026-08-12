enum CarPerformanceClass { street, drift, muscle, exotic }

enum CarRarity { common, rare, epic, legendary }

/// Typed, backend-friendly schema for a vehicle offered by the garage.
///
/// This deliberately references a [vehicleDefinitionId] instead of embedding
/// simulation tuning. The garage/catalog layer can evolve independently while
/// the deterministic vehicle definitions remain the gameplay source of truth.
class CarCatalogEntry {
  const CarCatalogEntry({
    required this.id,
    required this.vehicleDefinitionId,
    required this.displayName,
    required this.performanceClass,
    required this.rarity,
    required this.basePriceCoins,
    required this.requiredLevel,
    required this.isStarter,
    this.paintIds = const <String>[],
    this.wheelIds = const <String>[],
    this.tags = const <String>[],
  });

  final String id;
  final String vehicleDefinitionId;
  final String displayName;
  final CarPerformanceClass performanceClass;
  final CarRarity rarity;
  final int basePriceCoins;
  final int requiredLevel;
  final bool isStarter;
  final List<String> paintIds;
  final List<String> wheelIds;
  final List<String> tags;

  factory CarCatalogEntry.fromJson(Map<String, Object?> json) {
    return CarCatalogEntry(
      id: json['id'] as String,
      vehicleDefinitionId: json['vehicleDefinitionId'] as String,
      displayName: json['displayName'] as String,
      performanceClass: CarPerformanceClass.values.byName(
        json['performanceClass'] as String,
      ),
      rarity: CarRarity.values.byName(json['rarity'] as String),
      basePriceCoins: json['basePriceCoins'] as int,
      requiredLevel: json['requiredLevel'] as int,
      isStarter: json['isStarter'] as bool,
      paintIds: _stringList(json['paintIds']),
      wheelIds: _stringList(json['wheelIds']),
      tags: _stringList(json['tags']),
    );
  }

  Map<String, Object?> toJson() => <String, Object?>{
        'id': id,
        'vehicleDefinitionId': vehicleDefinitionId,
        'displayName': displayName,
        'performanceClass': performanceClass.name,
        'rarity': rarity.name,
        'basePriceCoins': basePriceCoins,
        'requiredLevel': requiredLevel,
        'isStarter': isStarter,
        'paintIds': paintIds,
        'wheelIds': wheelIds,
        'tags': tags,
      };

  static List<String> _stringList(Object? value) {
    if (value == null) {
      return const <String>[];
    }
    return List<String>.unmodifiable((value as List<Object?>).cast<String>());
  }
}

/// Versioned transport document that can later be served by Laravel/MySQL
/// without coupling the Flutter client to database tables.
class CarCatalogDocument {
  const CarCatalogDocument({
    required this.schemaVersion,
    required this.cars,
  });

  final int schemaVersion;
  final List<CarCatalogEntry> cars;

  factory CarCatalogDocument.fromJson(Map<String, Object?> json) {
    final rawCars = json['cars'] as List<Object?>;
    return CarCatalogDocument(
      schemaVersion: json['schemaVersion'] as int,
      cars: List<CarCatalogEntry>.unmodifiable(
        rawCars.map(
          (Object? item) => CarCatalogEntry.fromJson(
            Map<String, Object?>.from(item! as Map<Object?, Object?>),
          ),
        ),
      ),
    );
  }

  Map<String, Object?> toJson() => <String, Object?>{
        'schemaVersion': schemaVersion,
        'cars': cars.map((CarCatalogEntry car) => car.toJson()).toList(),
      };

  CarCatalogEntry? findById(String id) {
    for (final car in cars) {
      if (car.id == id) {
        return car;
      }
    }
    return null;
  }
}
