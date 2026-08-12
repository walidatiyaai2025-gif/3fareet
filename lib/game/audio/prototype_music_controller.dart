import 'dart:convert';

import 'package:flame_audio/flame_audio.dart';
import 'package:flutter/services.dart';

class PrototypeMusicController {
  PrototypeMusicController({required this.bundle});

  static const String embeddedLoopAsset =
      'assets/audio/embedded/cairo_rap_shaabi_loop_4s.b64';

  final AssetBundle bundle;
  final AudioPlayer _player = AudioPlayer(playerId: 'prototype-bgm');

  bool _started = false;

  Future<void> start() async {
    if (_started) {
      return;
    }

    final encoded = await bundle.loadString(embeddedLoopAsset);
    final bytes = base64Decode(encoded.trim());

    await _player.setReleaseMode(ReleaseMode.loop);
    await _player.play(
      BytesSource(bytes, mimeType: 'audio/mpeg'),
      volume: 0.52,
    );

    _started = true;
  }

  Future<void> stop() async {
    if (!_started) {
      return;
    }

    await _player.stop();
    _started = false;
  }

  Future<void> dispose() async {
    await _player.dispose();
  }
}
