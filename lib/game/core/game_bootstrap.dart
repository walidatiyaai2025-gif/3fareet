import 'package:afareet_asphalt/game/assets/game_asset_loader.dart';
import 'package:afareet_asphalt/game/config/game_config.dart';

enum BootstrapState { idle, initializing, ready, failed, disposed }

class GameBootstrap {
  GameBootstrap({
    required this.assetLoader,
    required this.configLoader,
  });

  final GameAssetLoader assetLoader;
  final GameConfigLoader configLoader;

  BootstrapState state = BootstrapState.idle;
  GameConfig? _config;

  GameConfig get config {
    final value = _config;
    if (value == null) {
      throw StateError('GameBootstrap is not ready. Call initialize() first.');
    }
    return value;
  }

  Future<void> initialize() async {
    if (state == BootstrapState.ready) {
      return;
    }
    if (state == BootstrapState.disposed) {
      throw StateError('GameBootstrap has been disposed.');
    }

    state = BootstrapState.initializing;
    try {
      await assetLoader.load();
      _config = await configLoader.load();
      state = BootstrapState.ready;
    } on Object {
      state = BootstrapState.failed;
      rethrow;
    }
  }

  void dispose() {
    assetLoader.dispose();
    state = BootstrapState.disposed;
  }
}
