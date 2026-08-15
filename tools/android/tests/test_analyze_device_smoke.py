import importlib.util
import json
import tempfile
import unittest
from pathlib import Path


MODULE_PATH = Path(__file__).resolve().parents[1] / "analyze_device_smoke.py"
SPEC = importlib.util.spec_from_file_location("analyze_device_smoke", MODULE_PATH)
MODULE = importlib.util.module_from_spec(SPEC)
assert SPEC.loader is not None
SPEC.loader.exec_module(MODULE)


class AnalyzeDeviceSmokeTests(unittest.TestCase):
    def _write_checkpoint(
        self,
        root: Path,
        label: str,
        *,
        pss_kib: int,
        p95: float,
        p99: float,
        thermal: int = 1,
        red_flags: int = 0,
        apk_sha: str = "a" * 64,
        device_sha: str = "b" * 64,
    ) -> None:
        checkpoint = root / "checkpoints" / label
        checkpoint.mkdir(parents=True, exist_ok=True)
        (checkpoint / "checkpoint.json").write_text(json.dumps({
            "label": label,
            "apkSha256": apk_sha,
            "deviceSerialSha256": device_sha,
            "automatedRedFlagCount": red_flags,
        }), encoding="utf-8")
        (checkpoint / "meminfo.txt").write_text(f"TOTAL PSS: {pss_kib}\n", encoding="utf-8")
        (checkpoint / "gfxinfo.txt").write_text(
            f"95th percentile: {p95}ms\n99th percentile: {p99}ms\nJanky frames: 2 (1.0%)\n",
            encoding="utf-8",
        )
        (checkpoint / "thermalservice.txt").write_text(f"Thermal Status: {thermal}\n", encoding="utf-8")
        (checkpoint / "battery.txt").write_text("level: 80\nUSB powered: false\n", encoding="utf-8")

    def _session(self, root: Path) -> None:
        (root / "session.json").write_text(json.dumps({
            "apk": {"sha256": "a" * 64},
            "device": {"serialSha256": "b" * 64},
        }), encoding="utf-8")

    def test_mid_smoke_accepts_clean_android_observable_metrics_for_manual_review(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            self._session(root)
            self._write_checkpoint(root, "smoke-cold-start", pss_kib=500 * 1024, p95=20, p99=25)
            self._write_checkpoint(root, "smoke-warm-race", pss_kib=700 * 1024, p95=15, p99=20)
            self._write_checkpoint(root, "smoke-after-restarts", pss_kib=728 * 1024, p95=16, p99=21)

            result = MODULE.analyze(root, "mid")
            self.assertEqual("PASSABLE_FOR_MANUAL_REVIEW", result["verdict"])
            self.assertEqual([], result["blockers"])
            self.assertAlmostEqual(4.0, result["restartPssGrowthPercent"], places=2)
            self.assertFalse(result["verified"])

    def test_smoke_blocks_memory_growth_frame_budget_thermal_and_red_flags(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            self._session(root)
            self._write_checkpoint(root, "smoke-cold-start", pss_kib=500 * 1024, p95=20, p99=25)
            self._write_checkpoint(root, "smoke-warm-race", pss_kib=800 * 1024, p95=20, p99=30)
            self._write_checkpoint(
                root,
                "smoke-after-restarts",
                pss_kib=900 * 1024,
                p95=25,
                p99=35,
                thermal=3,
                red_flags=1,
            )

            result = MODULE.analyze(root, "mid")
            self.assertEqual("BLOCKED", result["verdict"])
            joined = "\n".join(result["blockers"])
            self.assertIn("red flags present", joined)
            self.assertIn("SEVERE or worse", joined)
            self.assertIn("frameP95Ms", joined)
            self.assertIn("frameP99Ms", joined)
            self.assertIn("restart PSS growth", joined)

    def test_missing_required_checkpoint_blocks(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            self._session(root)
            self._write_checkpoint(root, "smoke-cold-start", pss_kib=500 * 1024, p95=20, p99=25)
            result = MODULE.analyze(root, "low")
            self.assertEqual("BLOCKED", result["verdict"])
            self.assertTrue(any("missing required smoke checkpoint" in item for item in result["blockers"]))

    def test_checkpoint_fingerprint_mismatch_blocks(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            self._session(root)
            for label in MODULE.REQUIRED_LABELS:
                self._write_checkpoint(
                    root,
                    label,
                    pss_kib=500 * 1024,
                    p95=20,
                    p99=25,
                    apk_sha="c" * 64,
                )
            result = MODULE.analyze(root, "low")
            self.assertEqual("BLOCKED", result["verdict"])
            self.assertTrue(any("APK SHA does not match session" in item for item in result["blockers"]))


if __name__ == "__main__":
    unittest.main()
