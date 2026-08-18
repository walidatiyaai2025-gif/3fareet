import unittest
from pathlib import Path


REPO = Path(__file__).resolve().parents[3]
README = REPO / "docs" / "assets" / "01_vehicles" / "rival_cars_production" / "README.md"


class Uart004RivalHandoffReadmeContractTests(unittest.TestCase):
    def test_artist_handoff_matches_isolated_production_source_authority(self):
        text = README.read_text(encoding="utf-8")

        for required in (
            "Assets/Afareet/ArtSource/Vehicles/Rivals/Production/",
            "Rival_01_WedgeCoupe_Production.obj",
            "Rival_02_FastbackMuscle_Production.obj",
            "Rival_03_CompactPrototype_Production.obj",
            "ValidateAllSourcesBeforeMutation()",
            "and their `.meta` files",
            "review/reference material only",
            "cannot make UART-004 source-ready",
            "The actual production exchange files under `/Rivals/Production/` are intentionally absent",
            "**BLOCKED.**",
        ):
            self.assertIn(required, text)

        for stale in (
            "for example `Assets/Afareet/ArtSource/Vehicles/Rivals/Rival_01.fbx`",
            "loads the already-imported tracked OBJ source from `Assets/Afareet/ArtSource/Vehicles/Rivals/`;",
            "Three distinct static OBJ source **candidates** now exist under `Assets/Afareet/ArtSource/Vehicles/Rivals/`",
        ):
            self.assertNotIn(stale, text)

    def test_handoff_never_claims_production_acceptance_from_static_sources(self):
        text = README.read_text(encoding="utf-8")
        self.assertIn("does not promote any asset to accepted Production Art", text)
        self.assertIn("licensed Unity import/compile/render succeeds", text)
        self.assertIn("owner/Art Director visual review explicitly accepts them", text)


if __name__ == "__main__":
    unittest.main()
