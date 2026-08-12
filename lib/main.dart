import 'package:afareet_asphalt/game/afareet_game.dart';
import 'package:afareet_asphalt/game/assets/game_asset_loader.dart';
import 'package:afareet_asphalt/game/config/game_config.dart';
import 'package:afareet_asphalt/game/core/game_bootstrap.dart';
import 'package:afareet_asphalt/game/ui/front_end_shell.dart';
import 'package:flutter/material.dart';
import 'package:flutter/services.dart';

void main() {
  WidgetsFlutterBinding.ensureInitialized();

  final bootstrap = GameBootstrap(
    assetLoader: GameAssetLoader(
      bundle: rootBundle,
      manifest: const <String>['assets/config/game_config.json'],
    ),
    configLoader: GameConfigLoader(bundle: rootBundle),
  );

  runApp(AfareetApp(game: AfareetGame(bootstrap: bootstrap)));
}
