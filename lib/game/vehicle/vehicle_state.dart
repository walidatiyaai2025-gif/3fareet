class VehicleSafePoint {
  const VehicleSafePoint({
    required this.x,
    required this.y,
    required this.headingRadians,
  });

  final double x;
  final double y;
  final double headingRadians;
}

class VehicleState {
  VehicleState({
    this.x = 0,
    this.y = 0,
    this.headingRadians = 0,
    this.speedMps = 0,
    this.lateralSlipMps = 0,
    this.isDrifting = false,
    this.isOffTrack = false,
  });

  double x;
  double y;
  double headingRadians;
  double speedMps;
  double lateralSlipMps;
  bool isDrifting;
  bool isOffTrack;

  double get speedKph => speedMps.abs() * 3.6;

  void resetTo(VehicleSafePoint safePoint) {
    x = safePoint.x;
    y = safePoint.y;
    headingRadians = safePoint.headingRadians;
    speedMps = 0;
    lateralSlipMps = 0;
    isDrifting = false;
    isOffTrack = false;
  }
}
