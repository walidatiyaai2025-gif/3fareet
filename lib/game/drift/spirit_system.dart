enum DriftFeedbackLevel { none, spark, arc, supernatural }

enum SpiritMeterState { empty, charging, ready, boosting, cooldown }

typedef SpiritBoolHook = void Function(bool active);
typedef SpiritAudioHook = void Function(String cue);

class SpiritFeedbackHooks {
  const SpiritFeedbackHooks({
    this.onTrailState,
    this.onCameraBoost,
    this.onAudioCue,
  });

  final SpiritBoolHook? onTrailState;
  final SpiritBoolHook? onCameraBoost;
  final SpiritAudioHook? onAudioCue;
}

class SpiritBalance {
  const SpiritBalance({
    required this.maxEnergy,
    required this.minChargeSpeedMps,
    required this.chargePerSecond,
    required this.nitroMinActivationEnergy,
    required this.nitroDrainPerSecond,
    required this.nitroCooldownSeconds,
    required this.nitroAccelerationMps2,
  });

  final double maxEnergy;
  final double minChargeSpeedMps;
  final double chargePerSecond;
  final double nitroMinActivationEnergy;
  final double nitroDrainPerSecond;
  final double nitroCooldownSeconds;
  final double nitroAccelerationMps2;

  static const SpiritBalance prototype = SpiritBalance(
    maxEnergy: 100,
    minChargeSpeedMps: 10,
    chargePerSecond: 18,
    nitroMinActivationEnergy: 22,
    nitroDrainPerSecond: 34,
    nitroCooldownSeconds: 0.75,
    nitroAccelerationMps2: 18,
  );
}

class SpiritSystem {
  SpiritSystem({
    this.balance = SpiritBalance.prototype,
    this.hooks = const SpiritFeedbackHooks(),
  });

  final SpiritBalance balance;
  final SpiritFeedbackHooks hooks;

  double energy = 0;
  double cooldownRemaining = 0;
  bool nitroActive = false;
  DriftFeedbackLevel driftFeedback = DriftFeedbackLevel.none;

  double get normalizedEnergy => (energy / balance.maxEnergy).clamp(0.0, 1.0);

  SpiritMeterState get meterState {
    if (nitroActive) {
      return SpiritMeterState.boosting;
    }
    if (cooldownRemaining > 0) {
      return SpiritMeterState.cooldown;
    }
    if (energy >= balance.nitroMinActivationEnergy) {
      return SpiritMeterState.ready;
    }
    if (energy > 0) {
      return SpiritMeterState.charging;
    }
    return SpiritMeterState.empty;
  }

  double step({
    required double dt,
    required bool isDrifting,
    required double speedMps,
    required double driftIntensity,
    required bool nitroPressed,
  }) {
    if (dt <= 0) {
      return 0;
    }

    cooldownRemaining =
        (cooldownRemaining - dt).clamp(0.0, balance.nitroCooldownSeconds);

    final intensity = driftIntensity.clamp(0.0, 1.0);
    driftFeedback = _feedbackFor(isDrifting ? intensity : 0);

    if (!nitroActive &&
        isDrifting &&
        speedMps >= balance.minChargeSpeedMps) {
      final speedFactor =
          (speedMps / (balance.minChargeSpeedMps * 2.5)).clamp(0.45, 1.35);
      energy += balance.chargePerSecond * intensity * speedFactor * dt;
      energy = energy.clamp(0.0, balance.maxEnergy);
    }

    if (!nitroActive &&
        nitroPressed &&
        cooldownRemaining <= 0 &&
        energy >= balance.nitroMinActivationEnergy) {
      _setNitroActive(true);
    }

    if (nitroActive) {
      energy -= balance.nitroDrainPerSecond * dt;
      energy = energy.clamp(0.0, balance.maxEnergy);
      final boostCurve = 0.78 + (0.22 * normalizedEnergy);
      final acceleration = balance.nitroAccelerationMps2 * boostCurve;

      if (!nitroPressed || energy <= 0) {
        _setNitroActive(false);
        cooldownRemaining = balance.nitroCooldownSeconds;
      }
      return acceleration;
    }

    return 0;
  }

  void reset() {
    if (nitroActive) {
      _setNitroActive(false);
    }
    energy = 0;
    cooldownRemaining = 0;
    driftFeedback = DriftFeedbackLevel.none;
  }

  DriftFeedbackLevel _feedbackFor(double intensity) {
    if (intensity >= 0.72) {
      return DriftFeedbackLevel.supernatural;
    }
    if (intensity >= 0.38) {
      return DriftFeedbackLevel.arc;
    }
    if (intensity > 0.05) {
      return DriftFeedbackLevel.spark;
    }
    return DriftFeedbackLevel.none;
  }

  void _setNitroActive(bool active) {
    if (nitroActive == active) {
      return;
    }
    nitroActive = active;
    hooks.onTrailState?.call(active);
    hooks.onCameraBoost?.call(active);
    hooks.onAudioCue?.call(active ? 'nitro_start' : 'nitro_end');
  }
}
