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

enum EffectActivationResult { applied, stacked, refreshed }

class ActivePowerUpEffect {
  ActivePowerUpEffect({
    required this.definition,
    required this.remainingSeconds,
    required this.stackCount,
  });

  final PowerUpDefinition definition;
  double remainingSeconds;
  int stackCount;
}

/// Pure-Dart duration/stacking state. Rendering and vehicle simulation consume
/// the exposed modifiers instead of owning effect timers themselves.
class PowerUpEffectDurationManager {
  final Map<PowerUpKind, ActivePowerUpEffect> _active =
      <PowerUpKind, ActivePowerUpEffect>{};

  bool isActive(PowerUpKind kind) => _active.containsKey(kind);

  double remainingSeconds(PowerUpKind kind) =>
      _active[kind]?.remainingSeconds ?? 0;

  int stackCount(PowerUpKind kind) => _active[kind]?.stackCount ?? 0;

  Iterable<PowerUpKind> get activeKinds => _active.keys;

  EffectActivationResult activate(PowerUpDefinition definition) {
    final existing = _active[definition.kind];
    if (existing == null) {
      _active[definition.kind] = ActivePowerUpEffect(
        definition: definition,
        remainingSeconds: definition.durationSeconds,
        stackCount: 1,
      );
      return EffectActivationResult.applied;
    }

    existing.remainingSeconds = definition.durationSeconds;
    if (existing.stackCount < definition.maxStack) {
      existing.stackCount += 1;
      return EffectActivationResult.stacked;
    }
    return EffectActivationResult.refreshed;
  }

  List<PowerUpKind> step(double dt) {
    if (dt <= 0 || _active.isEmpty) {
      return const <PowerUpKind>[];
    }

    final expired = <PowerUpKind>[];
    for (final entry in _active.entries.toList(growable: false)) {
      entry.value.remainingSeconds =
          (entry.value.remainingSeconds - dt).clamp(0.0, 999).toDouble();
      if (entry.value.remainingSeconds <= 0) {
        expired.add(entry.key);
        _active.remove(entry.key);
      }
    }
    return expired;
  }

  void clear() => _active.clear();
}

class AsphaltShardTrapState {
  double remainingSeconds = 0;

  bool get active => remainingSeconds > 0;

  void deploy(double lifetimeSeconds) {
    remainingSeconds = lifetimeSeconds.clamp(0.0, 30.0).toDouble();
  }

  bool trigger() {
    if (!active) {
      return false;
    }
    remainingSeconds = 0;
    return true;
  }

  void step(double dt) {
    if (dt <= 0 || !active) {
      return;
    }
    remainingSeconds = (remainingSeconds - dt).clamp(0.0, 30.0).toDouble();
  }

  void reset() {
    remainingSeconds = 0;
  }
}

enum PowerUpFeedbackType { activated, deployed, hit, blocked, expired, reset }

class PowerUpFeedbackEvent {
  const PowerUpFeedbackEvent({
    required this.type,
    this.kind,
    this.sourceId,
    this.targetId,
  });

  final PowerUpFeedbackType type;
  final PowerUpKind? kind;
  final String? sourceId;
  final String? targetId;
}

typedef PowerUpFeedbackSink = void Function(PowerUpFeedbackEvent event);

class PowerUpAiUsageContext {
  const PowerUpAiUsageContext({
    required this.speedRatio,
    required this.raceProgress,
    this.rivalAheadGapMeters = double.infinity,
    this.rivalBehindGapMeters = double.infinity,
    this.onStraight = false,
    this.incomingThreat = false,
    this.underNegativeEffect = false,
  });

  final double speedRatio;
  final double raceProgress;
  final double rivalAheadGapMeters;
  final double rivalBehindGapMeters;
  final bool onStraight;
  final bool incomingThreat;
  final bool underNegativeEffect;
}

abstract interface class PowerUpAiUsagePolicy {
  bool shouldUse(
    PowerUpDefinition held,
    PowerUpAiUsageContext context,
  );
}

class DefaultPowerUpAiUsagePolicy implements PowerUpAiUsagePolicy {
  const DefaultPowerUpAiUsagePolicy();

  @override
  bool shouldUse(
    PowerUpDefinition held,
    PowerUpAiUsageContext context,
  ) {
    return switch (held.kind) {
      PowerUpKind.eyeShield =>
        context.incomingThreat || context.underNegativeEffect,
      PowerUpKind.asphaltShard => context.rivalBehindGapMeters <= 14,
      PowerUpKind.nitroSpirit =>
        context.onStraight &&
            context.speedRatio >= 0.35 &&
            !context.underNegativeEffect,
      PowerUpKind.trafficCurse => context.rivalAheadGapMeters <= 25,
      PowerUpKind.enchantedPound => context.raceProgress >= 0.65,
    };
  }
}

class PowerUpSystem {
  PowerUpSystem({
    required List<PowerUpSpawnPoint> spawnPoints,
    this.racerId = 'player',
    this.feedbackSink,
  }) : pickups = <String, PowerUpPickup>{
          for (final point in spawnPoints)
            point.id: PowerUpPickup(spawnPoint: point),
        };

  final String racerId;
  final PowerUpFeedbackSink? feedbackSink;
  final Map<String, PowerUpPickup> pickups;
  final RacePowerUpInventory inventory = RacePowerUpInventory();
  final EyeShieldState shield = EyeShieldState();
  final PowerUpEffectDurationManager effects = PowerUpEffectDurationManager();
  final AsphaltShardTrapState asphaltTrap = AsphaltShardTrapState();

  double get nitroPowerMultiplier =>
      effects.isActive(PowerUpKind.nitroSpirit) ? 1.35 : 1.0;

  double get speedMultiplier =>
      effects.isActive(PowerUpKind.trafficCurse) ? 0.72 : 1.0;

  double get handlingMultiplier =>
      effects.isActive(PowerUpKind.asphaltShard) ? 0.68 : 1.0;

  double get rewardMultiplier =>
      effects.isActive(PowerUpKind.enchantedPound) ? 2.0 : 1.0;

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

  bool useHeld({PowerUpSystem? target}) {
    final definition = inventory.held;
    if (definition == null) {
      return false;
    }
    if (definition.kind == PowerUpKind.trafficCurse && target == null) {
      return false;
    }

    inventory.consume();
    switch (definition.kind) {
      case PowerUpKind.eyeShield:
        shield.activate(definition.durationSeconds);
        _emit(PowerUpFeedbackType.activated, definition.kind);
      case PowerUpKind.asphaltShard:
        asphaltTrap.deploy(definition.durationSeconds);
        _emit(PowerUpFeedbackType.deployed, definition.kind);
      case PowerUpKind.nitroSpirit:
      case PowerUpKind.enchantedPound:
        effects.activate(definition);
        _emit(PowerUpFeedbackType.activated, definition.kind);
      case PowerUpKind.trafficCurse:
        target!.applyIncoming(definition, sourceId: racerId);
        _emit(
          PowerUpFeedbackType.activated,
          definition.kind,
          targetId: target.racerId,
        );
    }
    return true;
  }

  /// Consumes a deployed asphalt trap and applies its handling penalty to the
  /// target. A live Eye Shield absorbs the trap and still consumes it.
  bool triggerAsphaltTrap(PowerUpSystem target) {
    if (!asphaltTrap.trigger()) {
      return false;
    }
    final applied = target.applyIncoming(
      PowerUpDefinition.asphaltShard,
      sourceId: racerId,
    );
    _emit(
      PowerUpFeedbackType.hit,
      PowerUpKind.asphaltShard,
      targetId: target.racerId,
    );
    return applied;
  }

  /// Applies an offensive effect while enforcing immunity and stacking rules.
  bool applyIncoming(
    PowerUpDefinition definition, {
    String? sourceId,
  }) {
    final offensive = definition.kind == PowerUpKind.asphaltShard ||
        definition.kind == PowerUpKind.trafficCurse;
    if (!offensive) {
      return false;
    }

    if (shield.absorbHit()) {
      _emit(
        PowerUpFeedbackType.blocked,
        definition.kind,
        sourceId: sourceId,
      );
      return false;
    }

    effects.activate(definition);
    _emit(
      PowerUpFeedbackType.hit,
      definition.kind,
      sourceId: sourceId,
    );
    return true;
  }

  void step(double dt) {
    for (final pickup in pickups.values) {
      pickup.step(dt);
    }
    shield.step(dt);
    asphaltTrap.step(dt);
    final expired = effects.step(dt);
    for (final kind in expired) {
      _emit(PowerUpFeedbackType.expired, kind);
    }
  }

  void reset() {
    inventory.clear();
    shield.reset();
    effects.clear();
    asphaltTrap.reset();
    for (final pickup in pickups.values) {
      pickup.cooldownRemaining = 0;
    }
    _emit(PowerUpFeedbackType.reset, null);
  }

  void _emit(
    PowerUpFeedbackType type,
    PowerUpKind? kind, {
    String? sourceId,
    String? targetId,
  }) {
    feedbackSink?.call(
      PowerUpFeedbackEvent(
        type: type,
        kind: kind,
        sourceId: sourceId ?? racerId,
        targetId: targetId,
      ),
    );
  }
}
