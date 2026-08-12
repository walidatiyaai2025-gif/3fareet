import 'package:afareet_asphalt/game/garage/car_catalog.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  group('GAR-001 CarCatalog schema', () {
    test('entry round-trips through JSON transport shape', () {
      const entry = CarCatalogEntry(
        id: 'cairo_drift_01',
        vehicleDefinitionId: 'afareet_proto_01',
        displayName: 'Cairo Drift',
        performanceClass: CarPerformanceClass.drift,
        rarity: CarRarity.rare,
        basePriceCoins: 12000,
        requiredLevel: 4,
        isStarter: false,
        paintIds: <String>['obsidian', 'copper'],
        wheelIds: <String>['mesh_01'],
        tags: <String>['egypt', 'drift'],
      );

      final decoded = CarCatalogEntry.fromJson(entry.toJson());

      expect(decoded.id, entry.id);
      expect(decoded.vehicleDefinitionId, 'afareet_proto_01');
      expect(decoded.performanceClass, CarPerformanceClass.drift);
      expect(decoded.rarity, CarRarity.rare);
      expect(decoded.paintIds, <String>['obsidian', 'copper']);
    });

    test('versioned catalog document resolves cars by stable id', () {
      final document = CarCatalogDocument.fromJson(<String, Object?>{
        'schemaVersion': 1,
        'cars': <Object?>[
          <String, Object?>{
            'id': 'starter_01',
            'vehicleDefinitionId': 'afareet_proto_01',
            'displayName': 'Afreet Prototype',
            'performanceClass': 'street',
            'rarity': 'common',
            'basePriceCoins': 0,
            'requiredLevel': 1,
            'isStarter': true,
            'paintIds': <Object?>['black'],
            'wheelIds': <Object?>['stock'],
            'tags': <Object?>['starter'],
          },
        ],
      });

      expect(document.schemaVersion, 1);
      expect(document.findById('starter_01')?.isStarter, isTrue);
      expect(document.findById('missing'), isNull);
      expect(document.toJson()['cars'], isA<List<Map<String, Object?>>>());
    });
  });
}
