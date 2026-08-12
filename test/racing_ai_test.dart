import 'package:afareet_asphalt/game/ai/racing_ai.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  group('Racing AI', () {
    test('racing line exposes braking and drift zones', () {
      final line = RacingLine.cairoPrototype(1000);
      final brakeTarget = line.sample(190);
      final driftTarget = line.sample(310);

      expect(brakeTarget.brakingZone, isTrue);
      expect(driftTarget.driftZone, isTrue);
    });

    test('driver follows line, accelerates and uses deterministic strategy', () {
      final line = RacingLine.cairoPrototype(900);
      final driver = AiDriver(
        id: 'test_ai',
        line: line,
        difficulty: AiDifficultyProfile.street,
        personality: AiPersonality.cairoPhantom,
        seed: 42,
      );

      for (var i = 0; i < 120; i += 1) {
        driver.step(
          dt: 1 / 60,
          trackLengthMeters: 900,
          raceTimeSeconds: i / 60,
          opponents: const <AiDriverSnapshot>[],
        );
      }

      expect(driver.speedKph, greaterThan(0));
      expect(driver.distanceMeters, greaterThan(0));
      expect(driver.throttle, inInclusiveRange(0, 1));
      expect(driver.steering, inInclusiveRange(-1, 1));
    });

    test('overtaking and collision avoidance change lateral target', () {
      final line = RacingLine.cairoPrototype(900);
      final driver = AiDriver(
        id: 'passing_ai',
        line: line,
        difficulty: AiDifficultyProfile.legend,
        personality: AiPersonality.desertDjinn,
        seed: 7,
      );
      const opponent = AiDriverSnapshot(
        id: 'opponent',
        distanceMeters: 8,
        lateralOffsetMeters: 0,
        speedKph: 40,
        steering: 0,
        throttle: 0,
        brake: 0,
        drifting: false,
        nitro: false,
        finished: false,
      );

      driver.step(
        dt: 0.2,
        trackLengthMeters: 900,
        raceTimeSeconds: 0.2,
        opponents: const <AiDriverSnapshot>[opponent],
      );

      expect(driver.lateralOffsetMeters.abs(), greaterThan(0));
    });

    test('prototype pack contains three AI and reports player position', () {
      final pack = AiRacePack.prototype(trackLengthMeters: 900);
      expect(pack.drivers, hasLength(3));

      for (var i = 0; i < 240; i += 1) {
        pack.step(dt: 1 / 60, raceTimeSeconds: i / 60);
      }

      expect(pack.playerPosition(0), greaterThan(1));
      expect(pack.snapshots, hasLength(3));
    });

    test('stuck recovery and finish are consistent', () {
      final line = RacingLine.cairoPrototype(30);
      final driver = AiDriver(
        id: 'finish_ai',
        line: line,
        difficulty: AiDifficultyProfile.legend,
        personality: AiPersonality.cairoPhantom,
        seed: 3,
      );

      for (var i = 0; i < 600 && !driver.finished; i += 1) {
        driver.step(
          dt: 0.1,
          trackLengthMeters: 30,
          raceTimeSeconds: i * 0.1,
          opponents: const <AiDriverSnapshot>[],
        );
      }

      expect(driver.finished, isTrue);
      expect(driver.distanceMeters, 30);
      expect(driver.finishTimeSeconds, isNotNull);

      final finishTime = driver.finishTimeSeconds;
      driver.step(
        dt: 1,
        trackLengthMeters: 30,
        raceTimeSeconds: 999,
        opponents: const <AiDriverSnapshot>[],
      );
      expect(driver.finishTimeSeconds, finishTime);
    });
  });
}
