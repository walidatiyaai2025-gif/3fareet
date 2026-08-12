import 'package:afareet_asphalt/game/powerups/powerup_system.dart';
import 'package:flutter_test/flutter_test.dart';

PowerUpSystem systemFor(PowerUpDefinition definition) => PowerUpSystem(
      spawnPoints: <PowerUpSpawnPoint>[
        PowerUpSpawnPoint(
          id: 'spawn',
          distanceMeters: 100,
          definition: definition,
          respawnSeconds: 2,
        ),
      ],
    );

void main() {
  group('PowerUpSystem', () {
    test('collection fills the single race inventory slot', () {
      final system = systemFor(PowerUpDefinition.eyeShield);
      expect(system.collect('spawn'), isTrue);
      expect(system.inventory.held?.kind, PowerUpKind.eyeShield);
      expect(system.collect('spawn'), isFalse);
    });

    test('asphalt shard places and expires a trap', () {
      final system = systemFor(PowerUpDefinition.asphaltShard);
      system.collect('spawn');
      expect(system.useHeld(actorId: 'player-1'), isTrue);
      expect(system.traps.single.ownerId, 'player-1');
      system.step(4.1);
      expect(system.traps, isEmpty);
    });

    test('nitro spirit increases nitro while active', () {
      final system = systemFor(PowerUpDefinition.nitroSpirit);
      system.collect('spawn');
      system.useHeld();
      expect(system.nitroMultiplier, greaterThan(1));
      system.step(3.1);
      expect(system.nitroMultiplier, 1);
    });

    test('traffic curse slows the affected racer', () {
      final system = systemFor(PowerUpDefinition.trafficCurse);
      system.collect('spawn');
      system.useHeld();
      expect(system.speedMultiplier, lessThan(1));
    });

    test('enchanted pound doubles score while active', () {
      final system = systemFor(PowerUpDefinition.enchantedPound);
      system.collect('spawn');
      system.useHeld();
      expect(system.scoreMultiplier, 2);
    });

    test('duration manager expires timed effects', () {
      final manager = PowerUpEffectDurationManager();
      manager.activate(PowerUpDefinition.nitroSpirit);
      expect(manager.isActive(PowerUpKind.nitroSpirit), isTrue);
      expect(manager.step(3.1), contains(PowerUpKind.nitroSpirit));
      expect(manager.isActive(PowerUpKind.nitroSpirit), isFalse);
    });

    test('shield blocks traffic curse through immunity rule', () {
      final manager = PowerUpEffectDurationManager()
        ..activate(PowerUpDefinition.eyeShield);
      const rules = PowerUpRules();
      expect(
        rules.canApply(
          incoming: PowerUpKind.trafficCurse,
          effects: manager,
        ),
        isFalse,
      );
    });

    test('AI usage policy is deterministic for race context', () {
      const policy = ConservativePowerUpAiPolicy();
      expect(
        policy.shouldUse(
          definition: PowerUpDefinition.nitroSpirit,
          raceProgress: 0.8,
          position: 1,
        ),
        isTrue,
      );
      expect(
        policy.shouldUse(
          definition: PowerUpDefinition.enchantedPound,
          raceProgress: 0.1,
          position: 1,
        ),
        isFalse,
      );
    });

    test('event hook exposes VFX/audio lifecycle events', () {
      final events = <PowerUpEvent>[];
      final system = PowerUpSystem(
        spawnPoints: const <PowerUpSpawnPoint>[
          PowerUpSpawnPoint(
            id: 'spawn',
            distanceMeters: 100,
            definition: PowerUpDefinition.nitroSpirit,
          ),
        ],
        eventHook: events.add,
      );
      system.collect('spawn');
      system.useHeld();
      system.step(3.1);
      expect(events.map((event) => event.type), <PowerUpEventType>[
        PowerUpEventType.activated,
        PowerUpEventType.expired,
      ]);
    });

    test('race reset cleans inventory, effects, traps and cooldowns', () {
      final system = systemFor(PowerUpDefinition.asphaltShard);
      system.collect('spawn');
      system.useHeld();
      expect(system.traps, isNotEmpty);
      system.reset();
      expect(system.inventory.isEmpty, isTrue);
      expect(system.traps, isEmpty);
      expect(system.pickups['spawn']?.available, isTrue);
      expect(system.nitroMultiplier, 1);
      expect(system.speedMultiplier, 1);
      expect(system.scoreMultiplier, 1);
    });
  });
}
