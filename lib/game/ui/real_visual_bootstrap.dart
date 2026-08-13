import 'dart:async';

import 'package:afareet_asphalt/game/assets/real_visual_assets.dart';
import 'package:afareet_asphalt/game/ui/ui_tokens.dart';
import 'package:flutter/material.dart';

class RealVisualBootstrap extends StatefulWidget {
  const RealVisualBootstrap({required this.child, super.key});

  final Widget child;

  @override
  State<RealVisualBootstrap> createState() => _RealVisualBootstrapState();
}

class _RealVisualBootstrapState extends State<RealVisualBootstrap> {
  Timer? _timer;
  bool _showPreview = true;

  @override
  void initState() {
    super.initState();
    _timer = Timer(const Duration(milliseconds: 1500), () {
      if (mounted) {
        setState(() => _showPreview = false);
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
    return Directionality(
      textDirection: TextDirection.ltr,
      child: Stack(
        fit: StackFit.expand,
        children: <Widget>[
          widget.child,
          if (_showPreview)
            DecoratedBox(
              decoration: const BoxDecoration(
                gradient: RadialGradient(
                  center: Alignment(0, -0.2),
                  radius: 1.15,
                  colors: <Color>[
                    Color(0xFF102A3A),
                    AfareetUiTokens.background,
                  ],
                ),
              ),
              child: SafeArea(
                child: Center(
                  child: Column(
                    mainAxisSize: MainAxisSize.min,
                    children: <Widget>[
                      Container(
                        width: 176,
                        height: 176,
                        padding: const EdgeInsets.all(12),
                        decoration: BoxDecoration(
                          color: const Color(0xCC08111F),
                          borderRadius: BorderRadius.circular(34),
                          border: Border.all(
                            color: AfareetUiTokens.gold,
                            width: 2,
                          ),
                          boxShadow: const <BoxShadow>[
                            BoxShadow(
                              color: Color(0x5500E5FF),
                              blurRadius: 36,
                              spreadRadius: 2,
                            ),
                          ],
                        ),
                        child: ClipRRect(
                          borderRadius: BorderRadius.circular(24),
                          child: Image.asset(
                            RealVisualAssets.appIconPreview,
                            fit: BoxFit.cover,
                            errorBuilder: (context, error, stackTrace) {
                              return const ColoredBox(
                                color: AfareetUiTokens.surface,
                                child: Center(
                                  child: Icon(
                                    Icons.bolt_rounded,
                                    color: AfareetUiTokens.gold,
                                    size: 72,
                                  ),
                                ),
                              );
                            },
                          ),
                        ),
                      ),
                      const SizedBox(height: 24),
                      const Text(
                        '3FAREET ASPHALT',
                        style: TextStyle(
                          color: Colors.white,
                          fontSize: 28,
                          fontWeight: FontWeight.w900,
                          letterSpacing: 2.5,
                          decoration: TextDecoration.none,
                        ),
                      ),
                      const SizedBox(height: 8),
                      const Text(
                        'REAL VISUAL ASSET PIPELINE • P0',
                        style: TextStyle(
                          color: AfareetUiTokens.cyan,
                          fontSize: 12,
                          fontWeight: FontWeight.w700,
                          letterSpacing: 1.3,
                          decoration: TextDecoration.none,
                        ),
                      ),
                    ],
                  ),
                ),
              ),
            ),
        ],
      ),
    );
  }
}
