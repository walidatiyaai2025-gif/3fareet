import 'package:afareet_asphalt/game/garage/car_catalog.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  test('catalog validates entries and unlock filtering', () {
    const starter = CarCatalogEntry(
      id: 'cairo_runner',
      displayName: 'Cairo Runner',
      vehicleClass: VehicleClass.street,
      stats: VehicleStats(
        topSpeed: 62,
        acceleration: 70,
        handling: 76,
        nitro: 58,
      ),
      basePaintId: 'midnight_blue',
      unlockLevel: 1,
    );
    const advanced = CarCatalogEntry(
      id: 'ifrit_gt',
      displayName: 'Ifrit GT',
      vehicleClass: VehicleClass.spirit,
      stats: VehicleStats(
        topSpeed: 88,
        acceleration: 84,
        handling: 72,
        nitro: 91,
      ),
      basePaintId: 'ember_red',
      unlockLevel: 5,
    );

    final catalog = CarCatalog(const [starter, advanced]);
    expect(catalog.byId('cairo_runner'), starter);
    expect(catalog.unlockedAt(1), [starter]);
    expect(catalog.unlockedAt(5), [starter, advanced]);
  });

  test('catalog rejects duplicate IDs', () {
    const entry = CarCatalogEntry(
      id: 'duplicate',
      displayName: 'Duplicate',
      vehicleClass: VehicleClass.street,
      stats: VehicleStats(
        topSpeed: 50,
        acceleration: 50,
        handling: 50,
        nitro: 50,
      ),
      basePaintId: 'base',
      unlockLevel: 1,
    );
    expect(() => CarCatalog(const [entry, entry]), throwsArgumentError);
  });
}
