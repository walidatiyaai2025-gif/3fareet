import 'dart:async';

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

  runApp(
    ThreeFareetLaunch(
      game: AfareetGame(bootstrap: bootstrap),
    ),
  );
}

class ThreeFareetLaunch extends StatefulWidget {
  const ThreeFareetLaunch({required this.game, super.key});

  final AfareetGame game;

  @override
  State<ThreeFareetLaunch> createState() => _ThreeFareetLaunchState();
}

class _ThreeFareetLaunchState extends State<ThreeFareetLaunch> {
  Timer? _timer;
  bool _ready = false;

  @override
  void initState() {
    super.initState();
    _timer = Timer(const Duration(milliseconds: 1400), () {
      if (mounted) {
        setState(() => _ready = true);
      }
    });
  }

  @override
  void dispose() {
    _timer?.cancel();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    if (_ready) {
      return AfareetApp(game: widget.game);
    }

    return MaterialApp(
      title: '3Fareet',
      debugShowCheckedModeBanner: false,
      home: Scaffold(
        backgroundColor: const Color(0xFF060815),
        body: Stack(
          fit: StackFit.expand,
          children: <Widget>[
            Image.asset(
              'assets/branding/3fareet_splash.jpg',
              fit: BoxFit.cover,
              filterQuality: FilterQuality.high,
            ),
            const DecoratedBox(
              decoration: BoxDecoration(
                gradient: LinearGradient(
                  begin: Alignment.topCenter,
                  end: Alignment.bottomCenter,
                  colors: <Color>[
                    Color(0x05000000),
                    Color(0x10000000),
                    Color(0xD9000000),
                  ],
                  stops: <double>[0, 0.64, 1],
                ),
              ),
            ),
            const SafeArea(
              child: Align(
                alignment: Alignment.bottomCenter,
                child: Padding(
                  padding: EdgeInsets.fromLTRB(24, 24, 24, 30),
                  child: Text(
                    '3Fareet',
                    textAlign: TextAlign.center,
                    style: TextStyle(
                      color: Colors.white,
                      fontSize: 30,
                      fontWeight: FontWeight.w900,
                      letterSpacing: 1.5,
                      shadows: <Shadow>[
                        Shadow(color: Color(0xFF8D37FF), blurRadius: 18),
                      ],
                    ),
                  ),
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }
}
