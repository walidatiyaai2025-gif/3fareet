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
        metadata_label: str | None = None,
        include_red_flag_count: bool = True,
    ) -> None:
        checkpoint = root / "checkpoints" / label
        checkpoint.mkdir(parents=True, exist_ok=True)
        metadata = {
            "label": label if metadata_label is None else metadata_label,
            "apkSha256": apk_sha,
            "deviceSerialSha256": device_sha,
        }
        if include_red_flag_count:
            metadata["automatedRedFlagCount"] = red_flags
        (checkpoint / "checkpoint.json").write_text(json.dumps(metadata), encoding="utf-8")
        (checkpoint / "meminfo.txt").write_text(f"TOTAL PSS: {pss_kib}\n", encoding="utf-8")
        (checkpoint / "gfxinfo.txt").write_text(
            f"95th percentile: {p95}ms\n99th percentile: {p99}ms\nJanky frames: 2 (1.0%)\n",
            encoding="utf-8",
        )
        (checkpoint / "thermalservice.txt").write_text(f"Thermal Status: {thermal}\n", encoding="utf-8")
        (checkpoint / "battery.txt").write_text("level: 80\nUSB powered: false\n", encoding="utf-8")

    def _session(
        self,
        root: Path,
        *,
        apk_sha: str = "a" * 64,
        device_sha: str = "b" * 64,
        performance_tier: str | None = "mid",
        include_apk: bool = True,
        include_device: bool = True,
    ) -> None:
        payload = {}
        if include_apk:
            payload["apk"] = {"sha256": apk_sha}
        if include_device:
            payload["device"] = {"serialSha256": device_sha}
        if performance_tier is not None:
            payload["performanceTier"] = performance_tier
        (root / "session.json").write_text(json.dumps(payload), encoding="utf-8")

    def _write_clean_smoke_set(self, root: Path) -> None:
        self._write_checkpoint(root, "smoke-cold-start", pss_kib=500 * 1024, p95=20, p99=25)
        self._write_checkpoint(root, "smoke-warm-race", pss_kib=700 * 1024, p95=15, p99=20)
        self._write_checkpoint(root, "smoke-after-restarts", pss_kib=728 * 1024, p95=16, p99=21)

    def test_mid_smoke_accepts_clean_android_observable_metrics_for_manual_review(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            self._session(root)
            self._write_clean_smoke_set(root)

            result = MODULE.analyze(root, "mid")
            self.assertEqual("PASSABLE_FOR_MANUAL_REVIEW", result["verdict"])
            self.assertEqual([], result["blockers"])
            self.assertEqual("MID", result["sessionPerformanceTier"])
            self.assertAlmostEqual(4.0, result["restartPssGrowthPercent"], places=2)
            self.assertFalse(result["verified"])

    def test_missing_or_invalid_performance_tier_binding_blocks(self):
        cases = (None, "", "ultra")
        for performance_tier in cases:
            with self.subTest(performance_tier=performance_tier), tempfile.TemporaryDirectory() as temp:
                root = Path(temp)
                self._session(root, performance_tier=performance_tier)
                self._write_clean_smoke_set(root)
                result = MODULE.analyze(root, "mid")
                self.assertEqual("BLOCKED", result["verdict"])
                self.assertIn("session: missing or invalid performanceTier binding", result["blockers"])

    def test_requested_tier_must_match_capture_session_tier(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            self._session(root, performance_tier="mid")
            self._write_clean_smoke_set(root)

            result = MODULE.analyze(root, "low")
            self.assertEqual("BLOCKED", result["verdict"])
            self.assertIn(
                "session: performanceTier mismatch (captured=MID requested=LOW)",
                result["blockers"],
            )

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
            self._session(root, performance_tier="low")
            self._write_checkpoint(root, "smoke-cold-start", pss_kib=500 * 1024, p95=20, p99=25)
            result = MODULE.analyze(root, "low")
            self.assertEqual("BLOCKED", result["verdict"])
            self.assertTrue(any("missing required smoke checkpoint" in item for item in result["blockers"]))

    def test_checkpoint_fingerprint_mismatch_blocks(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            self._session(root, performance_tier="low")
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

    def test_missing_or_invalid_session_fingerprints_block(self):
        cases = (
            ({"include_apk": False}, "session: missing or invalid APK SHA-256 fingerprint"),
            ({"include_device": False}, "session: missing or invalid device serial SHA-256 fingerprint"),
            ({"apk_sha": "not-a-sha"}, "session: missing or invalid APK SHA-256 fingerprint"),
            ({"device_sha": "1234"}, "session: missing or invalid device serial SHA-256 fingerprint"),
        )
        for session_kwargs, expected in cases:
            with self.subTest(session_kwargs=session_kwargs), tempfile.TemporaryDirectory() as temp:
                root = Path(temp)
                self._session(root, **session_kwargs)
                self._write_clean_smoke_set(root)
                result = MODULE.analyze(root, "mid")
                self.assertEqual("BLOCKED", result["verdict"])
                self.assertIn(expected, result["blockers"])

    def test_missing_or_invalid_checkpoint_fingerprints_block(self):
        cases = (
            ({"apk_sha": ""}, "missing or invalid checkpoint APK SHA-256 fingerprint"),
            ({"apk_sha": "z" * 64}, "missing or invalid checkpoint APK SHA-256 fingerprint"),
            ({"device_sha": ""}, "missing or invalid checkpoint device SHA-256 fingerprint"),
            ({"device_sha": "1234"}, "missing or invalid checkpoint device SHA-256 fingerprint"),
        )
        for checkpoint_kwargs, expected in cases:
            with self.subTest(checkpoint_kwargs=checkpoint_kwargs), tempfile.TemporaryDirectory() as temp:
                root = Path(temp)
                self._session(root)
                self._write_clean_smoke_set(root)
                self._write_checkpoint(
                    root,
                    "smoke-warm-race",
                    pss_kib=700 * 1024,
                    p95=15,
                    p99=20,
                    **checkpoint_kwargs,
                )
                result = MODULE.analyze(root, "mid")
                self.assertEqual("BLOCKED", result["verdict"])
                self.assertTrue(any(expected in item for item in result["blockers"]))

    def test_checkpoint_label_mismatch_blocks(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            self._session(root)
            self._write_clean_smoke_set(root)
            self._write_checkpoint(
                root,
                "smoke-warm-race",
                pss_kib=700 * 1024,
                p95=15,
                p99=20,
                metadata_label="smoke-after-restarts",
            )
            result = MODULE.analyze(root, "mid")
            self.assertEqual("BLOCKED", result["verdict"])
            self.assertTrue(any("checkpoint metadata label mismatch" in item for item in result["blockers"]))

    def test_missing_or_invalid_red_flag_counter_blocks(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            self._session(root)
            self._write_clean_smoke_set(root)
            self._write_checkpoint(
                root,
                "smoke-warm-race",
                pss_kib=700 * 1024,
                p95=15,
                p99=20,
                include_red_flag_count=False,
            )
            result = MODULE.analyze(root, "mid")
            self.assertEqual("BLOCKED", result["verdict"])
            self.assertTrue(any("automated red-flag count is missing or invalid" in item for item in result["blockers"]))

            checkpoint_json = root / "checkpoints" / "smoke-warm-race" / "checkpoint.json"
            metadata = json.loads(checkpoint_json.read_text(encoding="utf-8"))
            metadata["automatedRedFlagCount"] = "not-a-number"
            checkpoint_json.write_text(json.dumps(metadata), encoding="utf-8")
            result = MODULE.analyze(root, "mid")
            self.assertEqual("BLOCKED", result["verdict"])
            self.assertTrue(any("automated red-flag count is missing or invalid" in item for item in result["blockers"]))

    def test_zero_warm_pss_baseline_blocks_restart_growth(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            self._session(root)
            self._write_checkpoint(root, "smoke-cold-start", pss_kib=500 * 1024, p95=20, p99=25)
            self._write_checkpoint(root, "smoke-warm-race", pss_kib=0, p95=15, p99=20)
            self._write_checkpoint(root, "smoke-after-restarts", pss_kib=10 * 1024, p95=16, p99=21)
            result = MODULE.analyze(root, "mid")
            self.assertEqual("BLOCKED", result["verdict"])
            self.assertIn("smoke-warm-race: process PSS baseline must be greater than zero", result["blockers"])


if __name__ == "__main__":
    unittest.main()
