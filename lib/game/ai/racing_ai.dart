class RacingLinePoint {
  const RacingLinePoint({
    required this.distanceMeters,
    required this.lateralOffsetMeters,
    required this.targetSpeedKph,
    this.brakingZone = false,
    this.driftZone = false,
  });

  final double distanceMeters;
  final double lateralOffsetMeters;
  final double targetSpeedKph;
  final bool brakingZone;
  final bool driftZone;
}

class RacingLineTarget {
  const RacingLineTarget({
    required this.lateralOffsetMeters,
    required this.targetSpeedKph,
    required this.brakingZone,
    required this.driftZone,
  });

  final double lateralOffsetMeters;
  final double targetSpeedKph;
  final bool brakingZone;
  final bool driftZone;
}

class RacingLine {
  RacingLine({required List<RacingLinePoint> points})
      : points = List<RacingLinePoint>.unmodifiable(points) {
    if (points.length < 2) {
      throw ArgumentError('A racing line needs at least two points.');
    }
    var previous = -1.0;
    for (final point in points) {
      if (point.distanceMeters <= previous) {
        throw ArgumentError('Racing line points must be strictly ordered.');
      }
      previous = point.distanceMeters;
    }
  }

  final List<RacingLinePoint> points;

  factory RacingLine.cairoPrototype(double trackLengthMeters) {
    return RacingLine(
      points: <RacingLinePoint>[
        const RacingLinePoint(
          distanceMeters: 0,
          lateralOffsetMeters: 0,
          targetSpeedKph: 165,
        ),
        RacingLinePoint(
          distanceMeters: trackLengthMeters * 0.18,
          lateralOffsetMeters: -1.8,
          targetSpeedKph: 118,
          brakingZone: true,
        ),
        RacingLinePoint(
          distanceMeters: trackLengthMeters * 0.30,
          lateralOffsetMeters: -2.6,
          targetSpeedKph: 126,
          driftZone: true,
        ),
        RacingLinePoint(
          distanceMeters: trackLengthMeters * 0.48,
          lateralOffsetMeters: 1.4,
          targetSpeedKph: 176,
        ),
        RacingLinePoint(
          distanceMeters: trackLengthMeters * 0.68,
          lateralOffsetMeters: 2.3,
          targetSpeedKph: 122,
          brakingZone: true,
          driftZone: true,
        ),
        RacingLinePoint(
          distanceMeters: trackLengthMeters * 0.84,
          lateralOffsetMeters: 0.4,
          targetSpeedKph: 188,
        ),
      ],
    );
  }

  RacingLineTarget sample(double distanceMeters) {
    final safeDistance = distanceMeters.isFinite ? distanceMeters : 0.0;
    var selected = points.first;
    for (final point in points) {
      if (point.distanceMeters > safeDistance) {
        break;
      }
      selected = point;
    }
    return RacingLineTarget(
      lateralOffsetMeters: selected.lateralOffsetMeters,
      targetSpeedKph: selected.targetSpeedKph,
      brakingZone: selected.brakingZone,
      driftZone: selected.driftZone,
    );
  }
}

class AiDifficultyProfile {
  const AiDifficultyProfile({
    required this.id,
    required this.speedScale,
    required this.reaction,
    required this.aggression,
    required this.mistakeProbability,
  });

  final String id;
  final double speedScale;
  final double reaction;
  final double aggression;
  final double mistakeProbability;

  static const AiDifficultyProfile rookie = AiDifficultyProfile(
    id: 'rookie',
    speedScale: 0.88,
    reaction: 0.62,
    aggression: 0.30,
    mistakeProbability: 0.08,
  );

  static const AiDifficultyProfile street = AiDifficultyProfile(
    id: 'street',
    speedScale: 0.97,
    reaction: 0.78,
    aggression: 0.58,
    mistakeProbability: 0.035,
  );

  static const AiDifficultyProfile legend = AiDifficultyProfile(
    id: 'legend',
    speedScale: 1.04,
    reaction: 0.92,
    aggression: 0.82,
    mistakeProbability: 0.012,
  );
}

class AiPersonality {
  const AiPersonality({
    required this.id,
    required this.displayName,
    required this.overtakeBias,
    required this.defensiveBias,
    required this.nitroBias,
  });

  final String id;
  final String displayName;
  final double overtakeBias;
  final double defensiveBias;
  final double nitroBias;

  static const AiPersonality cairoPhantom = AiPersonality(
    id: 'cairo_phantom',
    displayName: 'Cairo Phantom',
    overtakeBias: 0.75,
    defensiveBias: 0.52,
    nitroBias: 0.72,
  );

  static const AiPersonality nileFox = AiPersonality(
    id: 'nile_fox',
    displayName: 'Nile Fox',
    overtakeBias: 0.58,
    defensiveBias: 0.74,
    nitroBias: 0.55,
  );

  static const AiPersonality desertDjinn = AiPersonality(
    id: 'desert_djinn',
    displayName: 'Desert Djinn',
    overtakeBias: 0.86,
    defensiveBias: 0.38,
    nitroBias: 0.84,
  );
}

enum AiPowerUpIntent { hold, defensive, offensive }

typedef AiPowerUpStrategy = AiPowerUpIntent Function(AiDriverContext context);

class AiDriverContext {
  const AiDriverContext({
    required this.speedKph,
    required this.targetSpeedKph,
    required this.opponentAheadMeters,
    required this.opponentBehindMeters,
  });

  final double speedKph;
  final double targetSpeedKph;
  final double? opponentAheadMeters;
  final double? opponentBehindMeters;
}

class AiDriverSnapshot {
  const AiDriverSnapshot({
    required this.id,
    required this.distanceMeters,
    required this.lateralOffsetMeters,
    required this.speedKph,
    required this.steering,
    required this.throttle,
    required this.brake,
    required this.drifting,
    required this.nitro,
    required this.finished,
  });

  final String id;
  final double distanceMeters;
  final double lateralOffsetMeters;
  final double speedKph;
  final double steering;
  final double throttle;
  final double brake;
  final bool drifting;
  final bool nitro;
  final bool finished;
}

class AiDriver {
  AiDriver({
    required this.id,
    required this.line,
    required this.difficulty,
    required this.personality,
    required this.seed,
    this.powerUpStrategy = defaultPowerUpStrategy,
    this.startLateralOffsetMeters = 0,
  })  : lateralOffsetMeters = startLateralOffsetMeters,
        _rngState = seed & 0x7fffffff;

  final String id;
  final RacingLine line;
  final AiDifficultyProfile difficulty;
  final AiPersonality personality;
  final int seed;
  final AiPowerUpStrategy powerUpStrategy;
  final double startLateralOffsetMeters;

  double distanceMeters = 0;
  double lateralOffsetMeters;
  double speedKph = 0;
  double steering = 0;
  double throttle = 0;
  double brake = 0;
  double nitroEnergy = 100;
  bool drifting = false;
  bool nitro = false;
  bool finished = false;
  double? finishTimeSeconds;

  double _stuckSeconds = 0;
  double _mistakeClock = 0;
  int _rngState;

  AiDriverSnapshot get snapshot => AiDriverSnapshot(
        id: id,
        distanceMeters: distanceMeters,
        lateralOffsetMeters: lateralOffsetMeters,
        speedKph: speedKph,
        steering: steering,
        throttle: throttle,
        brake: brake,
        drifting: drifting,
        nitro: nitro,
        finished: finished,
      );

  void step({
    required double dt,
    required double trackLengthMeters,
    required double raceTimeSeconds,
    required List<AiDriverSnapshot> opponents,
  }) {
    if (finished || dt <= 0 || !dt.isFinite) {
      return;
    }

    final target = line.sample(distanceMeters);
    final ahead = _nearestAhead(opponents);
    final behind = _nearestBehind(opponents);
    var desiredLateral = target.lateralOffsetMeters;

    if (ahead != null && ahead < 18) {
      final side = (id.hashCode & 1) == 0 ? 1.0 : -1.0;
      desiredLateral += side * (1.5 + personality.overtakeBias);
    }
    if (behind != null && behind < 12 && difficulty.aggression > 0.5) {
      desiredLateral *= 1 - (0.22 * personality.defensiveBias);
    }

    final lateralError = desiredLateral - lateralOffsetMeters;
    steering = (lateralError * difficulty.reaction * 0.55)
        .clamp(-1.0, 1.0)
        .toDouble();
    lateralOffsetMeters += steering * dt * (4.2 + difficulty.aggression);
    lateralOffsetMeters = lateralOffsetMeters.clamp(-6.8, 6.8).toDouble();

    var desiredSpeed = target.targetSpeedKph * difficulty.speedScale;
    if (target.brakingZone) {
      desiredSpeed *= 0.82;
    }

    _mistakeClock += dt;
    if (_mistakeClock >= 1) {
      _mistakeClock -= 1;
      if (_random01() < difficulty.mistakeProbability) {
        desiredSpeed *= 0.82;
        steering = (steering + ((_random01() - 0.5) * 0.45))
            .clamp(-1.0, 1.0)
            .toDouble();
      }
    }

    drifting = target.driftZone && speedKph > 70 && steering.abs() > 0.12;
    final powerUpIntent = powerUpStrategy(
      AiDriverContext(
        speedKph: speedKph,
        targetSpeedKph: desiredSpeed,
        opponentAheadMeters: ahead,
        opponentBehindMeters: behind,
      ),
    );
    nitro = !target.brakingZone &&
        nitroEnergy >= 22 &&
        speedKph < desiredSpeed * (0.90 + (0.04 * personality.nitroBias));
    if (powerUpIntent == AiPowerUpIntent.offensive && ahead != null && ahead < 15) {
      nitro = nitro || personality.nitroBias > 0.65;
    }

    if (nitro) {
      nitroEnergy = (nitroEnergy - (32 * dt)).clamp(0.0, 100.0).toDouble();
      desiredSpeed += 24 * personality.nitroBias;
    } else {
      nitroEnergy = (nitroEnergy + (4 * dt)).clamp(0.0, 100.0).toDouble();
    }

    final speedError = desiredSpeed - speedKph;
    throttle = (speedError / 34).clamp(0.0, 1.0).toDouble();
    brake = (-speedError / 42).clamp(0.0, 1.0).toDouble();
    final accelerationKphPerSecond = (48 * throttle) - (72 * brake);
    speedKph = (speedKph + (accelerationKphPerSecond * dt))
        .clamp(0.0, 260.0)
        .toDouble();

    if (speedKph < 4) {
      _stuckSeconds += dt;
    } else {
      _stuckSeconds = 0;
    }
    if (_stuckSeconds >= 2.4) {
      speedKph = 42;
      lateralOffsetMeters = 0;
      steering = 0;
      _stuckSeconds = 0;
    }

    distanceMeters += (speedKph / 3.6) * dt;
    if (distanceMeters >= trackLengthMeters) {
      distanceMeters = trackLengthMeters;
      speedKph = 0;
      throttle = 0;
      brake = 0;
      nitro = false;
      drifting = false;
      finished = true;
      finishTimeSeconds ??= raceTimeSeconds;
    }
  }

  void restart() {
    distanceMeters = 0;
    lateralOffsetMeters = startLateralOffsetMeters;
    speedKph = 0;
    steering = 0;
    throttle = 0;
    brake = 0;
    nitroEnergy = 100;
    drifting = false;
    nitro = false;
    finished = false;
    finishTimeSeconds = null;
    _stuckSeconds = 0;
    _mistakeClock = 0;
    _rngState = seed & 0x7fffffff;
  }

  double? _nearestAhead(List<AiDriverSnapshot> opponents) {
    double? nearest;
    for (final opponent in opponents) {
      if (opponent.id == id || opponent.finished) {
        continue;
      }
      final delta = opponent.distanceMeters - distanceMeters;
      if (delta > 0 && (nearest == null || delta < nearest)) {
        nearest = delta;
      }
    }
    return nearest;
  }

  double? _nearestBehind(List<AiDriverSnapshot> opponents) {
    double? nearest;
    for (final opponent in opponents) {
      if (opponent.id == id || opponent.finished) {
        continue;
      }
      final delta = distanceMeters - opponent.distanceMeters;
      if (delta > 0 && (nearest == null || delta < nearest)) {
        nearest = delta;
      }
    }
    return nearest;
  }

  double _random01() {
    _rngState = ((_rngState * 1103515245) + 12345) & 0x7fffffff;
    return _rngState / 0x7fffffff;
  }

  static AiPowerUpIntent defaultPowerUpStrategy(AiDriverContext context) {
    final ahead = context.opponentAheadMeters;
    if (ahead != null && ahead < 16 && context.speedKph < context.targetSpeedKph) {
      return AiPowerUpIntent.offensive;
    }
    final behind = context.opponentBehindMeters;
    if (behind != null && behind < 10) {
      return AiPowerUpIntent.defensive;
    }
    return AiPowerUpIntent.hold;
  }
}

class AiRacePack {
  AiRacePack({
    required this.trackLengthMeters,
    required List<AiDriver> drivers,
  }) : drivers = List<AiDriver>.unmodifiable(drivers);

  final double trackLengthMeters;
  final List<AiDriver> drivers;

  factory AiRacePack.prototype({required double trackLengthMeters}) {
    final line = RacingLine.cairoPrototype(trackLengthMeters);
    return AiRacePack(
      trackLengthMeters: trackLengthMeters,
      drivers: <AiDriver>[
        AiDriver(
          id: 'ai_cairo_phantom',
          line: line,
          difficulty: AiDifficultyProfile.street,
          personality: AiPersonality.cairoPhantom,
          seed: 1001,
          startLateralOffsetMeters: -2.2,
        ),
        AiDriver(
          id: 'ai_nile_fox',
          line: line,
          difficulty: AiDifficultyProfile.rookie,
          personality: AiPersonality.nileFox,
          seed: 2002,
          startLateralOffsetMeters: 2.2,
        ),
        AiDriver(
          id: 'ai_desert_djinn',
          line: line,
          difficulty: AiDifficultyProfile.legend,
          personality: AiPersonality.desertDjinn,
          seed: 3003,
          startLateralOffsetMeters: -0.8,
        ),
      ],
    );
  }

  List<AiDriverSnapshot> get snapshots =>
      drivers.map((driver) => driver.snapshot).toList(growable: false);

  void step({required double dt, required double raceTimeSeconds}) {
    final frame = snapshots;
    for (final driver in drivers) {
      driver.step(
        dt: dt,
        trackLengthMeters: trackLengthMeters,
        raceTimeSeconds: raceTimeSeconds,
        opponents: frame,
      );
    }
  }

  int playerPosition(double playerDistanceMeters) {
    var ahead = 0;
    for (final driver in drivers) {
      if (driver.finished || driver.distanceMeters > playerDistanceMeters) {
        ahead += 1;
      }
    }
    return ahead + 1;
  }

  void restart() {
    for (final driver in drivers) {
      driver.restart();
    }
  }
}
