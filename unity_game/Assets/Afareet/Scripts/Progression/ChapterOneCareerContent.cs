using System.Collections.Generic;

namespace Afareet.Progression
{
    public static class ChapterOneCareerContent
    {
        public static CareerChapter CreateFoundation()
        {
            return new CareerChapter(
                id: "chapter_01_cairo_after_dark",
                title: "Cairo After Dark",
                order: 1,
                nodes: new List<CareerRaceNode>
                {
                    new CareerRaceNode(
                        id: "c01_r01",
                        title: "Corniche Run",
                        mode: CareerRaceMode.Circuit,
                        trackId: "cairo_corniche_night",
                        requiredStars: 0),
                    new CareerRaceNode(
                        id: "c01_r02",
                        title: "Clock of Khan",
                        mode: CareerRaceMode.TimeTrial,
                        trackId: "khan_el_khalili_sprint",
                        requiredStars: 2,
                        targetTimeSeconds: 92d),
                    new CareerRaceNode(
                        id: "c01_r03",
                        title: "Last Car Standing",
                        mode: CareerRaceMode.Elimination,
                        trackId: "ring_road_midnight",
                        requiredStars: 4),
                    new CareerRaceNode(
                        id: "c01_r04",
                        title: "Spirit Drift",
                        mode: CareerRaceMode.DriftChallenge,
                        trackId: "citadel_drift",
                        requiredStars: 6,
                        targetDriftScore: 12000),
                    new CareerRaceNode(
                        id: "c01_boss",
                        title: "Djinn of the Asphalt",
                        mode: CareerRaceMode.Boss,
                        trackId: "pyramids_spirit_run",
                        requiredStars: 9,
                        bossVehicleId: "djinn_spirit")
                });
        }
    }
}
