import 'dart:async';

import 'package:afareet_asphalt/game/afareet_game.dart';
import 'package:afareet_asphalt/game/core/game_telemetry.dart';
import 'package:afareet_asphalt/game/ui/debug_overlay.dart';
import 'package:afareet_asphalt/game/ui/front_end_flow.dart';
import 'package:afareet_asphalt/game/ui/prototype_controls.dart';
import 'package:afareet_asphalt/game/ui/prototype_hud.dart';
import 'package:afareet_asphalt/game/ui/ui_overlay_keys.dart';
import 'package:afareet_asphalt/game/ui/ui_tokens.dart';
import 'package:afareet_asphalt/game/ui/vehicle_tuning_panel.dart';
import 'package:flame/game.dart';
import 'package:flutter/material.dart';

class AfareetApp extends StatefulWidget {
  const AfareetApp({required this.game, super.key});

  final AfareetGame game;

  @override
  State<AfareetApp> createState() => _AfareetAppState();
}

class _AfareetAppState extends State<AfareetApp> with WidgetsBindingObserver {
  final FrontEndFlowController _flow = FrontEndFlowController();

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addObserver(this);
    widget.game.pauseSimulation(showOverlay: false);
    unawaited(
      Future<void>.delayed(const Duration(milliseconds: 2800), () {
        if (mounted) {
          _flow.showMainMenu();
        }
      }),
    );
  }

  @override
  void dispose() {
    WidgetsBinding.instance.removeObserver(this);
    _flow.dispose();
    super.dispose();
  }

  @override
  void didChangeAppLifecycleState(AppLifecycleState state) {
    switch (state) {
      case AppLifecycleState.resumed:
        if (_flow.stage == FrontEndStage.racing) {
          widget.game.resumeSimulation();
        }
        return;
      case AppLifecycleState.inactive:
      case AppLifecycleState.hidden:
      case AppLifecycleState.paused:
      case AppLifecycleState.detached:
        widget.game.pauseSimulation(
          showOverlay: _flow.stage == FrontEndStage.racing,
        );
        return;
    }
  }

  @override
  Widget build(BuildContext context) {
    return AnimatedBuilder(
      animation: _flow,
      builder: (context, child) {
        return MaterialApp(
          debugShowCheckedModeBanner: false,
          theme: ThemeData(
            brightness: Brightness.dark,
            useMaterial3: true,
            scaffoldBackgroundColor: AfareetUiTokens.background,
            colorScheme: const ColorScheme.dark(
              primary: AfareetUiTokens.cyan,
              secondary: AfareetUiTokens.gold,
              surface: AfareetUiTokens.surfaceSoft,
            ),
          ),
          builder: (context, child) {
            final media = MediaQuery.of(context);
            final scale = AfareetUiTokens.clampTextScale(
              media.textScaler.scale(1),
            );
            return MediaQuery(
              data: media.copyWith(textScaler: TextScaler.linear(scale)),
              child: Directionality(
                textDirection: _flow.isArabic
                    ? TextDirection.rtl
                    : TextDirection.ltr,
                child: child ?? const SizedBox.shrink(),
              ),
            );
          },
          home: Scaffold(
            body: Stack(
              fit: StackFit.expand,
              children: <Widget>[
                GameWidget<AfareetGame>(
                  game: widget.game,
                  overlayBuilderMap:
                      <String, Widget Function(BuildContext, AfareetGame)>{
                        PrototypeHud.overlayKey: (context, game) =>
                            PrototypeHud(game: game),
                        GameDebugOverlay.overlayKey: (context, game) =>
                            GameDebugOverlay(game: game),
                        PrototypeControls.overlayKey: (context, game) =>
                            PrototypeControls(game: game),
                        VehicleTuningPanel.overlayKey: (context, game) =>
                            VehicleTuningPanel(game: game),
                        UiOverlayKeys.pauseMenu: (context, game) =>
                            _PrototypePauseMenu(
                              game: game,
                              onQuit: _returnToMenu,
                            ),
                        UiOverlayKeys.raceResult: (context, game) =>
                            _PrototypeResultOverlay(
                              game: game,
                              onQuit: _returnToMenu,
                            ),
                      },
                  initialActiveOverlays: const <String>[
                    PrototypeHud.overlayKey,
                    PrototypeControls.overlayKey,
                  ],
                ),
                if (_flow.stage != FrontEndStage.racing)
                  _FrontEndLayer(
                    flow: _flow,
                    onPlay: _flow.showModeSelection,
                    onBack: _flow.showMainMenu,
                    onRace: _beginPrototypeRace,
                    onRetry: _flow.retry,
                  ),
              ],
            ),
          ),
        );
      },
    );
  }

  void _beginPrototypeRace() {
    _flow.showLoading();
    unawaited(
      Future<void>.delayed(const Duration(milliseconds: 320), () {
        if (!mounted) {
          return;
        }
        widget.game.dismissRuntimeMenus();
        widget.game.restartRace();
        widget.game.resumeSimulation();
        _flow.enterRace();
      }),
    );
  }

  void _returnToMenu() {
    widget.game.dismissRuntimeMenus();
    widget.game.pauseSimulation(showOverlay: false);
    _flow.showMainMenu();
  }
}

class _FrontEndLayer extends StatelessWidget {
  const _FrontEndLayer({
    required this.flow,
    required this.onPlay,
    required this.onBack,
    required this.onRace,
    required this.onRetry,
  });

  final FrontEndFlowController flow;
  final VoidCallback onPlay;
  final VoidCallback onBack;
  final VoidCallback onRace;
  final VoidCallback onRetry;

  @override
  Widget build(BuildContext context) {
    if (flow.stage == FrontEndStage.splash) {
      return const _SplashScreen();
    }

    return ColoredBox(
      color: AfareetUiTokens.background,
      child: SafeArea(
        child: Padding(
          padding: const EdgeInsets.all(AfareetUiTokens.contentPadding),
          child: switch (flow.stage) {
            FrontEndStage.splash => const SizedBox.shrink(),
            FrontEndStage.mainMenu => _MainMenu(
              isArabic: flow.isArabic,
              onPlay: onPlay,
              onLanguage: flow.toggleLanguage,
            ),
            FrontEndStage.modeSelection => _ModeSelection(
              isArabic: flow.isArabic,
              onBack: onBack,
              onRace: onRace,
            ),
            FrontEndStage.loading => const _LoadingScreen(),
            FrontEndStage.error => _ErrorScreen(
              message: flow.errorMessage ?? 'Unknown error',
              onRetry: onRetry,
            ),
            FrontEndStage.racing => const SizedBox.shrink(),
          },
        ),
      ),
    );
  }
}

class _SplashScreen extends StatefulWidget {
  const _SplashScreen();

  @override
  State<_SplashScreen> createState() => _SplashScreenState();
}

class _SplashScreenState extends State<_SplashScreen> {
  @override
  void didChangeDependencies() {
    precacheImage(
      const AssetImage('assets/images/afareet_splash.jpg'),
      context,
    );
    super.didChangeDependencies();
  }

  @override
  Widget build(BuildContext context) {
    return ColoredBox(
      color: const Color(0xFF160B25),
      child: Stack(
        fit: StackFit.expand,
        children: <Widget>[
          Image.asset(
            'assets/images/afareet_splash.jpg',
            fit: BoxFit.cover,
            filterQuality: FilterQuality.high,
            errorBuilder: (context, error, stackTrace) => const ColoredBox(
              color: AfareetUiTokens.background,
              child: Center(
                child: Icon(
                  Icons.auto_awesome,
                  color: AfareetUiTokens.gold,
                  size: 64,
                ),
              ),
            ),
          ),
          const DecoratedBox(
            decoration: BoxDecoration(
              gradient: LinearGradient(
                begin: Alignment.topCenter,
                end: Alignment.bottomCenter,
                colors: <Color>[
                  Colors.transparent,
                  Colors.transparent,
                  Color(0x330D0618),
                  Color(0xF20D0618),
                ],
                stops: <double>[0, .7, .82, 1],
              ),
            ),
          ),
          SafeArea(
            child: Align(
              alignment: Alignment.bottomCenter,
              child: Padding(
                padding: const EdgeInsets.fromLTRB(28, 0, 28, 22),
                child: ConstrainedBox(
                  constraints: const BoxConstraints(maxWidth: 390),
                  child: TweenAnimationBuilder<double>(
                    tween: Tween<double>(begin: 0, end: 1),
                    duration: const Duration(milliseconds: 2550),
                    curve: Curves.easeInOutCubic,
                    builder: (context, progress, child) {
                      return Column(
                        mainAxisSize: MainAxisSize.min,
                        children: <Widget>[
                          Container(
                            height: 18,
                            padding: const EdgeInsets.all(3),
                            decoration: BoxDecoration(
                              color: const Color(0xCC1A0C2A),
                              borderRadius: BorderRadius.circular(99),
                              border: Border.all(
                                color: const Color(0xFFFFC857),
                                width: 1.4,
                              ),
                              boxShadow: const <BoxShadow>[
                                BoxShadow(
                                  color: Color(0xAA9B35FF),
                                  blurRadius: 18,
                                  spreadRadius: 1,
                                ),
                              ],
                            ),
                            child: ClipRRect(
                              borderRadius: BorderRadius.circular(99),
                              child: LinearProgressIndicator(
                                value: progress,
                                backgroundColor: const Color(0xFF28143B),
                                valueColor: const AlwaysStoppedAnimation<Color>(
                                  Color(0xFFB747FF),
                                ),
                              ),
                            ),
                          ),
                          const SizedBox(height: 10),
                          Text(
                            progress < .9
                                ? 'جاري تحضير العفاريت...'
                                : 'استعد للانطلاق!',
                            textDirection: TextDirection.rtl,
                            style: const TextStyle(
                              color: Colors.white,
                              fontSize: 15,
                              fontWeight: FontWeight.w800,
                              shadows: <Shadow>[
                                Shadow(color: Colors.black, blurRadius: 8),
                              ],
                            ),
                          ),
                        ],
                      );
                    },
                  ),
                ),
              ),
            ),
          ),
        ],
      ),
    );
  }
}

class _MainMenu extends StatelessWidget {
  const _MainMenu({
    required this.isArabic,
    required this.onPlay,
    required this.onLanguage,
  });

  final bool isArabic;
  final VoidCallback onPlay;
  final VoidCallback onLanguage;

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: <Widget>[
        Row(
          mainAxisAlignment: MainAxisAlignment.spaceBetween,
          children: <Widget>[
            const _BrandLockup(),
            TextButton.icon(
              onPressed: onLanguage,
              icon: const Icon(Icons.language),
              label: Text(isArabic ? 'EN' : 'عربي'),
            ),
          ],
        ),
        const Spacer(),
        Text(
          isArabic ? 'الشارع ليك.' : 'OWN THE NIGHT.',
          style: const TextStyle(
            fontSize: 42,
            fontWeight: FontWeight.w900,
            height: 0.95,
          ),
        ),
        const SizedBox(height: 14),
        Text(
          isArabic
              ? 'سباق أركيد مصري بنبض النيون والدريفت السحري.'
              : 'Egyptian arcade racing with neon speed and supernatural drift.',
          style: const TextStyle(
            color: AfareetUiTokens.textSecondary,
            fontSize: 16,
          ),
        ),
        const SizedBox(height: 28),
        _PrimaryButton(
          label: isArabic ? 'ابدأ السباق' : 'PLAY',
          icon: Icons.play_arrow_rounded,
          onPressed: onPlay,
        ),
        const SizedBox(height: 12),
        const _StatusStrip(),
      ],
    );
  }
}

class _ModeSelection extends StatelessWidget {
  const _ModeSelection({
    required this.isArabic,
    required this.onBack,
    required this.onRace,
  });

  final bool isArabic;
  final VoidCallback onBack;
  final VoidCallback onRace;

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: <Widget>[
        Row(
          children: <Widget>[
            IconButton(onPressed: onBack, icon: const Icon(Icons.arrow_back)),
            const SizedBox(width: 8),
            Text(
              isArabic ? 'اختر نمط اللعب' : 'SELECT MODE',
              style: const TextStyle(fontSize: 24, fontWeight: FontWeight.w900),
            ),
          ],
        ),
        const Spacer(),
        _ModeCard(
          title: isArabic ? 'سباق القاهرة التجريبي' : 'CAIRO PROTOTYPE RACE',
          subtitle: isArabic
              ? 'لفة واحدة • 3 منافسين • دريفت + نيترو'
              : '1 lap • 3 rivals • Magic Drift + Nitro Spirit',
          onPressed: onRace,
        ),
        const Spacer(),
      ],
    );
  }
}

class _LoadingScreen extends StatelessWidget {
  const _LoadingScreen();

  @override
  Widget build(BuildContext context) {
    return const Center(
      child: SizedBox(
        width: 280,
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: <Widget>[
            LinearProgressIndicator(minHeight: 7),
            SizedBox(height: 18),
            Text(
              'IGNITING CAIRO...',
              style: TextStyle(fontWeight: FontWeight.w800, letterSpacing: 2),
            ),
          ],
        ),
      ),
    );
  }
}

class _ErrorScreen extends StatelessWidget {
  const _ErrorScreen({required this.message, required this.onRetry});

  final String message;
  final VoidCallback onRetry;

  @override
  Widget build(BuildContext context) {
    return Center(
      child: Container(
        constraints: const BoxConstraints(maxWidth: 460),
        padding: const EdgeInsets.all(24),
        decoration: BoxDecoration(
          color: AfareetUiTokens.surface,
          borderRadius: BorderRadius.circular(AfareetUiTokens.radiusLarge),
          border: Border.all(color: AfareetUiTokens.danger),
        ),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: <Widget>[
            const Icon(
              Icons.error_outline,
              color: AfareetUiTokens.danger,
              size: 42,
            ),
            const SizedBox(height: 14),
            const Text(
              'RACE SYSTEM ERROR',
              style: TextStyle(fontWeight: FontWeight.w900),
            ),
            const SizedBox(height: 8),
            Text(message, textAlign: TextAlign.center),
            const SizedBox(height: 18),
            _PrimaryButton(
              label: 'TRY AGAIN',
              icon: Icons.refresh_rounded,
              onPressed: onRetry,
            ),
          ],
        ),
      ),
    );
  }
}

class _PrototypePauseMenu extends StatefulWidget {
  const _PrototypePauseMenu({required this.game, required this.onQuit});

  final AfareetGame game;
  final VoidCallback onQuit;

  @override
  State<_PrototypePauseMenu> createState() => _PrototypePauseMenuState();
}

class _PrototypePauseMenuState extends State<_PrototypePauseMenu> {
  @override
  Widget build(BuildContext context) {
    return ColoredBox(
      color: const Color(0xB8040915),
      child: SafeArea(
        child: Center(
          child: Container(
            width: 360,
            padding: const EdgeInsets.all(24),
            decoration: BoxDecoration(
              color: AfareetUiTokens.surface,
              borderRadius: BorderRadius.circular(AfareetUiTokens.radiusLarge),
              border: Border.all(color: AfareetUiTokens.cyan),
            ),
            child: Column(
              mainAxisSize: MainAxisSize.min,
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: <Widget>[
                const Text(
                  'PAUSED',
                  textAlign: TextAlign.center,
                  style: TextStyle(fontSize: 30, fontWeight: FontWeight.w900),
                ),
                const SizedBox(height: 18),
                SwitchListTile(
                  value: widget.game.cameraShakeEnabled,
                  title: const Text('Camera shake'),
                  subtitle: const Text('Accessibility toggle'),
                  onChanged: (value) {
                    setState(() {
                      widget.game.setCameraShakeEnabled(value);
                    });
                  },
                ),
                const SizedBox(height: 10),
                _PrimaryButton(
                  label: 'RESUME',
                  icon: Icons.play_arrow_rounded,
                  onPressed: widget.game.resumeSimulation,
                ),
                const SizedBox(height: 10),
                OutlinedButton(
                  onPressed: () {
                    widget.game.restartRace();
                    widget.game.resumeSimulation();
                  },
                  child: const Text('RESTART RACE'),
                ),
                TextButton(
                  onPressed: widget.onQuit,
                  child: const Text('MAIN MENU'),
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}

class _PrototypeResultOverlay extends StatelessWidget {
  const _PrototypeResultOverlay({required this.game, required this.onQuit});

  final AfareetGame game;
  final VoidCallback onQuit;

  @override
  Widget build(BuildContext context) {
    return ColoredBox(
      color: const Color(0xD9040915),
      child: SafeArea(
        child: Center(
          child: ValueListenableBuilder<GameTelemetry>(
            valueListenable: game.telemetry,
            builder: (context, telemetry, child) {
              return Container(
                width: 380,
                padding: const EdgeInsets.all(26),
                decoration: BoxDecoration(
                  color: AfareetUiTokens.surface,
                  borderRadius: BorderRadius.circular(
                    AfareetUiTokens.radiusLarge,
                  ),
                  border: Border.all(color: AfareetUiTokens.gold),
                ),
                child: Column(
                  mainAxisSize: MainAxisSize.min,
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: <Widget>[
                    const Icon(
                      Icons.emoji_events,
                      color: AfareetUiTokens.gold,
                      size: 48,
                    ),
                    const SizedBox(height: 12),
                    Text(
                      'P${telemetry.position} FINISH',
                      textAlign: TextAlign.center,
                      style: const TextStyle(
                        fontSize: 30,
                        fontWeight: FontWeight.w900,
                      ),
                    ),
                    const SizedBox(height: 8),
                    Text(
                      '${telemetry.raceTimeSeconds.toStringAsFixed(2)} s',
                      textAlign: TextAlign.center,
                      style: const TextStyle(
                        color: AfareetUiTokens.cyan,
                        fontSize: 22,
                      ),
                    ),
                    const SizedBox(height: 20),
                    _PrimaryButton(
                      label: 'RACE AGAIN',
                      icon: Icons.replay_rounded,
                      onPressed: () {
                        game.restartRace();
                        game.resumeSimulation();
                      },
                    ),
                    TextButton(
                      onPressed: onQuit,
                      child: const Text('MAIN MENU'),
                    ),
                  ],
                ),
              );
            },
          ),
        ),
      ),
    );
  }
}

class _BrandLockup extends StatelessWidget {
  const _BrandLockup();

  @override
  Widget build(BuildContext context) {
    return const Row(
      mainAxisSize: MainAxisSize.min,
      children: <Widget>[
        Icon(Icons.bolt_rounded, color: AfareetUiTokens.gold),
        SizedBox(width: 8),
        Text(
          '3FAREET',
          style: TextStyle(fontWeight: FontWeight.w900, letterSpacing: 2),
        ),
      ],
    );
  }
}

class _PrimaryButton extends StatelessWidget {
  const _PrimaryButton({
    required this.label,
    required this.icon,
    required this.onPressed,
  });

  final String label;
  final IconData icon;
  final VoidCallback onPressed;

  @override
  Widget build(BuildContext context) {
    return FilledButton.icon(
      onPressed: onPressed,
      icon: Icon(icon),
      label: Padding(
        padding: const EdgeInsets.symmetric(vertical: 16),
        child: Text(
          label,
          style: const TextStyle(
            fontWeight: FontWeight.w900,
            letterSpacing: 1.5,
          ),
        ),
      ),
    );
  }
}

class _ModeCard extends StatelessWidget {
  const _ModeCard({
    required this.title,
    required this.subtitle,
    required this.onPressed,
  });

  final String title;
  final String subtitle;
  final VoidCallback onPressed;

  @override
  Widget build(BuildContext context) {
    return InkWell(
      onTap: onPressed,
      borderRadius: BorderRadius.circular(AfareetUiTokens.radiusLarge),
      child: Ink(
        padding: const EdgeInsets.all(26),
        decoration: BoxDecoration(
          color: AfareetUiTokens.surface,
          borderRadius: BorderRadius.circular(AfareetUiTokens.radiusLarge),
          border: Border.all(color: AfareetUiTokens.cyan),
        ),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: <Widget>[
            const Icon(
              Icons.sports_motorsports,
              color: AfareetUiTokens.gold,
              size: 40,
            ),
            const SizedBox(height: 18),
            Text(
              title,
              style: const TextStyle(fontSize: 24, fontWeight: FontWeight.w900),
            ),
            const SizedBox(height: 8),
            Text(
              subtitle,
              style: const TextStyle(color: AfareetUiTokens.textSecondary),
            ),
            const SizedBox(height: 20),
            const Row(
              children: <Widget>[
                Text(
                  'ENTER RACE',
                  style: TextStyle(
                    color: AfareetUiTokens.cyan,
                    fontWeight: FontWeight.w800,
                  ),
                ),
                SizedBox(width: 8),
                Icon(Icons.arrow_forward, color: AfareetUiTokens.cyan),
              ],
            ),
          ],
        ),
      ),
    );
  }
}

class _StatusStrip extends StatelessWidget {
  const _StatusStrip();

  @override
  Widget build(BuildContext context) {
    return const DecoratedBox(
      decoration: BoxDecoration(
        color: AfareetUiTokens.surfaceSoft,
        borderRadius: BorderRadius.all(
          Radius.circular(AfareetUiTokens.radiusMedium),
        ),
      ),
      child: Padding(
        padding: EdgeInsets.symmetric(horizontal: 16, vertical: 12),
        child: Row(
          children: <Widget>[
            Icon(Icons.circle, size: 9, color: AfareetUiTokens.cyan),
            SizedBox(width: 9),
            Expanded(
              child: Text(
                'PROTOTYPE • OFFLINE RACE • 3 AI RIVALS',
                style: TextStyle(
                  color: AfareetUiTokens.textSecondary,
                  fontWeight: FontWeight.w700,
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }
}
