enum PowerUpKind {
  eyeShield,
  asphaltShard,
  nitroSpirit,
  trafficCurse,
  enchantedPound,
}

enum PowerUpEventType { activated, expired, blocked, trapPlaced }

class PowerUpDefinition {
  const PowerUpDefinition({
    required this.id,
    required this.kind,
    required this.durationSeconds,
    required this.maxStack,
  });

  final String id;
  final PowerUpKind kind;
  final double durationSeconds;
  final int maxStack;

  static const eyeShield = PowerUpDefinition(
    id: 'eye_shield',
    kind: PowerUpKind.eyeShield,
    durationSeconds: 7,
    maxStack: 1,
  );
  static const asphaltShard = PowerUpDefinition(
    id: 'asphalt_shard',
    kind: PowerUpKind.asphaltShard,
    durationSeconds: 4,
    maxStack: 1,
  );
  static const nitroSpirit = PowerUpDefinition(
    id: 'nitro_spirit',
    kind: PowerUpKind.nitroSpirit,
    durationSeconds: 3,
    maxStack: 1,
  );
  static const trafficCurse = PowerUpDefinition(
    id: 'traffic_curse',
    kind: PowerUpKind.trafficCurse,
    durationSeconds: 5,
    maxStack: 1,
  );
  static const enchantedPound = PowerUpDefinition(
    id: 'enchanted_pound',
    kind: PowerUpKind.enchantedPound,
    durationSeconds: 6,
    maxStack: 1,
  );
}

class PowerUpSpawnPoint {
  const PowerUpSpawnPoint({
    required this.id,
    required this.distanceMeters,
    required this.definition,
    this.respawnSeconds = 12,
  });

  final String id;
  final double distanceMeters;
  final PowerUpDefinition definition;
  final double respawnSeconds;
}

class PowerUpPickup {
  PowerUpPickup({required this.spawnPoint});

  final PowerUpSpawnPoint spawnPoint;
  double cooldownRemaining = 0;

  bool get available => cooldownRemaining <= 0;

  void collect() => cooldownRemaining = spawnPoint.respawnSeconds;

  void step(double dt) {
    if (dt <= 0) return;
    cooldownRemaining = (cooldownRemaining - dt).clamp(0.0, 999).toDouble();
  }
}

class RacePowerUpInventory {
  PowerUpDefinition? held;
  bool get isEmpty => held == null;

  bool collect(PowerUpDefinition definition) {
    if (held != null) return false;
    held = definition;
    return true;
  }

  PowerUpDefinition? consume() {
    final value = held;
    held = null;
    return value;
  }

  void clear() => held = null;
}

class TimedPowerUpEffect {
  TimedPowerUpEffect({required this.definition, required this.remainingSeconds});

  final PowerUpDefinition definition;
  double remainingSeconds;
  bool get active => remainingSeconds > 0;
}

class PowerUpEffectDurationManager {
  final Map<PowerUpKind, TimedPowerUpEffect> _effects = {};

  bool isActive(PowerUpKind kind) => _effects[kind]?.active ?? false;

  double remaining(PowerUpKind kind) => _effects[kind]?.remainingSeconds ?? 0;

  bool activate(PowerUpDefinition definition) {
    final current = _effects[definition.kind];
    if (current != null && current.active && definition.maxStack <= 1) {
      current.remainingSeconds = definition.durationSeconds;
      return false;
    }
    _effects[definition.kind] = TimedPowerUpEffect(
      definition: definition,
      remainingSeconds: definition.durationSeconds,
    );
    return true;
  }

  List<PowerUpKind> step(double dt) {
    if (dt <= 0) return const [];
    final expired = <PowerUpKind>[];
    for (final entry in _effects.entries.toList()) {
      entry.value.remainingSeconds =
          (entry.value.remainingSeconds - dt).clamp(0.0, 999).toDouble();
      if (!entry.value.active) {
        expired.add(entry.key);
        _effects.remove(entry.key);
      }
    }
    return expired;
  }

  void clear() => _effects.clear();
}

class PowerUpRules {
  const PowerUpRules();

  bool canApply({
    required PowerUpKind incoming,
    required PowerUpEffectDurationManager effects,
  }) {
    if (incoming == PowerUpKind.trafficCurse &&
        effects.isActive(PowerUpKind.eyeShield)) {
      return false;
    }
    return true;
  }
}

class AsphaltShardTrap {
  AsphaltShardTrap({required this.ownerId, required this.remainingSeconds});

  final String ownerId;
  double remainingSeconds;
  bool get active => remainingSeconds > 0;
}

abstract interface class PowerUpAiUsagePolicy {
  bool shouldUse({
    required PowerUpDefinition definition,
    required double raceProgress,
    required int position,
  });
}

class ConservativePowerUpAiPolicy implements PowerUpAiUsagePolicy {
  const ConservativePowerUpAiPolicy();

  @override
  bool shouldUse({
    required PowerUpDefinition definition,
    required double raceProgress,
    required int position,
  }) {
    return switch (definition.kind) {
      PowerUpKind.nitroSpirit => raceProgress > 0.65 || position > 2,
      PowerUpKind.trafficCurse => position > 1,
      PowerUpKind.asphaltShard => position <= 2,
      PowerUpKind.eyeShield => true,
      PowerUpKind.enchantedPound => raceProgress > 0.4,
    };
  }
}

class PowerUpEvent {
  const PowerUpEvent(this.type, this.kind);
  final PowerUpEventType type;
  final PowerUpKind kind;
}

typedef PowerUpEventHook = void Function(PowerUpEvent event);

class PowerUpSystem {
  PowerUpSystem({
    required List<PowerUpSpawnPoint> spawnPoints,
    this.eventHook,
    this.rules = const PowerUpRules(),
  }) : pickups = {
          for (final point in spawnPoints) point.id: PowerUpPickup(spawnPoint: point),
        };

  final Map<String, PowerUpPickup> pickups;
  final RacePowerUpInventory inventory = RacePowerUpInventory();
  final PowerUpEffectDurationManager effects = PowerUpEffectDurationManager();
  final PowerUpRules rules;
  final PowerUpEventHook? eventHook;
  final List<AsphaltShardTrap> traps = [];

  bool get shieldActive => effects.isActive(PowerUpKind.eyeShield);
  double get nitroMultiplier => effects.isActive(PowerUpKind.nitroSpirit) ? 1.35 : 1.0;
  double get speedMultiplier => effects.isActive(PowerUpKind.trafficCurse) ? 0.72 : 1.0;
  double get scoreMultiplier => effects.isActive(PowerUpKind.enchantedPound) ? 2.0 : 1.0;

  bool collect(String spawnPointId) {
    final pickup = pickups[spawnPointId];
    if (pickup == null || !pickup.available || !inventory.isEmpty) return false;
    if (!inventory.collect(pickup.spawnPoint.definition)) return false;
    pickup.collect();
    return true;
  }

  bool useHeld({String actorId = 'player'}) {
    final definition = inventory.consume();
    if (definition == null) return false;
    if (!rules.canApply(incoming: definition.kind, effects: effects)) {
      eventHook?.call(PowerUpEvent(PowerUpEventType.blocked, definition.kind));
      return false;
    }

    if (definition.kind == PowerUpKind.asphaltShard) {
      traps.add(AsphaltShardTrap(
        ownerId: actorId,
        remainingSeconds: definition.durationSeconds,
      ));
      eventHook?.call(PowerUpEvent(PowerUpEventType.trapPlaced, definition.kind));
      return true;
    }

    effects.activate(definition);
    eventHook?.call(PowerUpEvent(PowerUpEventType.activated, definition.kind));
    return true;
  }

  bool absorbHostileEffect() {
    if (!shieldActive) return false;
    effects.clear();
    return true;
  }

  void step(double dt) {
    if (dt <= 0) return;
    for (final pickup in pickups.values) {
      pickup.step(dt);
    }
    for (final kind in effects.step(dt)) {
      eventHook?.call(PowerUpEvent(PowerUpEventType.expired, kind));
    }
    for (final trap in traps.toList()) {
      trap.remainingSeconds =
          (trap.remainingSeconds - dt).clamp(0.0, 999).toDouble();
      if (!trap.active) traps.remove(trap);
    }
  }

  void reset() {
    inventory.clear();
    effects.clear();
    traps.clear();
    for (final pickup in pickups.values) {
      pickup.cooldownRemaining = 0;
    }
  }
}
