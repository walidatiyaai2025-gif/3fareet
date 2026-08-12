import 'package:afareet_asphalt/game/garage/car_catalog.dart';
import 'package:afareet_asphalt/game/garage/garage_controller.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  final catalog = CarCatalog(const <CarCatalogEntry>[
    CarCatalogEntry(
      id: 'cairo_street',
      displayName: 'Cairo Street',
      vehicleClass: VehicleClass.street,
      stats: VehicleStats(
        topSpeed: 70,
        acceleration: 65,
        handling: 80,
        nitro: 55,
      ),
      basePaintId: 'factory',
      unlockLevel: 1,
      previewAsset: 'assets/vehicles/cairo_street.png',
    ),
    CarCatalogEntry(
      id: 'djinn_gt',
      displayName: 'Djinn GT',
      vehicleClass: VehicleClass.spirit,
      stats: VehicleStats(
        topSpeed: 92,
        acceleration: 88,
        handling: 72,
        nitro: 95,
      ),
      basePaintId: 'desert_gold',
      unlockLevel: 8,
    ),
  ]);

  test('garage list exposes locked, selected and equipped state', () {
    final controller = GarageController(catalog: catalog, playerLevel: 1);

    expect(controller.vehicles, hasLength(2));
    expect(controller.vehicles.first.unlocked, isTrue);
    expect(controller.vehicles.first.selected, isTrue);
    expect(controller.vehicles.first.equipped, isTrue);
    expect(controller.vehicles.last.unlocked, isFalse);
  });

  test('detail selection and preview follow selected car', () {
    final controller = GarageController(catalog: catalog, playerLevel: 1);

    expect(controller.selectVehicle('djinn_gt'), isTrue);
    expect(controller.selectedVehicle.displayName, 'Djinn GT');
    expect(controller.preview.vehicleId, 'djinn_gt');
    expect(controller.preview.paintId, 'desert_gold');
  });

  test('locked car cannot customize or equip', () {
    final controller = GarageController(catalog: catalog, playerLevel: 1);
    controller.selectVehicle('djinn_gt');

    expect(controller.setPaint('neon_cyan'), isFalse);
    expect(controller.setWheel('mesh_black'), isFalse);
    expect(controller.equipSelected(), isFalse);
    expect(controller.equippedVehicleId, 'cairo_street');
  });

  test('level unlock enables equip flow', () {
    final controller = GarageController(catalog: catalog, playerLevel: 1);
    controller.selectVehicle('djinn_gt');

    controller.setPlayerLevel(8);

    expect(controller.isUnlocked('djinn_gt'), isTrue);
    expect(controller.equipSelected(), isTrue);
    expect(controller.equippedVehicleId, 'djinn_gt');
  });

  test('paint, wheel, trail and spirit customization update preview', () {
    final controller = GarageController(catalog: catalog, playerLevel: 10);

    expect(controller.setPaint('neon_cyan'), isTrue);
    expect(controller.setWheel('mesh_black'), isTrue);
    expect(controller.setMagicTrail('gold_sparks'), isTrue);
    expect(controller.setSpiritCosmetic('scarab'), isTrue);

    final preview = controller.preview;
    expect(preview.paintId, 'neon_cyan');
    expect(preview.wheelId, 'mesh_black');
    expect(preview.magicTrailId, 'gold_sparks');
    expect(preview.spiritCosmeticId, 'scarab');
  });

  test('invalid customization values are rejected', () {
    final controller = GarageController(catalog: catalog, playerLevel: 10);

    expect(controller.setPaint('unknown'), isFalse);
    expect(controller.setWheel('unknown'), isFalse);
    expect(controller.setMagicTrail('unknown'), isFalse);
    expect(controller.setSpiritCosmetic('unknown'), isFalse);
  });

  test('normalized stat visualization remains bounded', () {
    final controller = GarageController(catalog: catalog, playerLevel: 10);

    expect(controller.normalizedStats.keys,
        containsAll(<String>['Top Speed', 'Acceleration', 'Handling', 'Nitro']));
    expect(controller.normalizedStats.values.every((value) => value >= 0 && value <= 1), isTrue);
  });
}
