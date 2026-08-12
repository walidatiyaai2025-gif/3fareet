enum CareerRaceMode { circuit, timeTrial, elimination, driftChallenge, boss }

enum CareerNodeState { locked, available, completed }

class CareerRaceNode {
  const CareerRaceNode({
    required this.id,
    required this.title,
    required this.mode,
    required this.trackId,
    required this.requiredStars,
    this.targetTimeSeconds,
    this.targetDriftScore,
    this.bossVehicleId,
  });

  final String id;
  final String title;
  final CareerRaceMode mode;
  final String trackId;
  final int requiredStars;
  final double? targetTimeSeconds;
  final int? targetDriftScore;
  final String? bossVehicleId;

  bool get isValid {
    if (id.trim().isEmpty || title.trim().isEmpty || trackId.trim().isEmpty || requiredStars < 0) {
      return false;
    }
    switch (mode) {
      case CareerRaceMode.timeTrial:
        return targetTimeSeconds != null && targetTimeSeconds! > 0;
      case CareerRaceMode.driftChallenge:
        return targetDriftScore != null && targetDriftScore! > 0;
      case CareerRaceMode.boss:
        return bossVehicleId != null && bossVehicleId!.trim().isNotEmpty;
      case CareerRaceMode.circuit:
      case CareerRaceMode.elimination:
        return true;
    }
  }
}

class CareerChapter {
  const CareerChapter({
    required this.id,
    required this.title,
    required this.order,
    required this.nodes,
    this.requiredStars = 0,
  });

  final String id;
  final String title;
  final int order;
  final int requiredStars;
  final List<CareerRaceNode> nodes;

  bool get isValid => id.trim().isNotEmpty && title.trim().isNotEmpty && order >= 1 && requiredStars >= 0 && nodes.isNotEmpty && nodes.every((node) => node.isValid) && nodes.map((node) => node.id).toSet().length == nodes.length;
}

class CareerMap {
  CareerMap(Iterable<CareerChapter> chapters)
      : chapters = List<CareerChapter>.unmodifiable(chapters.toList()..sort((a, b) => a.order.compareTo(b.order))) {
    if (this.chapters.isEmpty || this.chapters.any((chapter) => !chapter.isValid)) {
      throw ArgumentError('Career map contains invalid chapters.');
    }
    if (this.chapters.map((chapter) => chapter.id).toSet().length != this.chapters.length) {
      throw ArgumentError('Career chapter IDs must be unique.');
    }
  }

  final List<CareerChapter> chapters;

  CareerChapter? chapterById(String id) {
    for (final chapter in chapters) {
      if (chapter.id == id) return chapter;
    }
    return null;
  }

  CareerNodeState nodeState(CareerRaceNode node, {required int earnedStars, required Set<String> completedNodeIds}) {
    if (completedNodeIds.contains(node.id)) return CareerNodeState.completed;
    return earnedStars >= node.requiredStars ? CareerNodeState.available : CareerNodeState.locked;
  }
}
