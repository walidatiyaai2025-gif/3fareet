import 'package:afareet_asphalt/game/career/career_models.dart';
import 'package:afareet_asphalt/game/career/career_save.dart';
import 'package:afareet_asphalt/game/career/chapter_one.dart';
import 'package:afareet_asphalt/game/career/progression_system.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  test('career node prerequisites and completion flow are deterministic', () {
    final chapter = buildChapterOneFoundation();
    const service = CareerProgressionService();
    var progress = const CareerProgress(
      version: CareerProgress.currentVersion,
      stars: 0,
      completedNodeIds: <String>{},
      claimedRewardIds: <String>{},
    );

    expect(service.canEnter(chapter.nodes.first, progress), isTrue);
    expect(service.canEnter(chapter.nodes[1], progress), isFalse);

    progress = service.completeNode(progress, chapter.nodes.first.id, starsEarned: 3);
    expect(progress.stars, 3);
    expect(service.canEnter(chapter.nodes[1], progress), isTrue);
    expect(service.chapterComplete(chapter, progress), isFalse);
  });

  test('reward claim is idempotent', () {
    const service = CareerProgressionService();
    const progress = CareerProgress(
      version: CareerProgress.currentVersion,
      stars: 5,
      completedNodeIds: <String>{'a'},
      claimedRewardIds: <String>{},
    );
    final once = service.claim('reward_a', progress);
    final twice = service.claim('reward_a', once);
    expect(once.claimedRewardIds, contains('reward_a'));
    expect(twice.claimedRewardIds.length, 1);
  });

  test('career save codec round-trips and migrates legacy save', () {
    const codec = CareerSaveCodec();
    const progress = CareerProgress(
      version: CareerProgress.currentVersion,
      stars: 7,
      completedNodeIds: <String>{'c01_r01'},
      claimedRewardIds: <String>{'reward_1'},
    );
    final decoded = codec.decode(codec.encode(progress));
    expect(decoded.stars, 7);
    expect(decoded.completedNodeIds, contains('c01_r01'));
    expect(decoded.claimedRewardIds, contains('reward_1'));

    final migrated = codec.decode('{"totalStars":4,"completed":["legacy_race"]}');
    expect(migrated.version, CareerProgress.currentVersion);
    expect(migrated.stars, 4);
    expect(migrated.completedNodeIds, contains('legacy_race'));
  });

  test('chapter one includes all required race modes', () {
    final modes = buildChapterOneFoundation().nodes.map((node) => node.mode).toSet();
    expect(modes, containsAll(<CareerRaceMode>[
      CareerRaceMode.circuit,
      CareerRaceMode.timeTrial,
      CareerRaceMode.elimination,
      CareerRaceMode.driftChallenge,
      CareerRaceMode.boss,
    ]));
  });
}
