import 'package:afareet_asphalt/game/career/career_models.dart';

class CareerObjective {
  const CareerObjective({required this.id, required this.description, required this.target});
  final String id;
  final String description;
  final num target;
}

class CareerReward {
  const CareerReward({this.coins = 0, this.spirit = 0, this.unlockVehicleId});
  final int coins;
  final int spirit;
  final String? unlockVehicleId;
}

class CareerNodeDefinition {
  const CareerNodeDefinition({required this.node, required this.objectives, required this.rewards});
  final CareerRaceNode node;
  final List<CareerObjective> objectives;
  final List<CareerReward> rewards;
}

class CareerProgress {
  const CareerProgress({
    required this.version,
    required this.stars,
    required this.completedNodeIds,
    required this.claimedRewardIds,
  });

  static const currentVersion = 1;

  final int version;
  final int stars;
  final Set<String> completedNodeIds;
  final Set<String> claimedRewardIds;

  CareerProgress copyWith({int? stars, Set<String>? completedNodeIds, Set<String>? claimedRewardIds}) => CareerProgress(
        version: currentVersion,
        stars: stars ?? this.stars,
        completedNodeIds: completedNodeIds ?? this.completedNodeIds,
        claimedRewardIds: claimedRewardIds ?? this.claimedRewardIds,
      );
}

class CareerProgressionService {
  const CareerProgressionService();

  bool canEnter(CareerRaceNode node, CareerProgress progress) => progress.stars >= node.requiredStars;

  CareerProgress completeNode(CareerProgress progress, String nodeId, {required int starsEarned}) {
    final completed = {...progress.completedNodeIds, nodeId};
    return progress.copyWith(stars: progress.stars + starsEarned.clamp(0, 3), completedNodeIds: completed);
  }

  bool canClaim(String rewardId, CareerProgress progress) => !progress.claimedRewardIds.contains(rewardId);

  CareerProgress claim(String rewardId, CareerProgress progress) {
    if (!canClaim(rewardId, progress)) return progress;
    return progress.copyWith(claimedRewardIds: {...progress.claimedRewardIds, rewardId});
  }

  bool chapterComplete(CareerChapter chapter, CareerProgress progress) => chapter.nodes.every((node) => progress.completedNodeIds.contains(node.id));
}
