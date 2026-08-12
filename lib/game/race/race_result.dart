enum RaceExitReason { finished, quit }

class RaceResult {
  const RaceResult({
    required this.reason,
    required this.totalTimeSeconds,
    required this.lapsCompleted,
    required this.position,
  });

  final RaceExitReason reason;
  final double totalTimeSeconds;
  final int lapsCompleted;
  final int position;
}
