import 'package:afareet_asphalt/game/career/career_models.dart';

CareerChapter buildChapterOneFoundation() => const CareerChapter(
      id: 'chapter_01_cairo_after_dark',
      title: 'Cairo After Dark',
      order: 1,
      nodes: <CareerRaceNode>[
        CareerRaceNode(
          id: 'c01_r01',
          title: 'Corniche Run',
          mode: CareerRaceMode.circuit,
          trackId: 'cairo_corniche_night',
          requiredStars: 0,
        ),
        CareerRaceNode(
          id: 'c01_r02',
          title: 'Clock of Khan',
          mode: CareerRaceMode.timeTrial,
          trackId: 'khan_el_khalili_sprint',
          requiredStars: 2,
          targetTimeSeconds: 92,
        ),
        CareerRaceNode(
          id: 'c01_r03',
          title: 'Last Car Standing',
          mode: CareerRaceMode.elimination,
          trackId: 'ring_road_midnight',
          requiredStars: 4,
        ),
        CareerRaceNode(
          id: 'c01_r04',
          title: 'Spirit Drift',
          mode: CareerRaceMode.driftChallenge,
          trackId: 'citadel_drift',
          requiredStars: 6,
          targetDriftScore: 12000,
        ),
        CareerRaceNode(
          id: 'c01_boss',
          title: 'Djinn of the Asphalt',
          mode: CareerRaceMode.boss,
          trackId: 'pyramids_spirit_run',
          requiredStars: 9,
          bossVehicleId: 'djinn_spirit',
        ),
      ],
    );
