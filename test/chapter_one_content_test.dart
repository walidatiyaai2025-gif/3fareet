import 'package:afareet_asphalt/game/career/chapter_one_content.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  test('chapter one content has objectives and rewards for every node', () {
    final content = buildChapterOneContent();
    expect(content, isNotEmpty);
    expect(content.every((entry) => entry.objectives.isNotEmpty), isTrue);
    expect(content.every((entry) => entry.rewards.isNotEmpty), isTrue);
    expect(content.last.rewards.any((reward) => reward.unlockVehicleId == 'djinn_spirit'), isTrue);
  });
}
