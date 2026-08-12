import 'package:flutter_test/flutter_test.dart';

void main() {
  final valid = RegExp(r'^[a-z0-9_./-]+$');

  test('runtime asset names use lowercase snake case', () {
    expect(valid.hasMatch('car_street_runner_body_lod0.glb'), isTrue);
    expect(valid.hasMatch('vfx_nitro_spirit_trail_01.webp'), isTrue);
    expect(valid.hasMatch('Car Final.PNG'), isFalse);
  });

  test('placeholder naming stays explicit', () {
    expect('car_placeholder_body_01.glb'.contains('_placeholder_'), isTrue);
    expect('car_temp_body_01.glb'.contains('_placeholder_'), isFalse);
  });
}
