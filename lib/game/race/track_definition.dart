import 'package:afareet_asphalt/game/vehicle/vehicle_state.dart';

class StartGridSlot {
  const StartGridSlot({
    required this.x,
    required this.y,
    required this.headingRadians,
  });

  final double x;
  final double y;
  final double headingRadians;

  VehicleSafePoint toSafePoint() => VehicleSafePoint(
        x: x,
        y: y,
        headingRadians: headingRadians,
      );
}

class TrackCheckpoint {
  const TrackCheckpoint({
    required this.id,
    required this.distanceMeters,
    required this.safePoint,
  });

  final String id;
  final double distanceMeters;
  final VehicleSafePoint safePoint;
}

class TrackDefinition {
  TrackDefinition({
    required this.id,
    required this.totalLengthMeters,
    required this.halfWidthMeters,
    required this.totalLaps,
    required this.startGrid,
    required this.checkpoints,
  }) {
    if (totalLengthMeters <= 0 || totalLaps <= 0 || startGrid.isEmpty) {
      throw ArgumentError('Track length, laps and start grid must be valid.');
    }
    var previous = 0.0;
    for (final checkpoint in checkpoints) {
      if (checkpoint.distanceMeters <= previous ||
          checkpoint.distanceMeters >= totalLengthMeters) {
        throw ArgumentError('Checkpoints must be ordered within track length.');
      }
      previous = checkpoint.distanceMeters;
    }
  }

  final String id;
  final double totalLengthMeters;
  final double halfWidthMeters;
  final int totalLaps;
  final List<StartGridSlot> startGrid;
  final List<TrackCheckpoint> checkpoints;

  static final TrackDefinition cairoPrototype = TrackDefinition(
    id: 'cairo_neon_test_track',
    totalLengthMeters: 900,
    halfWidthMeters: 8.5,
    totalLaps: 1,
    startGrid: const <StartGridSlot>[
      StartGridSlot(x: 0, y: 0, headingRadians: 0),
      StartGridSlot(x: -4, y: -2.2, headingRadians: 0),
      StartGridSlot(x: -8, y: 2.2, headingRadians: 0),
      StartGridSlot(x: -12, y: -2.2, headingRadians: 0),
    ],
    checkpoints: const <TrackCheckpoint>[
      TrackCheckpoint(
        id: 'cp_01',
        distanceMeters: 225,
        safePoint: VehicleSafePoint(x: 225, y: 0, headingRadians: 0),
      ),
      TrackCheckpoint(
        id: 'cp_02',
        distanceMeters: 450,
        safePoint: VehicleSafePoint(x: 450, y: 0, headingRadians: 0),
      ),
      TrackCheckpoint(
        id: 'cp_03',
        distanceMeters: 675,
        safePoint: VehicleSafePoint(x: 675, y: 0, headingRadians: 0),
      ),
    ],
  );
}
