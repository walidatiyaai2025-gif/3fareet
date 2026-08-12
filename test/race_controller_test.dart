import 'package:afareet_asphalt/game/race/race_controller.dart';
import 'package:afareet_asphalt/game/race/race_result.dart';
import 'package:afareet_asphalt/game/race/track_definition.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  test('countdown starts race and checkpoints are ordered', () {
    final race = RaceController(track: TrackDefinition.cairoPrototype)
      ..startCountdown();

    race.step(3.1);
    expect(race.phase, RacePhase.racing);
    expect(race.registerCheckpoint(1), isFalse);
    expect(race.registerCheckpoint(0), isTrue);
    expect(race.nextCheckpointIndex, 1);
  });

  test('finish is gated by checkpoints and creates result', () {
    final race = RaceController(track: TrackDefinition.cairoPrototype)
      ..startCountdown();
    race.step(3.1);

    expect(race.crossFinish(), isFalse);
    for (var i = 0; i < TrackDefinition.cairoPrototype.checkpoints.length; i += 1) {
      expect(race.registerCheckpoint(i), isTrue);
    }
    expect(race.crossFinish(position: 1), isTrue);
    expect(race.phase, RacePhase.finished);
    expect(race.result?.reason, RaceExitReason.finished);
    expect(race.lapsCompleted, 1);
  });

  test('wrong-way out-of-bounds respawn restart and quit rules work', () {
    final race = RaceController(track: TrackDefinition.cairoPrototype)
      ..startCountdown();
    race.step(3.1);
    race.setWrongWayFromVectors(
      forwardX: -1,
      forwardY: 0,
      tangentX: 1,
      tangentY: 0,
    );
    expect(race.wrongWay, isTrue);
    expect(race.isOutOfBounds(100), isTrue);
    expect(race.safeRespawnDistance(), 0);

    expect(race.registerCheckpoint(0), isTrue);
    expect(race.safeRespawnDistance(), 225);

    race.restart();
    expect(race.phase, RacePhase.countdown);
    expect(race.nextCheckpointIndex, 0);
    expect(race.raceTimeSeconds, 0);

    race.step(3.1);
    final result = race.quit(position: 3);
    expect(result.reason, RaceExitReason.quit);
    expect(result.position, 3);
  });

  test('ranking sorts lap before distance', () {
    final ranked = RaceController.rank(const <RacerProgress>[
      RacerProgress(racerId: 'a', lapsCompleted: 0, distanceAlongLap: 500),
      RacerProgress(racerId: 'b', lapsCompleted: 1, distanceAlongLap: 10),
      RacerProgress(racerId: 'c', lapsCompleted: 0, distanceAlongLap: 700),
    ]);

    expect(ranked.map((racer) => racer.racerId), <String>['b', 'c', 'a']);
  });
}
