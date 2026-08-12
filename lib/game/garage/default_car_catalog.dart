import 'package:afareet_asphalt/game/garage/car_catalog.dart';

CarCatalog buildDefaultCarCatalog() => CarCatalog(const <CarCatalogEntry>[
      CarCatalogEntry(
        id: 'cairo_street_runner',
        displayName: 'Cairo Street Runner',
        vehicleClass: VehicleClass.street,
        stats: VehicleStats(topSpeed: 68, acceleration: 72, handling: 82, nitro: 64),
        basePaintId: 'factory',
        unlockLevel: 1,
      ),
      CarCatalogEntry(
        id: 'desert_muscle',
        displayName: 'Desert Muscle',
        vehicleClass: VehicleClass.muscle,
        stats: VehicleStats(topSpeed: 78, acceleration: 76, handling: 58, nitro: 70),
        basePaintId: 'desert_gold',
        unlockLevel: 3,
      ),
      CarCatalogEntry(
        id: 'neon_exotic',
        displayName: 'Neon Exotic',
        vehicleClass: VehicleClass.exotic,
        stats: VehicleStats(topSpeed: 90, acceleration: 86, handling: 76, nitro: 84),
        basePaintId: 'neon_cyan',
        unlockLevel: 6,
      ),
      CarCatalogEntry(
        id: 'djinn_spirit',
        displayName: 'Djinn Spirit',
        vehicleClass: VehicleClass.spirit,
        stats: VehicleStats(topSpeed: 86, acceleration: 90, handling: 88, nitro: 96),
        basePaintId: 'neon_cyan',
        unlockLevel: 10,
      ),
    ]);

List<String> validateCarCatalog(CarCatalog catalog) {
  final errors = <String>[];
  if (catalog.entries.length < 4) {
    errors.add('Catalog must expose at least four vehicle archetypes.');
  }
  final classes = catalog.entries.map((entry) => entry.vehicleClass).toSet();
  for (final vehicleClass in VehicleClass.values) {
    if (!classes.contains(vehicleClass)) {
      errors.add('Missing vehicle class: ${vehicleClass.name}.');
    }
  }
  if (!catalog.entries.any((entry) => entry.unlockLevel == 1)) {
    errors.add('At least one vehicle must unlock at level 1.');
  }
  return errors;
}
