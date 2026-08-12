enum PowerUpKind {
  eyeShield,
  asphaltShard,
  nitroSpirit,
  trafficCurse,
  enchantedPound,
}

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

  static const PowerUpDefinition eyeShield = PowerUpDefinition(
    id: 'eye_shield',
    kind: PowerUpKind.eyeShield,
    durationSeconds: 7,
    maxStack: 1,
  );

  static const PowerUpDefinition asphaltShard = PowerUpDefinition(
    id: 'asphalt_shard',
    kind: PowerUpKind.asphaltShard,
    durationSeconds: 4,
    maxStack: 1,
  );

  static const PowerUpDefinition nitroSpirit = PowerUpDefinition(
    id: 'nitro_spirit',
    kind: PowerUpKind.nitroSpirit,
    durationSeconds: 3,
    maxStack: 1,
  );

  static const PowerUpDefinition trafficCurse = PowerUpDefinition(
    id: 'traffic_curse',
    kind: PowerUpKind.trafficCurse,
    durationSeconds: 5,
    maxStack: 1,
  );

  static const PowerUpDefinition enchantedPound = PowerUpDefinition(
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

  void collect() {
    cooldownRemaining = spawnPoint.respawnSeconds;
  }

  void step(double dt) {
    if (dt <= 0) {
      return;
    }
    cooldownRemaining = (cooldownRemaining - dt).clamp(0.0, 999).toDouble();
  }
}

class RacePowerUpInventory {
  PowerUpDefinition? held;

  bool get isEmpty => held == null;

  bool collect(PowerUpDefinition definition) {
    if (held != null) {
      return false;
    }
    held = definition;
    return true;
  }

  PowerUpDefinition? consume() {
    final value = held;
    held = null;
    return value;
  }

  void clear() {
    held = null;
  }
}

class EyeShieldState {
  double remainingSeconds = 0;
  int hitsRemaining = 0;

  bool get active => remainingSeconds > 0 && hitsRemaining > 0;

  void activate(double durationSeconds) {
    remainingSeconds = durationSeconds.clamp(0.0, 30.0).toDouble();
    hitsRemaining = remainingSeconds > 0 ? 1 : 0;
  }

  bool absorbHit() {
    if (!active) {
      return false;
    }
    hitsRemaining = 0;
    remainingSeconds = 0;
    return true;
  }

  void step(double dt) {
    if (dt <= 0 || !active) {
      return;
    }
    remainingSeconds = (remainingSeconds - dt).clamp(0.0, 30.0).toDouble();
    if (remainingSeconds <= 0) {
      hitsRemaining = 0;
    }
  }

  void reset() {
    remainingSeconds = 0;
    hitsRemaining = 0;
  }
}

class PowerUpSystem {
  PowerUpSystem({required List<PowerUpSpawnPoint> spawnPoints})
      : pickups = <String, PowerUpPickup>{
          for (final point in spawnPoints) point.id: PowerUpPickup(spawnPoint: point),
        };

  final Map<String, PowerUpPickup> pickups;
  final RacePowerUpInventory inventory = RacePowerUpInventory();
  final EyeShieldState shield = EyeShieldState();

  bool collect(String spawnPointId) {
    final pickup = pickups[spawnPointId];
    if (pickup == null || !pickup.available || !inventory.isEmpty) {
      return false;
    }
    if (!inventory.collect(pickup.spawnPoint.definition)) {
      return false;
    }
    pickup.collect();
    return true;
  }

  bool useHeld() {
    final definition = inventory.consume();
    if (definition == null) {
      return false;
    }
    if (definition.kind == PowerUpKind.eyeShield) {
      shield.activate(definition.durationSeconds);
    }
    return true;
  }

  void step(double dt) {
    for (final pickup in pickups.values) {
      pickup.step(dt);
    }
    shield.step(dt);
  }

  void reset() {
    inventory.clear();
    shield.reset();
    for (final pickup in pickups.values) {
      pickup.cooldownRemaining = 0;
    }
  }
}
