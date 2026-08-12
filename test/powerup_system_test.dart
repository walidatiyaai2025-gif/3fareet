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

    test('PWR-006 asphalt shard deploys trap and penalizes target handling', () {
      final attacker = PowerUpSystem(
        racerId: 'ai-1',
        spawnPoints: const <PowerUpSpawnPoint>[
          PowerUpSpawnPoint(
            id: 'trap',
            distanceMeters: 40,
            definition: PowerUpDefinition.asphaltShard,
          ),
        ],
      );
      final target = PowerUpSystem(racerId: 'player', spawnPoints: const []);

      expect(attacker.collect('trap'), isTrue);
      expect(attacker.useHeld(), isTrue);
      expect(attacker.asphaltTrap.active, isTrue);
      expect(attacker.triggerAsphaltTrap(target), isTrue);
      expect(attacker.asphaltTrap.active, isFalse);
      expect(target.handlingMultiplier, lessThan(1));

      target.step(PowerUpDefinition.asphaltShard.durationSeconds + 0.1);
      expect(target.handlingMultiplier, 1);
    });

    test('PWR-007 nitro spirit exposes timed power multiplier', () {
      final system = PowerUpSystem(
        spawnPoints: const <PowerUpSpawnPoint>[
          PowerUpSpawnPoint(
            id: 'nitro',
            distanceMeters: 50,
            definition: PowerUpDefinition.nitroSpirit,
          ),
        ],
      );

      expect(system.collect('nitro'), isTrue);
      expect(system.useHeld(), isTrue);
      expect(system.nitroPowerMultiplier, greaterThan(1));
      system.step(PowerUpDefinition.nitroSpirit.durationSeconds + 0.1);
      expect(system.nitroPowerMultiplier, 1);
    });

    test('PWR-008 traffic curse slows target and shield grants immunity', () {
      final attacker = PowerUpSystem(
        racerId: 'ai-2',
        spawnPoints: const <PowerUpSpawnPoint>[
          PowerUpSpawnPoint(
            id: 'curse',
            distanceMeters: 80,
            definition: PowerUpDefinition.trafficCurse,
          ),
        ],
      );
      final target = PowerUpSystem(racerId: 'player', spawnPoints: const []);
      target.shield.activate(PowerUpDefinition.eyeShield.durationSeconds);

      expect(attacker.collect('curse'), isTrue);
      expect(attacker.useHeld(target: target), isTrue);
      expect(target.speedMultiplier, 1);
      expect(target.shield.active, isFalse);

      expect(
        target.applyIncoming(
          PowerUpDefinition.trafficCurse,
          sourceId: attacker.racerId,
        ),
        isTrue,
      );
      expect(target.speedMultiplier, lessThan(1));
    });

    test('PWR-009 enchanted pound enables timed reward multiplier', () {
      final system = PowerUpSystem(
        spawnPoints: const <PowerUpSpawnPoint>[
          PowerUpSpawnPoint(
            id: 'pound',
            distanceMeters: 120,
            definition: PowerUpDefinition.enchantedPound,
          ),
        ],
      );

      expect(system.collect('pound'), isTrue);
      expect(system.useHeld(), isTrue);
      expect(system.rewardMultiplier, 2);
      system.step(PowerUpDefinition.enchantedPound.durationSeconds + 0.1);
      expect(system.rewardMultiplier, 1);
    });

    test('PWR-010 and PWR-011 duration manager stacks and caps effects', () {
      const stackable = PowerUpDefinition(
        id: 'stackable_nitro',
        kind: PowerUpKind.nitroSpirit,
        durationSeconds: 3,
        maxStack: 2,
      );
      final manager = PowerUpEffectDurationManager();

      expect(manager.activate(stackable), EffectActivationResult.applied);
      expect(manager.activate(stackable), EffectActivationResult.stacked);
      expect(manager.stackCount(PowerUpKind.nitroSpirit), 2);
      expect(manager.activate(stackable), EffectActivationResult.refreshed);
      expect(manager.stackCount(PowerUpKind.nitroSpirit), 2);

      manager.step(3.1);
      expect(manager.isActive(PowerUpKind.nitroSpirit), isFalse);
    });

    test('PWR-012 default AI policy uses race context per power-up', () {
      const policy = DefaultPowerUpAiUsagePolicy();
      const context = PowerUpAiUsageContext(
        speedRatio: 0.7,
        raceProgress: 0.8,
        rivalAheadGapMeters: 12,
        rivalBehindGapMeters: 10,
        onStraight: true,
        incomingThreat: true,
      );

      expect(policy.shouldUse(PowerUpDefinition.eyeShield, context), isTrue);
      expect(policy.shouldUse(PowerUpDefinition.asphaltShard, context), isTrue);
      expect(policy.shouldUse(PowerUpDefinition.nitroSpirit, context), isTrue);
      expect(policy.shouldUse(PowerUpDefinition.trafficCurse, context), isTrue);
      expect(policy.shouldUse(PowerUpDefinition.enchantedPound, context), isTrue);
    });

    test('PWR-013 feedback hook reports activation, block and expiry', () {
      final events = <PowerUpFeedbackEvent>[];
      final system = PowerUpSystem(
        racerId: 'player',
        feedbackSink: events.add,
        spawnPoints: const <PowerUpSpawnPoint>[
          PowerUpSpawnPoint(
            id: 'nitro',
            distanceMeters: 20,
            definition: PowerUpDefinition.nitroSpirit,
          ),
        ],
      );

      expect(system.collect('nitro'), isTrue);
      expect(system.useHeld(), isTrue);
      system.step(PowerUpDefinition.nitroSpirit.durationSeconds + 0.1);

      expect(events.first.type, PowerUpFeedbackType.activated);
      expect(events.first.kind, PowerUpKind.nitroSpirit);
      expect(events.last.type, PowerUpFeedbackType.expired);
    });

    test('PWR-014 reset cleans race-scoped inventory, effects and traps', () {
      final system = PowerUpSystem(
        spawnPoints: const <PowerUpSpawnPoint>[
          PowerUpSpawnPoint(
            id: 'trap',
            distanceMeters: 20,
            definition: PowerUpDefinition.asphaltShard,
          ),
        ],
      );
      system.effects.activate(PowerUpDefinition.nitroSpirit);
      system.shield.activate(3);
      expect(system.collect('trap'), isTrue);
      expect(system.useHeld(), isTrue);

      system.reset();

      expect(system.inventory.isEmpty, isTrue);
      expect(system.effects.activeKinds, isEmpty);
      expect(system.shield.active, isFalse);
      expect(system.asphaltTrap.active, isFalse);
      expect(system.pickups['trap']?.available, isTrue);
    });
  });
}
