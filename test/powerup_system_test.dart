import 'package:afareet_asphalt/game/powerups/powerup_system.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  group('PowerUpSystem', () {
    test('spawn point collection fills one inventory slot', () {
      final system = PowerUpSystem(
        spawnPoints: const <PowerUpSpawnPoint>[
          PowerUpSpawnPoint(
            id: 'shield_spawn',
            distanceMeters: 100,
            definition: PowerUpDefinition.eyeShield,
          ),
          PowerUpSpawnPoint(
            id: 'nitro_spawn',
            distanceMeters: 200,
            definition: PowerUpDefinition.nitroSpirit,
          ),
        ],
      );

      expect(system.collect('shield_spawn'), isTrue);
      expect(system.inventory.held?.kind, PowerUpKind.eyeShield);
      expect(system.collect('nitro_spawn'), isFalse);
      expect(system.pickups['shield_spawn']?.available, isFalse);
    });

    test('eye shield activates and absorbs one hit', () {
      final system = PowerUpSystem(
        spawnPoints: const <PowerUpSpawnPoint>[
          PowerUpSpawnPoint(
            id: 'shield_spawn',
            distanceMeters: 100,
            definition: PowerUpDefinition.eyeShield,
          ),
        ],
      );

      expect(system.collect('shield_spawn'), isTrue);
      expect(system.useHeld(), isTrue);
      expect(system.shield.active, isTrue);
      expect(system.shield.absorbHit(), isTrue);
      expect(system.shield.active, isFalse);
      expect(system.shield.absorbHit(), isFalse);
    });

    test('pickups respawn after cooldown', () {
      final system = PowerUpSystem(
        spawnPoints: const <PowerUpSpawnPoint>[
          PowerUpSpawnPoint(
            id: 'shield_spawn',
            distanceMeters: 100,
            definition: PowerUpDefinition.eyeShield,
            respawnSeconds: 2,
          ),
        ],
      );

      expect(system.collect('shield_spawn'), isTrue);
      system.inventory.clear();
      system.step(2.1);
      expect(system.pickups['shield_spawn']?.available, isTrue);
    });
  });
}
