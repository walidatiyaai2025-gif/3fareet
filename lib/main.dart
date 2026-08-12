import 'package:afareet_asphalt/game/afareet_game.dart';
import 'package:afareet_asphalt/game/assets/game_asset_loader.dart';
import 'package:afareet_asphalt/game/config/game_config.dart';
import 'package:afareet_asphalt/game/core/game_bootstrap.dart';
import 'package:afareet_asphalt/game/ui/debug_overlay.dart';
import 'package:afareet_asphalt/game/ui/prototype_controls.dart';
import 'package:afareet_asphalt/game/ui/prototype_hud.dart';
import 'package:afareet_asphalt/game/ui/vehicle_tuning_panel.dart';
import 'package:flame/game.dart';
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

class AfareetApp extends StatefulWidget {
  const AfareetApp({required this.game, super.key});

  final AfareetGame game;

  @override
  State<AfareetApp> createState() => _AfareetAppState();
}

class _AfareetAppState extends State<AfareetApp> with WidgetsBindingObserver {
  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addObserver(this);
  }

  @override
  void dispose() {
    WidgetsBinding.instance.removeObserver(this);
    super.dispose();
  }

  @override
  void didChangeAppLifecycleState(AppLifecycleState state) {
    switch (state) {
      case AppLifecycleState.resumed:
        widget.game.resumeSimulation();
      case AppLifecycleState.inactive:
      case AppLifecycleState.hidden:
      case AppLifecycleState.paused:
      case AppLifecycleState.detached:
        widget.game.pauseSimulation();
    }
  }

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      debugShowCheckedModeBanner: false,
      theme: ThemeData.dark(useMaterial3: true),
      home: Scaffold(
        body: GameWidget<AfareetGame>(
          game: widget.game,
          overlayBuilderMap: <String, Widget Function(BuildContext, AfareetGame)>{
            PrototypeHud.overlayKey: (context, game) => PrototypeHud(game: game),
            GameDebugOverlay.overlayKey: (context, game) => GameDebugOverlay(game: game),
            PrototypeControls.overlayKey: (context, game) =>
                PrototypeControls(game: game),
            VehicleTuningPanel.overlayKey: (context, game) =>
                VehicleTuningPanel(game: game),
          },
          initialActiveOverlays: const <String>[
            PrototypeHud.overlayKey,
            GameDebugOverlay.overlayKey,
            PrototypeControls.overlayKey,
          ],
        ),
      ),
    );
  }
}
