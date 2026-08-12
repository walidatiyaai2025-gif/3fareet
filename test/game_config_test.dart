import 'package:afareet_asphalt/game/config/game_config.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  test('game config parses prototype simulation values', () {
    final config = GameConfig.fromJson(<String, Object?>{
      'targetFps': 60,
      'fixedStepSeconds': 1 / 60,
      'maxCatchUpSteps': 5,
      'prototypeTrackId': 'cairo_test',
      'initialSpirit': 0.25,
      'debugOverlayEnabled': true,
    });

    expect(config.targetFps, 60);
    expect(config.maxCatchUpSteps, 5);
    expect(config.prototypeTrackId, 'cairo_test');
    expect(config.initialSpirit, 0.25);
    expect(config.debugOverlayEnabled, isTrue);
  });
}
