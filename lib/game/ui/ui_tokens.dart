import 'package:flutter/material.dart';

abstract class AfareetUiTokens {
  static const Color background = Color(0xFF040915);
  static const Color surface = Color(0xE60A1730);
  static const Color surfaceSoft = Color(0xCC101E38);
  static const Color cyan = Color(0xFF00E5FF);
  static const Color gold = Color(0xFFFFC857);
  static const Color danger = Color(0xFFFF5678);
  static const Color textPrimary = Color(0xFFF7FAFF);
  static const Color textSecondary = Color(0xFFA9B9D2);

  static const double radiusLarge = 24;
  static const double radiusMedium = 16;
  static const double contentPadding = 20;

  static double clampTextScale(double scale) {
    if (!scale.isFinite) {
      return 1;
    }
    return scale.clamp(0.85, 1.35).toDouble();
  }
}
