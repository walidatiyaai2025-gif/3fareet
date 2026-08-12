import 'package:afareet_asphalt/game/race/race_result.dart';
import 'package:afareet_asphalt/game/race/track_definition.dart';
import 'package:afareet_asphalt/game/vehicle/vehicle_state.dart';

enum RacePhase { waiting, countdown, racing, finished, quit }

class RacerProgress {
  const RacerProgress({
    required this.racerId,
    required this.lapsCompleted,
    required this.distanceAlongLap,
  });

  final String racerId;
  final int lapsCompleted;
  final double distanceAlongLap;
}

class RaceController {
  RaceController({
    required this.track,
    this.countdownDurationSeconds = 3,
  });

  final TrackDefinition track;
  final double countdownDurationSeconds;

  RacePhase phase = RacePhase.waiting;
  double countdownRemaining = 0;
  double raceTimeSeconds = 0;
  int lapsCompleted = 0;
  int nextCheckpointIndex = 0;
  int lastCheckpointIndex = -1;
  bool wrongWay = false;
  RaceResult? result;

  int get currentLap =>
      (lapsCompleted + 1).clamp(1, track.totalLaps).toInt();

  bool get allCheckpointsPassed => nextCheckpointIndex >= track.checkpoints.length;

  void startCountdown() {
    if (phase == RacePhase.finished || phase == RacePhase.quit) {
      return;
    }
    phase = RacePhase.countdown;
    countdownRemaining = countdownDurationSeconds;
  }

  void step(double dt) {
    if (dt <= 0) {
      return;
    }
    if (phase == RacePhase.countdown) {
      countdownRemaining -= dt;
      if (countdownRemaining <= 0) {
        countdownRemaining = 0;
        phase = RacePhase.racing;
      }
      return;
    }
    if (phase == RacePhase.racing) {
      raceTimeSeconds += dt;
    }
  }

  bool registerCheckpoint(int checkpointIndex) {
    if (phase != RacePhase.racing || checkpointIndex != nextCheckpointIndex) {
      return false;
    }
    lastCheckpointIndex = checkpointIndex;
    nextCheckpointIndex += 1;
    return true;
  }

  bool crossFinish({int position = 1}) {
    if (phase != RacePhase.racing || !allCheckpointsPassed) {
      return false;
    }

    lapsCompleted += 1;
    if (lapsCompleted >= track.totalLaps) {
      phase = RacePhase.finished;
      result = RaceResult(
        reason: RaceExitReason.finished,
        totalTimeSeconds: raceTimeSeconds,
        lapsCompleted: lapsCompleted,
        position: position,
      );
      return true;
    }

    nextCheckpointIndex = 0;
    lastCheckpointIndex = -1;
    return true;
  }

  void setWrongWayFromVectors({
    required double forwardX,
    required double forwardY,
    required double tangentX,
    required double tangentY,
  }) {
    final dot = (forwardX * tangentX) + (forwardY * tangentY);
    wrongWay = dot < -0.25;
  }

  bool isOutOfBounds(double distanceFromTrackCenterMeters) {
    return distanceFromTrackCenterMeters.abs() > track.halfWidthMeters + 2;
  }

  VehicleSafePoint safeRespawnPoint() {
    if (lastCheckpointIndex >= 0) {
      return track.checkpoints[lastCheckpointIndex].safePoint;
    }
    return track.startGrid.first.toSafePoint();
  }

  double safeRespawnDistance() {
    if (lastCheckpointIndex >= 0) {
      return track.checkpoints[lastCheckpointIndex].distanceMeters;
    }
    return 0;
  }

  void restart() {
    phase = RacePhase.waiting;
    countdownRemaining = 0;
    raceTimeSeconds = 0;
    lapsCompleted = 0;
    nextCheckpointIndex = 0;
    lastCheckpointIndex = -1;
    wrongWay = false;
    result = null;
    startCountdown();
  }

  RaceResult quit({int position = 1}) {
    phase = RacePhase.quit;
    result = RaceResult(
      reason: RaceExitReason.quit,
      totalTimeSeconds: raceTimeSeconds,
      lapsCompleted: lapsCompleted,
      position: position,
    );
    return result!;
  }

  static List<RacerProgress> rank(List<RacerProgress> racers) {
    final ranked = List<RacerProgress>.from(racers);
    ranked.sort((a, b) {
      final lapCompare = b.lapsCompleted.compareTo(a.lapsCompleted);
      if (lapCompare != 0) {
        return lapCompare;
      }
      return b.distanceAlongLap.compareTo(a.distanceAlongLap);
    });
    return ranked;
  }
}
