import 'package:afareet_asphalt/game/vehicle/vehicle_definition.dart';

class VehicleTuningController {
  VehicleTuningController(this.definition);

  VehicleDefinition definition;

  void setMaxSpeedKph(double value) {
    definition = definition.copyWith(maxForwardSpeedMps: value / 3.6);
  }

  void setAcceleration(double value) {
    definition = definition.copyWith(accelerationMps2: value);
  }

  void setGripRecovery(double value) {
    definition = definition.copyWith(gripRecoveryPerSecond: value);
  }

  void setDriftSlipBuild(double value) {
    definition = definition.copyWith(driftSlipBuildMps2: value);
  }

  void resetPrototypePreset() {
    definition = PrototypeVehiclePreset.definition;
  }
}
