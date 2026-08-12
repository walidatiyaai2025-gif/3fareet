import 'dart:convert';

import 'package:afareet_asphalt/game/career/progression_system.dart';

class CareerSaveCodec {
  const CareerSaveCodec();

  String encode(CareerProgress progress) => jsonEncode(<String, Object?>{
        'version': CareerProgress.currentVersion,
        'stars': progress.stars,
        'completedNodeIds': progress.completedNodeIds.toList()..sort(),
        'claimedRewardIds': progress.claimedRewardIds.toList()..sort(),
      });

  CareerProgress decode(String source) {
    final raw = jsonDecode(source);
    if (raw is! Map<String, dynamic>) {
      throw const FormatException('Career save root must be an object.');
    }
    return migrate(raw);
  }

  CareerProgress migrate(Map<String, dynamic> raw) {
    final version = raw['version'];
    if (version == 1) {
      return CareerProgress(
        version: CareerProgress.currentVersion,
        stars: (raw['stars'] as num?)?.toInt().clamp(0, 9999) ?? 0,
        completedNodeIds: _stringSet(raw['completedNodeIds']),
        claimedRewardIds: _stringSet(raw['claimedRewardIds']),
      );
    }
    if (version == null || version == 0) {
      return CareerProgress(
        version: CareerProgress.currentVersion,
        stars: (raw['totalStars'] as num?)?.toInt().clamp(0, 9999) ?? 0,
        completedNodeIds: _stringSet(raw['completed']),
        claimedRewardIds: const <String>{},
      );
    }
    throw FormatException('Unsupported career save version: $version');
  }

  Set<String> _stringSet(Object? value) {
    if (value is! List) return <String>{};
    return value.whereType<String>().where((item) => item.trim().isNotEmpty).toSet();
  }
}
