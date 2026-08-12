class VehicleDefinition {
  const VehicleDefinition({
    required this.id,
    required this.displayName,
    required this.maxForwardSpeedMps,
    required this.maxReverseSpeedMps,
    required this.accelerationMps2,
    required this.brakeDecelerationMps2,
    required this.reverseAccelerationMps2,
    required this.coastDecelerationMps2,
    required this.steeringRateLowSpeedRad,
    required this.steeringRateHighSpeedRad,
    required this.steeringHighSpeedStartMps,
    required this.gripRecoveryPerSecond,
    required this.driftGripRecoveryPerSecond,
    required this.driftEntryMinSpeedMps,
    required this.driftExitMinSpeedMps,
    required this.driftSlipBuildMps2,
    required this.driftExitStabilizationPerSecond,
    required this.maxLateralSlipMps,
    required this.offTrackDecelerationMps2,
    required this.collisionSpeedRetention,
  });

  final String id;
  final String displayName;
  final double maxForwardSpeedMps;
  final double maxReverseSpeedMps;
  final double accelerationMps2;
  final double brakeDecelerationMps2;
  final double reverseAccelerationMps2;
  final double coastDecelerationMps2;
  final double steeringRateLowSpeedRad;
  final double steeringRateHighSpeedRad;
  final double steeringHighSpeedStartMps;
  final double gripRecoveryPerSecond;
  final double driftGripRecoveryPerSecond;
  final double driftEntryMinSpeedMps;
  final double driftExitMinSpeedMps;
  final double driftSlipBuildMps2;
  final double driftExitStabilizationPerSecond;
  final double maxLateralSlipMps;
  final double offTrackDecelerationMps2;
  final double collisionSpeedRetention;

  VehicleDefinition copyWith({
    double? maxForwardSpeedMps,
    double? accelerationMps2,
    double? gripRecoveryPerSecond,
    double? driftSlipBuildMps2,
  }) {
    return VehicleDefinition(
      id: id,
      displayName: displayName,
      maxForwardSpeedMps: maxForwardSpeedMps ?? this.maxForwardSpeedMps,
      maxReverseSpeedMps: maxReverseSpeedMps,
      accelerationMps2: accelerationMps2 ?? this.accelerationMps2,
      brakeDecelerationMps2: brakeDecelerationMps2,
      reverseAccelerationMps2: reverseAccelerationMps2,
      coastDecelerationMps2: coastDecelerationMps2,
      steeringRateLowSpeedRad: steeringRateLowSpeedRad,
      steeringRateHighSpeedRad: steeringRateHighSpeedRad,
      steeringHighSpeedStartMps: steeringHighSpeedStartMps,
      gripRecoveryPerSecond:
          gripRecoveryPerSecond ?? this.gripRecoveryPerSecond,
      driftGripRecoveryPerSecond: driftGripRecoveryPerSecond,
      driftEntryMinSpeedMps: driftEntryMinSpeedMps,
      driftExitMinSpeedMps: driftExitMinSpeedMps,
      driftSlipBuildMps2: driftSlipBuildMps2 ?? this.driftSlipBuildMps2,
      driftExitStabilizationPerSecond: driftExitStabilizationPerSecond,
      maxLateralSlipMps: maxLateralSlipMps,
      offTrackDecelerationMps2: offTrackDecelerationMps2,
      collisionSpeedRetention: collisionSpeedRetention,
    );
  }
}

abstract final class PrototypeVehiclePreset {
  static const VehicleDefinition definition = VehicleDefinition(
    id: 'afareet_proto_01',
    displayName: 'Afreet Prototype',
    maxForwardSpeedMps: 52,
    maxReverseSpeedMps: 10,
    accelerationMps2: 13.5,
    brakeDecelerationMps2: 24,
    reverseAccelerationMps2: 8,
    coastDecelerationMps2: 2.2,
    steeringRateLowSpeedRad: 2.65,
    steeringRateHighSpeedRad: 1.1,
    steeringHighSpeedStartMps: 42,
    gripRecoveryPerSecond: 8.5,
    driftGripRecoveryPerSecond: 2.2,
    driftEntryMinSpeedMps: 9,
    driftExitMinSpeedMps: 5.5,
    driftSlipBuildMps2: 13,
    driftExitStabilizationPerSecond: 12,
    maxLateralSlipMps: 15,
    offTrackDecelerationMps2: 11,
    collisionSpeedRetention: 0.58,
  );
}
