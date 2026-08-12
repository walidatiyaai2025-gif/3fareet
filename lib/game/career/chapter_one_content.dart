import 'package:afareet_asphalt/game/career/chapter_one.dart';
import 'package:afareet_asphalt/game/career/progression_system.dart';

List<CareerNodeDefinition> buildChapterOneContent() {
  final chapter = buildChapterOneFoundation();
  return <CareerNodeDefinition>[
    for (var index = 0; index < chapter.nodes.length; index++)
      CareerNodeDefinition(
        node: chapter.nodes[index],
        objectives: <CareerObjective>[
          CareerObjective(id: 'finish_${chapter.nodes[index].id}', description: 'Finish the event', target: 1),
          if (index > 0)
            CareerObjective(id: 'clean_${chapter.nodes[index].id}', description: 'Finish without restart', target: 1),
        ],
        rewards: <CareerReward>[
          CareerReward(coins: 250 + (index * 100), spirit: 5 + index),
          if (chapter.nodes[index].mode.name == 'boss')
            const CareerReward(unlockVehicleId: 'djinn_spirit'),
        ],
      ),
  ];
}
