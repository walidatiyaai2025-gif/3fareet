import 'dart:math' as math;

import 'package:afareet_asphalt/game/afareet_game.dart';
import 'package:flame/components.dart';
import 'package:flutter/painting.dart';

/// Lightweight, asset-free visual slice for the playable prototype.
///
/// The simulation remains deterministic and independent from rendering. This
/// component only projects the current race state into a cinematic Cairo road.
class PrototypeScene extends Component with HasGameReference<AfareetGame> {
  PrototypeScene({required this.trackId}) : super(priority: -100);

  final String trackId;

  static const _cyan = Color(0xFF00E5FF);
  static const _gold = Color(0xFFFFC857);
  static const _magenta = Color(0xFFFF3CAC);

  @override
  void render(Canvas canvas) {
    super.render(canvas);
    final size = game.size;
    if (size.x <= 0 || size.y <= 0) return;

    final viewport = Size(size.x, size.y);
    _paintSky(canvas, viewport);
    _paintCairo(canvas, viewport);
    _paintRoad(canvas, viewport);

    try {
      _paintRace(canvas, viewport);
    } on StateError {
      // Game bootstrap can render one frame before RaceSession is available.
    }
  }

  void _paintSky(Canvas canvas, Size size) {
    final rect = Offset.zero & size;
    canvas.drawRect(
      rect,
      Paint()
        ..shader = const LinearGradient(
          begin: Alignment.topCenter,
          end: Alignment.bottomCenter,
          colors: <Color>[
            Color(0xFF030512),
            Color(0xFF101238),
            Color(0xFF4A1747),
            Color(0xFFCA5C42),
            Color(0xFF080D1A),
          ],
          stops: <double>[0, .28, .52, .64, .66],
        ).createShader(rect),
    );

    final moon = Offset(size.width * .78, size.height * .17);
    canvas.drawCircle(
      moon,
      size.shortestSide * .075,
      Paint()
        ..color = const Color(0x55FFC857)
        ..maskFilter = const MaskFilter.blur(BlurStyle.normal, 28),
    );
    canvas.drawCircle(moon, size.shortestSide * .034, Paint()..color = _gold);

    final starPaint = Paint()..color = const Color(0x99DDFBFF);
    for (var i = 0; i < 34; i++) {
      final x = ((i * 83) % 997) / 997 * size.width;
      final y = ((i * 47) % 211) / 211 * size.height * .42;
      canvas.drawCircle(Offset(x, y), i % 5 == 0 ? 1.4 : .7, starPaint);
    }
  }

  void _paintCairo(Canvas canvas, Size size) {
    final horizon = size.height * .47;
    final silhouette = Paint()..color = const Color(0xFF070B18);

    final pyramid = Path()
      ..moveTo(size.width * .04, horizon)
      ..lineTo(size.width * .20, size.height * .25)
      ..lineTo(size.width * .36, horizon)
      ..close();
    canvas.drawPath(pyramid, silhouette);
    canvas.drawLine(
      Offset(size.width * .20, size.height * .25),
      Offset(size.width * .36, horizon),
      Paint()
        ..color = const Color(0x66FFC857)
        ..strokeWidth = 2,
    );

    final skyline = Path()..moveTo(size.width * .37, horizon);
    for (var i = 0; i < 16; i++) {
      final x = size.width * (.37 + i * .043);
      final buildingTop = horizon - size.height * (.035 + (i % 4) * .017);
      skyline
        ..lineTo(x, buildingTop)
        ..lineTo(x + size.width * .032, buildingTop)
        ..lineTo(x + size.width * .032, horizon);
    }
    skyline
      ..lineTo(size.width, horizon)
      ..close();
    canvas.drawPath(skyline, silhouette);

    _paintMinaret(canvas, Offset(size.width * .68, horizon), size.height * .18);
    _paintMinaret(canvas, Offset(size.width * .88, horizon), size.height * .13);

    final windows = Paint()..color = const Color(0xAAFFC857);
    for (var i = 0; i < 22; i++) {
      final x = size.width * (.39 + (i * .027));
      final y = horizon - size.height * (.018 + (i % 3) * .018);
      canvas.drawRect(Rect.fromLTWH(x, y, 2.5, 1.5), windows);
    }
  }

  void _paintMinaret(Canvas canvas, Offset base, double height) {
    final paint = Paint()..color = const Color(0xFF080B18);
    canvas.drawRect(
      Rect.fromLTWH(base.dx - 5, base.dy - height * .72, 10, height * .72),
      paint,
    );
    final spire = Path()
      ..moveTo(base.dx - 10, base.dy - height * .72)
      ..lineTo(base.dx, base.dy - height)
      ..lineTo(base.dx + 10, base.dy - height * .72)
      ..close();
    canvas.drawPath(spire, paint);
    canvas.drawLine(
      Offset(base.dx, base.dy - height),
      Offset(base.dx, base.dy - height - 9),
      Paint()
        ..color = _gold
        ..strokeWidth = 1.5,
    );
  }

  void _paintRoad(Canvas canvas, Size size) {
    final horizonY = size.height * .45;
    final road = Path()
      ..moveTo(size.width * .43, horizonY)
      ..lineTo(size.width * .96, size.height)
      ..lineTo(size.width * .04, size.height)
      ..lineTo(size.width * .57, horizonY)
      ..close();
    canvas.drawPath(
      road,
      Paint()
        ..shader = const LinearGradient(
          begin: Alignment.topCenter,
          end: Alignment.bottomCenter,
          colors: <Color>[Color(0xFF182033), Color(0xFF05070D)],
        ).createShader(Offset.zero & size),
    );

    final edge = Paint()
      ..color = _cyan
      ..strokeWidth = 3
      ..maskFilter = const MaskFilter.blur(BlurStyle.normal, 3);
    canvas.drawLine(
      Offset(size.width * .43, horizonY),
      Offset(size.width * .04, size.height),
      edge,
    );
    canvas.drawLine(
      Offset(size.width * .57, horizonY),
      Offset(size.width * .96, size.height),
      edge,
    );

    final distance = game.raceSession.distanceAlongLapMeters;
    final offset = (distance / 8) % 1;
    for (var i = -1; i < 12; i++) {
      final t = ((i + offset) / 11).clamp(0.0, 1.0);
      final nextT = (t + .045).clamp(0.0, 1.0);
      final y1 = _perspectiveY(t, horizonY, size.height);
      final y2 = _perspectiveY(nextT, horizonY, size.height);
      final width1 = _roadHalfWidth(t, size.width);
      final width2 = _roadHalfWidth(nextT, size.width);
      final center = size.width / 2;
      final stripe = Path()
        ..moveTo(center - width1 * .018, y1)
        ..lineTo(center - width2 * .018, y2)
        ..lineTo(center + width2 * .018, y2)
        ..lineTo(center + width1 * .018, y1)
        ..close();
      canvas.drawPath(stripe, Paint()..color = const Color(0x88EAFBFF));
    }
  }

  void _paintRace(Canvas canvas, Size size) {
    final session = game.raceSession;
    final horizonY = size.height * .45;
    final playerDistance = session.distanceAlongLapMeters;
    final playerSlip = session.vehicle.state.lateralSlipMps;

    for (var i = 0; i < session.ai.drivers.length; i++) {
      final ai = session.ai.drivers[i];
      final relative = ai.distanceMeters - playerDistance;
      if (relative < -8 || relative > 155) continue;
      final t = (1 - relative / 175).clamp(.12, .86);
      final y = _perspectiveY(t, horizonY, size.height);
      final roadWidth = _roadHalfWidth(t, size.width);
      final x =
          size.width / 2 + (ai.lateralOffsetMeters / 8.5) * roadWidth * .76;
      final scale = .28 + t * .65;
      _paintCar(
        canvas,
        Offset(x, y),
        38 * scale,
        <Color>[_magenta, _gold, _cyan][i],
        nitro: ai.nitro,
      );
    }

    final playerX = size.width / 2 + playerSlip.clamp(-8, 8) * 5;
    final playerY = size.height * .78;
    final drifting = session.vehicle.state.isDrifting;
    final nitro = session.spirit.nitroActive;
    if (drifting || nitro) {
      _paintSpiritTrail(canvas, Offset(playerX, playerY + 25), drifting, nitro);
    }
    canvas.save();
    canvas.translate(playerX, playerY);
    canvas.rotate((-playerSlip * .012).clamp(-.12, .12));
    _paintCar(
      canvas,
      Offset.zero,
      math.min(size.width * .11, 72),
      _cyan,
      nitro: nitro,
    );
    canvas.restore();
  }

  void _paintSpiritTrail(Canvas canvas, Offset origin, bool drift, bool nitro) {
    final color = nitro ? _cyan : _magenta;
    final glow = Paint()
      ..color = color.withValues(alpha: .65)
      ..strokeWidth = nitro ? 16 : 10
      ..strokeCap = StrokeCap.round
      ..maskFilter = const MaskFilter.blur(BlurStyle.normal, 12);
    canvas.drawLine(origin, origin + Offset(0, nitro ? 125 : 75), glow);
    if (drift) {
      canvas.drawArc(
        Rect.fromCenter(center: origin, width: 150, height: 90),
        .2,
        2.7,
        false,
        Paint()
          ..color = _magenta.withValues(alpha: .55)
          ..strokeWidth = 6
          ..style = PaintingStyle.stroke,
      );
    }
  }

  void _paintCar(
    Canvas canvas,
    Offset center,
    double width,
    Color accent, {
    required bool nitro,
  }) {
    final height = width * 1.48;
    final body = RRect.fromRectAndRadius(
      Rect.fromCenter(center: center, width: width, height: height),
      Radius.circular(width * .22),
    );
    canvas.drawRRect(
      body.inflate(width * .12),
      Paint()
        ..color = accent.withValues(alpha: .28)
        ..maskFilter = const MaskFilter.blur(BlurStyle.normal, 14),
    );
    canvas.drawRRect(
      body,
      Paint()
        ..shader = LinearGradient(
          begin: Alignment.topLeft,
          end: Alignment.bottomRight,
          colors: <Color>[
            accent,
            const Color(0xFF0B1020),
            accent.withValues(alpha: .45),
          ],
        ).createShader(body.outerRect),
    );
    final cabin = RRect.fromRectAndRadius(
      Rect.fromCenter(
        center: center - Offset(0, height * .12),
        width: width * .58,
        height: height * .36,
      ),
      Radius.circular(width * .12),
    );
    canvas.drawRRect(cabin, Paint()..color = const Color(0xDD07101E));
    canvas.drawLine(
      center + Offset(-width * .34, height * .34),
      center + Offset(width * .34, height * .34),
      Paint()
        ..color = accent
        ..strokeWidth = 3,
    );
    final lightPaint = Paint()..color = nitro ? _cyan : const Color(0xFFFF4567);
    canvas.drawCircle(
      center + Offset(-width * .28, height * .31),
      width * .07,
      lightPaint,
    );
    canvas.drawCircle(
      center + Offset(width * .28, height * .31),
      width * .07,
      lightPaint,
    );
  }

  double _perspectiveY(double t, double horizon, double bottom) {
    final curved = t * t;
    return horizon + (bottom - horizon) * curved;
  }

  double _roadHalfWidth(double t, double width) => width * (.07 + .39 * t * t);
}
