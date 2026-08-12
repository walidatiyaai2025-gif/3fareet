import 'package:afareet_asphalt/game/input/game_input.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  test('input values are clamped and pause is edge-triggered', () {
    final input = GameInputState()
      ..setSteering(2)
      ..setThrottle(4)
      ..setBrake(-1)
      ..setAction(GameAction.pause, true);

    final first = input.consumeSnapshot();
    final second = input.consumeSnapshot();

    expect(first.steering, 1);
    expect(first.throttle, 1);
    expect(first.brake, 0);
    expect(first.pausePressed, isTrue);
    expect(second.pausePressed, isFalse);
  });
}
