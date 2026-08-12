import 'dart:typed_data';

import 'package:flutter/services.dart';

enum AssetLoaderState { idle, loading, ready, failed, disposed }

class GameAssetLoader {
  GameAssetLoader({
    required this.bundle,
    required this.manifest,
  });

  final AssetBundle bundle;
  final List<String> manifest;

  final Map<String, ByteData> _cache = <String, ByteData>{};
  AssetLoaderState state = AssetLoaderState.idle;

  bool contains(String assetPath) => _cache.containsKey(assetPath);

  ByteData require(String assetPath) {
    final value = _cache[assetPath];
    if (value == null) {
      throw StateError('Asset not loaded: $assetPath');
    }
    return value;
  }

  Future<void> load() async {
    if (state == AssetLoaderState.ready) {
      return;
    }
    if (state == AssetLoaderState.disposed) {
      throw StateError('GameAssetLoader has been disposed.');
    }

    state = AssetLoaderState.loading;
    try {
      for (final assetPath in manifest) {
        _cache[assetPath] = await bundle.load(assetPath);
      }
      state = AssetLoaderState.ready;
    } on Object {
      state = AssetLoaderState.failed;
      rethrow;
    }
  }

  void dispose() {
    _cache.clear();
    state = AssetLoaderState.disposed;
  }
}
